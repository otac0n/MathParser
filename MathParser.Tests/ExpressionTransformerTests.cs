// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using MathParser.Testing;
    using NUnit.Framework;

    [TestFixture]
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
            TestContext.CurrentContext.ApproveFromFile(contents, ".txt", File.WriteAllText, File.ReadAllText, (expected, actual) =>
            {
                Assert.That(actual.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Last(), Is.EqualTo("(stable)"));
                Assert.That(actual, Is.EqualTo(expected));
            });
        }
    }
}
