using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace hashtagstograph
{
    public class SqlitePostReader : IPostReader
    {
        private readonly string _connectionString;
        private readonly string _query;
        private readonly string _separator;
        
        public SqlitePostReader(string connectionString, string query, string separator)
        {
            _connectionString = connectionString;
            _query = query;
            _separator = separator;
        }

        public IEnumerable<Post> ReadPosts(Func<string, string> getHash)
        {
            using var connection = new SqliteConnection(_connectionString);
            
            connection.Open();

            using var cmd = new SqliteCommand(_query, connection);
            using var reader = cmd.ExecuteReader();
            
            while (reader.Read())
            {
                var postId = reader.GetString(0);
                var tagLine = reader.GetString(1);
                
                var tags = tagLine
                    .Split(_separator, StringSplitOptions.RemoveEmptyEntries)
                    .Select(IPostReader.NormalizeTag)
                    .Where(o => o != string.Empty)
                    .Distinct()
                    .Select(o => new Tag(getHash(o), o))
                    .ToImmutableArray();

                var post = new Post(postId, tags);
                
                yield return post;
            }
        }
    }
}