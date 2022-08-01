namespace MyFTP.Utils
{
    public static class StringExt
    {
        public static string PadBoth(this string str, int length, char @char)
        {
            int spaces = length - str.Length;
            int padLeft = spaces / 2 + str.Length;
            return str.PadLeft(padLeft, @char).PadRight(length, @char);
        }
    }
}
