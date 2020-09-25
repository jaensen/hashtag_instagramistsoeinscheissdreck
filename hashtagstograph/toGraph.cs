using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using CsvHelper.Configuration;

namespace hashtagstograph
{
    class Program
    {
        // TODO: Find adjectives in tags
        // TODO: Perform sentiment analysis on tag-adjectives
        
        private static async Task Read(IPostReader postReader, Index to)
        {
            await Task.Run(() =>
            {
                // Warning: Don't use "StringExtensions.GetHashCodeInt64(string str)" when
                //          the hash should be persisted.
                //          .Net Core generates a random hash seed on every program start
                //          so that the generated hashes won't match in a later run. 
                foreach (var post in postReader.ReadPosts(o => o))
                {
                    to.PostsById.TryAdd(post.Id, post);
                    
                    foreach (var postTag in post.Tags)
                    {
                        var isNewTag = to.TotalOccurrencesByTagHash.AddOrUpdate(
                            postTag.Hash,
                            1,
                            (key, existing) => existing + 1) == 1;

                        if (isNewTag)
                        {
                            to.TagValuesByTagHash.TryAdd(postTag.Hash, postTag.Value);
                        }

                        to.PostIdsByTagHash.AddOrUpdate(
                            postTag.Hash,
                            ImmutableHashSet.Create(new[] {post.Id}),
                            (key, existing) => existing.Add(post.Id));
                    }
                }
            });
        }

        private static async Task<ConcurrentDictionary<string, (string, int)>> CountCoOccurrences(Index index)
        {
            var coOccurrences = new ConcurrentDictionary<string, (string, int)>();
            
            await Task.WhenAll(
                index
                .TagValuesByTagHash
                .Keys
                .Select(tagHash => 
                    Task.Run(() => 
                    {
                        if (!index.PostIdsByTagHash.TryGetValue(tagHash, out var tagHashInPostIds))
                            throw new Exception($"Index corrupt (tagHash '{tagHash}' couldn't be assigned to any post).");

                        foreach (var postId in tagHashInPostIds)
                        {
                            var otherTagHashesInPost = index
                                .PostsById[postId]
                                .Tags
                                .Select(tag => tag.Hash);
                            
                            foreach (var otherTagHashInPost in otherTagHashesInPost)
                            {
                                if (tagHash == otherTagHashInPost)
                                    continue;

                                coOccurrences.AddOrUpdate(
                                    tagHash,
                                    (key) => (otherTagHashInPost, 1),
                                    (key, current) => (current.Item1, current.Item2 + 1));
                            }
                        }
                    })));

            return coOccurrences;
        }
        
        static async Task Main()
        {
            var connectionString = "Data Source=/home/daniel/Downloads/unterdb_DE.db;Cache=Shared";
            var query = "select id, tags from locations";
            var hashSeparator = "|";
            var nodesFilePath = "/home/daniel/Desktop/tag_nodes.csv";
            var edgesFilePath = "/home/daniel/Desktop/tag_edges.csv";
            var minCoOccurrence = 50;
            
            var index = new Index();
            
            var posts = new SqlitePostReader(
                connectionString, 
                query, 
                hashSeparator);
            
            await Read(posts, to: index);

            var foundEdges = await CountCoOccurrences(index);
            var usedTags = new HashSet<string>();

            var utf8WithoutBom = new UTF8Encoding(false);            
            await using var edgesStream = File.Create(edgesFilePath);
            await using var edgesWriter = new StreamWriter(edgesStream, utf8WithoutBom);
            await using var edgesCsvWriter = new CsvHelper.CsvWriter(
                edgesWriter,
                new CsvConfiguration(CultureInfo.InvariantCulture));

            edgesCsvWriter.WriteField("Source", true);
            edgesCsvWriter.WriteField("Target", true);
            edgesCsvWriter.WriteField("Weight", true);
            edgesCsvWriter.NextRecord();

            foreach (var edge in foundEdges)
            {
                if (edge.Value.Item2 < minCoOccurrence)
                    continue;

                usedTags.Add(edge.Key);
                usedTags.Add(edge.Value.Item1);
                
                edgesCsvWriter.WriteField(edge.Key, true);
                edgesCsvWriter.WriteField(edge.Value.Item1, true);
                edgesCsvWriter.WriteField(edge.Value.Item2.ToString(), true);
                edgesCsvWriter.NextRecord();
            }
            
            await using var nodesStream = File.Create(nodesFilePath);
            await using var nodesWriter = new StreamWriter(nodesStream, utf8WithoutBom);
            await using var nodesCsvWriter = new CsvHelper.CsvWriter(
                nodesWriter,
                new CsvConfiguration(CultureInfo.InvariantCulture));

            nodesCsvWriter.WriteField("Id", true);
            nodesCsvWriter.WriteField("Label", true);
            nodesCsvWriter.WriteField("Weight", true);
            nodesCsvWriter.NextRecord();

            foreach (var tag in index.TagValuesByTagHash)
            {
                if (!usedTags.Contains(tag.Key)) // Skip all tags which have no links
                    continue;

                var totalOccurrences = index.TotalOccurrencesByTagHash[tag.Key];

                nodesCsvWriter.WriteField(tag.Key, true);
                nodesCsvWriter.WriteField(tag.Value, true);
                nodesCsvWriter.WriteField(totalOccurrences.ToString(), true);
                nodesCsvWriter.NextRecord();
            }
        }
    }
}