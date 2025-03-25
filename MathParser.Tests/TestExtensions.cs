namespace MathParser.Tests
{
    using System.IO;
    using System.Text.RegularExpressions;

    internal static class TestExtensions
    {

        public static string SanitizeName(string testName)
        {
            return Regex.Replace(testName.Replace('"', '\'').Replace('*', '×').Replace('/', '÷'), "[" + Regex.Escape(new string(Path.GetInvalidFileNameChars())) + "]", "_");
        }
    }
}
