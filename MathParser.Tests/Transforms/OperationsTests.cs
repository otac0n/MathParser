// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser.Tests.Transforms
{
    using System.IO;
    using System.Reflection;
    using MathParser.Testing;
    using MathParser.Transforms;
    using NUnit.Framework;
    using static MathParser.Testing.TestExtensions;

    [TestFixture]
    internal class OperationsTests
    {
        [TestCaseSource(typeof(TestData), nameof(TestData.SimplifyStrings))]
        public void Simplify_Always_ReturnsTheExpectedExpression(string input)
        {
            var parser = new Parser();
            var expression = parser.Parse(input);

            var simplified = Operations.Simplify(expression);
            var result = simplified.TransformToString();

            WriteAndAssertResult(result);
        }

        private static void WriteAndAssertResult(string contents)
        {
            var test = TestContext.CurrentContext.Test;
            var testPath = Path.Combine(test.ClassName, SanitizeName(test.Name) + ".txt");
            var actualPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "ActualResults", testPath);
            var expectedPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "ExpectedResults", testPath);
            Directory.CreateDirectory(Path.GetDirectoryName(actualPath));
            File.WriteAllText(actualPath, contents);

            Assert.That(File.Exists(expectedPath), Is.True, () => $"A file matching '{actualPath}' is expected at '{expectedPath}'.");
            var expected = File.ReadAllText(expectedPath);
            Assert.That(contents, Is.EqualTo(expected));
        }
    }
}
