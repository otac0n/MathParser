// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using NUnit.Framework;
    using MathParser.Testing;
    using static MathParser.Testing.TestExtensions;

    public class ExpressionTransformerTests
    {

        [TestCaseSource(typeof(TestData), nameof(TestData.ExpressionStrings))]
        public void IteratedParseAndTransformToString_Always_ReturnsTheSameExpression(string input)
        {
            var allInput = new HashSet<string>();
            var allParsed = new HashSet<string>();
            string previousInput = null;
            string previousParsed = null;

            var sb = new StringBuilder();
            void Write(string line)
            {
                TestContext.WriteLine(line);
                sb.AppendLine(line);
            }

            Write(input);
            var parser = new Parser();
            allInput.Add(previousInput = input);

            while (true)
            {
                var expression = parser.Parse(input);
                var parsed = expression.ToString();
                Write("(parsed)");
                Write(parsed);

                if (parsed == previousParsed)
                {
                    Write("(stable)");
                    break;
                }
                else if (!allParsed.Add(parsed))
                {
                    Write("(astable)");
                    break;
                }
                else
                {
                    previousParsed = parsed;
                }

                input = expression.TransformToString();
                Write("(transformed)");
                Write(input);

                if (input == previousInput)
                {
                    Write("(stable)");
                    break;
                }
                else if (!allInput.Add(input))
                {
                    Write("(astable)");
                    break;
                }
                else
                {
                    previousInput = input;
                }
            }

            WriteAndAssertResult(sb.ToString());
        }

        private static void WriteAndAssertResult(string contents)
        {
            var test = TestContext.CurrentContext.Test;
            var testPath = Path.Combine(test.ClassName, SanitizeName(test.Name) + ".txt");
            var actualPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "ActualResults", testPath);
            var expectedPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "ExpectedResults", testPath);
            Directory.CreateDirectory(Path.GetDirectoryName(actualPath));
            File.WriteAllText(actualPath, contents);

            Assert.That(contents.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Last(), Is.EqualTo("(stable)"));
            Assert.That(File.Exists(expectedPath), Is.True, () => $"A file matching '{actualPath}' is expected at '{expectedPath}'.");
            var expected = File.ReadAllText(expectedPath);
            Assert.That(contents, Is.EqualTo(expected));
        }
    }
}
