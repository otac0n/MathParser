namespace MathParser.Testing
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using NUnit.Framework;

    public static class ApprovalExtensions
    {
        public static string SanitizeName(string testName)
        {
            return Regex.Replace(testName.Replace('"', '\'').Replace('*', '×').Replace('/', '÷'), "[" + Regex.Escape(new string(Path.GetInvalidFileNameChars())) + "]", "_");
        }

        public static void ApproveFromFile(this TestContext context, string actual, Action<string, string> assert) =>
            context.ApproveFromFile(actual, ".txt", File.WriteAllText, File.ReadAllText, assert);

        public static void ApproveFromFile<T>(this TestContext context, T actual, string extension, Action<string, T> save, Func<string, T> load, Action<T, T> assert)
        {
            var test = TestContext.CurrentContext.Test;
            var testPath = Path.Combine(test.ClassName, SanitizeName(test.Name) + extension);
            var actualPath = PathEncoder.ExtendPath(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "ActualResults", testPath));
            var expectedPath = PathEncoder.ExtendPath(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "ExpectedResults", testPath));
            Directory.CreateDirectory(Path.GetDirectoryName(actualPath));
            save(actualPath, actual);

            Assert.That(File.Exists(expectedPath), Is.True, () => $"A file matching '{actualPath}' is expected at '{expectedPath}'.");
            var expected = load(expectedPath);
            try
            {
                assert(expected, actual);
            }
            finally
            {
                (expected as IDisposable)?.Dispose();
            }
        }
    }
}
