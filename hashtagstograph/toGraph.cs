using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper.Configuration;
using Microsoft.Data.Sqlite;

namespace hashtagstograph
{
    class Post
    {
        public string Id { get; set; }

        public string[] Tags { get; set; }
    }
    
    class Program
    {
        // TODO: Find adjectives in tags
        // TODO: Perform sentiment analysis on tag-adjectives
        
        static void Main()
        {
            var centerTag = "#munich";
            var minTagOccurrence = 1;
            var minCoOccurrence = 1;
            
            int tagIdCounter = 0;
            var distinctTagsWithTotalCount = new Dictionary<string, int>();
            var distinctTagIDs = new Dictionary<string, int>();
            
            var tagsToPosts = new Dictionary<string, List<Post>>();
            
            using (var connection =
                new SqliteConnection("Data Source=/home/daniel/Downloads/unterdb_DE.db;Cache=Shared"))
            {
                connection.Open();
                
                using (var cmd = new SqliteCommand("select id, tags from locations", connection))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var post = new Post
                        {
                            Id = reader.GetString(0)
                        };

                        var tagLine = reader.GetString(1);

                        post.Tags = tagLine.Split("|")
                            .Select(o =>
                                o.Replace("\r", "")
                                    .Replace("\n", "")
                                    .ToLowerInvariant()
                                    .Trim())
                            .Where(o => o != "")
                            .ToArray();

                        foreach (var tag in post.Tags)
                        {
                            if (tag.Length == 0)
                                continue;
                            
                            string.Intern(tag);

                            if (distinctTagsWithTotalCount.TryGetValue(tag, out var count))
                            {
                                distinctTagsWithTotalCount[tag] = count + 1;
                            }
                            else
                            {
                                distinctTagsWithTotalCount.Add(tag, 1);
                                distinctTagIDs.Add(tag, tagIdCounter++); // Give each tag a unique ID
                            }

                            // Create a map that contains all posts which use this tag
                            // Tag -> [posts that have this Tag]
                            if (tagsToPosts.TryGetValue(tag, out var posts))
                            {
                                posts.Add(post);
                            }
                            else
                            {
                                posts = new List<Post>();
                                posts.Add(post);
                                tagsToPosts.Add(tag, posts);
                            }
                        }
                    }
                }
            }
            
            var tagCombos = new Dictionary<string, Dictionary<string, int>>();

            // Loop trough all tags ..
            foreach (var tag in distinctTagsWithTotalCount.Keys)
            {
                var distinctOtherTags = new Dictionary<string, int>();
                
                // .. then trough all posts which have this tag
                foreach (var postsWithTag in tagsToPosts[tag])
                {
                    foreach (var otherTag in postsWithTag.Tags)
                    {
                        if (otherTag == tag)
                            continue;

                        if (distinctOtherTags.TryGetValue(otherTag, out var count))
                        {
                            distinctOtherTags[otherTag] = count + 1;
                        }
                        else
                        {
                            distinctOtherTags.Add(otherTag, 1);
                        }
                    }
                }
                
                tagCombos.Add(tag, distinctOtherTags);
            }


            var utf8WithoutBom = new UTF8Encoding(false);
            using (var stream = File.Create("/home/daniel/Desktop/tag_edges.csv"))
            using (var streamWriter = new StreamWriter(stream, utf8WithoutBom))
            using (var csvWriter = new CsvHelper.CsvWriter(streamWriter, new CsvConfiguration(CultureInfo.InvariantCulture)))
            using (var tagStream = File.Create("/home/daniel/Desktop/tag_nodes.csv"))
            using (var tagStreamWriter = new StreamWriter(tagStream, utf8WithoutBom))
            using (var tagWriter =
                new CsvHelper.CsvWriter(tagStreamWriter, new CsvConfiguration(CultureInfo.InvariantCulture)))
            {
                csvWriter.WriteField("Source", false);
                csvWriter.WriteField("Target", false);
                csvWriter.WriteField("Weight", false);
                csvWriter.NextRecord();  
                    
                tagWriter.WriteField("Id", false);
                tagWriter.WriteField("Label", false);
                tagWriter.WriteField("Weight", false);
                tagWriter.NextRecord();                  
                
                var usedNodes = new HashSet<int>();
                
                foreach (var parentNode in tagCombos)
                {
                    if (distinctTagsWithTotalCount[parentNode.Key] < minTagOccurrence)
                        continue;
                    
                    if (parentNode.Key != centerTag)
                        continue;

                    usedNodes.Add(distinctTagIDs[parentNode.Key]);
                    
                    tagWriter.WriteField(distinctTagIDs[parentNode.Key].ToString(), false);
                    tagWriter.WriteField(parentNode.Key, true);
                    tagWriter.WriteField(distinctTagsWithTotalCount[parentNode.Key].ToString(), false);
                    tagWriter.NextRecord();

                    foreach (var comboNode in parentNode.Value)
                    {
                        if (distinctTagsWithTotalCount[comboNode.Key] < minTagOccurrence)
                            continue;
                        if (comboNode.Value < minCoOccurrence)
                            continue;
                        
                        if (!usedNodes.Contains(distinctTagIDs[comboNode.Key]))
                        {
                            usedNodes.Add(distinctTagIDs[comboNode.Key]);
                        }
                        else
                        {
                            continue;
                        }

                        tagWriter.WriteField(distinctTagIDs[comboNode.Key].ToString(), false);
                        tagWriter.WriteField(comboNode.Key, true);
                        tagWriter.WriteField(distinctTagsWithTotalCount[comboNode.Key].ToString(), false);
                        tagWriter.NextRecord();
                    }
                    
                    foreach (var a in parentNode.Value)
                    {
                        if (!usedNodes.Contains(distinctTagIDs[parentNode.Key]))
                            continue;
                        if (!usedNodes.Contains(distinctTagIDs[a.Key]))
                            continue;
                        if (a.Value < minCoOccurrence) // Erstelle nur Kanten zu Tags, die mind. N mal mit dem anderen Tag zusammen verwendet wurden.
                            continue;
                        
                        csvWriter.WriteField(distinctTagIDs[parentNode.Key].ToString(), false);
                        csvWriter.WriteField(distinctTagIDs[a.Key].ToString(), false);
                        csvWriter.WriteField(a.Value.ToString(), false);
                        csvWriter.NextRecord();
                    }
                }
            }
        }
    }
}