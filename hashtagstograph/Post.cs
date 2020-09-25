using System.Collections.Immutable;

namespace hashtagstograph
{
    public class Post
    {
        public string Id { get; }

        public ImmutableArray<Tag> Tags { get; }

        public Post(string id, ImmutableArray<Tag> tags)
        {
            Id = id;
            Tags = tags.ToImmutableArray();
        }
    }
}