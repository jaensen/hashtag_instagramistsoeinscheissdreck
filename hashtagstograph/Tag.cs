namespace hashtagstograph
{
    public class Tag
    {
        public string Hash { get; }
        public string Value { get; }

        public Tag(string hash, string value)
        {
            Hash = hash;
            Value = value;
        }
    }
}