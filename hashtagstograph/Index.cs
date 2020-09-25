using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace hashtagstograph
{
    public class Index
    {
        public ConcurrentDictionary<string, Post> PostsById { get; } 
            = new ConcurrentDictionary<string, Post>();
        
        public ConcurrentDictionary<string, string> TagValuesByTagHash { get; } 
            = new ConcurrentDictionary<string, string>();
        
        public ConcurrentDictionary<string, int> TotalOccurrencesByTagHash { get; } 
            = new ConcurrentDictionary<string, int>();
        
        public ConcurrentDictionary<string, ImmutableHashSet<string>> PostIdsByTagHash { get; }
            = new ConcurrentDictionary<string, ImmutableHashSet<string>>();
    }
}