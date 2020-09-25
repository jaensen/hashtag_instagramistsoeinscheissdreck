namespace hashtagstograph
{
    public static class StringExtensions
    {
        public static long GetHashCodeInt64(this string str)
        {
            var s1 = str.Substring(0, str.Length / 2);
            var s2 = str.Substring(str.Length / 2);
            return ((long)s1.GetHashCode()) << 0x20 | s2.GetHashCode();
        }
    }
}