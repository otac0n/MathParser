// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser.Tests.Transforms
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Numerics;
    using System.Reflection;
    using MathParser.Transforms;
    using NUnit.Framework;
    using static TestExtensions;

    [TestFixture]
    internal class OperationsTests
    {
        public static string[] TestCases =
        [
            "0*x",
            "x*0",
            "1*x",
            "x*1",
            "0+x",
            "x+0",
            "x*x",
            "x^2*x",
            "x*x^2",
            "x^2/x",
            "x/x^2",
            "x/x",
        ];

        [TestCaseSource(nameof(OperationsTests.TestCases))]
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
