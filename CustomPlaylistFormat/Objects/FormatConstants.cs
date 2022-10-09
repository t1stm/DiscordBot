namespace CustomPlaylistFormat.Objects
{
    public static class FormatConstants
    {
        public static readonly char[] FileStartHeader = {'t', 'o', '6', 'o', (char) 0, 'v','1', (char) 0}; // Too bloated. 2 bytes wasted smh....
        public static readonly char[] PlaylistBeginHeader = {'P','L','A','Y'};
        public static readonly char[] InfoBeginHeader = {'I','N','F','O'};
    }
}