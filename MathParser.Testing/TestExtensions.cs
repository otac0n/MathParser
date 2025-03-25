namespace MathParser.Testing
{
    using System.IO;
    using System.Text.RegularExpressions;

    public static class TestExtensions
    {
        public static string SanitizeName(string testName)
        {
            return Regex.Replace(testName.Replace('"', '\'').Replace('*', '×').Replace('/', '÷'), "[" + Regex.Escape(new string(Path.GetInvalidFileNameChars())) + "]", "_");
        }
    }
}
