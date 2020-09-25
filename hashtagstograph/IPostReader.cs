using System;
using System.Collections.Generic;

namespace hashtagstograph
{
    public interface IPostReader
    {
        public IEnumerable<Post> ReadPosts(Func<string, string> getHash);

        public static string NormalizeTag(string tag)
        {
            var lowerCaseTrimmedTag = tag
                .Trim()
                .ToLowerInvariant();

            lowerCaseTrimmedTag = lowerCaseTrimmedTag.Length > 0 && lowerCaseTrimmedTag[0] == '#' 
                ? lowerCaseTrimmedTag.Substring(1, lowerCaseTrimmedTag.Length - 1) 
                : lowerCaseTrimmedTag;

            var containsCr = lowerCaseTrimmedTag.IndexOf("\r", StringComparison.Ordinal) > -1;
            var containsNl = lowerCaseTrimmedTag.IndexOf("\n", StringComparison.Ordinal) > -1;
            
            if (!containsCr && !containsNl)
                return lowerCaseTrimmedTag;
                        
            if (containsCr && containsNl)
                return lowerCaseTrimmedTag
                    .Replace("\r", "")
                    .Replace("\n", " ");
                        
            return lowerCaseTrimmedTag.Replace(!containsCr ? "\n" : "\r", " ");
        }
    }
}