// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;
    using NUnit.Framework;

    public class ExpressionTransformerTests
    {
        public static string[] ExpressionTestCases => new string[]
        {
            "τ+π",
            "i",
            "e",
            "φ",
            "∞",
            "sin(x)",
            "COS(x)",
            "Atan(x)",
            "pOw(x,y)",
            "a (x)",
            "1.1",
            "10/2",
            "(((((((1+1)^2)^3)^4)^5)^6)^7)^8",
            "3*5",
            "1+1",
            "2^5",
            "2^2^2^2",
            "3-8",
            "(1+2^3^4)^(5*(6+7))",
            "((1+2)^(3+4))^5",
            "(1+2)*(3+4)",
            "1+2÷(2*4)",
            "8-(5+2)",
            "8+(5+2)",
            "(8+5)+2",
            "8-5-2",
            "(-2)^2",
            "-(2^2)",
            "(√1)^2",
            "√(1^2)",
            "√-1",
            "√Sqrt(√2)",
            "√(1+2)",
            "√(1*2)",
            "√(1*2)^√3*4",
            "√(1*2)+√3*4",
            "√(1*2)^√3*4",
            "((1+2)+(3+4))",
            "(((1+2)+3)+4)",
            "(1+(2+(3+4)))",
            "((1+(2+3))+4)",
            "(1+((2+3)+4))",
            "((1*2)*(3*4))",
            "(((1*2)*3)*4)",
            "(1*(2*(3*4)))",
            "((1*(2*3))*4)",
            "(1*((2*3)*4))",
            "((1÷2)÷(3÷4))",
            "(((1÷2)÷3)÷4)",
            "(1÷(2÷(3÷4)))",
            "((1÷(2÷3))÷4)",
            "(1÷((2÷3)÷4))",
            "((1-2)-(3-4))",
            "(((1-2)-3)-4)",
            "(1-(2-(3-4)))",
            "((1-(2-3))-4)",
            "(1-((2-3)-4))",
            "((1^2)^(3^4))",
            "(((1^2)^3)^4)",
            "(1^(2^(3^4)))",
            "((1^(2^3))^4)",
            "(1^((2^3)^4))",
        };

        [TestCaseSource(typeof(ExpressionTransformerTests), nameof(ExpressionTransformerTests.ExpressionTestCases))]
        public void IteratedParseAndTransformToString_Always_ReturnsTheSameExpression(string input)
        {
            var allInput = new HashSet<string>();
            var allParsed = new HashSet<string>();
            string previousInput = null;
            string previousParsed = null;

            var sb = new StringBuilder().AppendLine(input);
            var parser = new Parser();
            allInput.Add(previousInput = input);

            while (true)
            {
                var expression = parser.Parse(input);
                var parsed = expression.ToString();
                sb.AppendLine("(parsed)");
                sb.AppendLine(parsed);

                if (parsed == previousParsed)
                {
                    sb.AppendLine("(stable)");
                    break;
                }
                else if (!allParsed.Add(parsed))
                {
                    sb.AppendLine("(astable)");
                    break;
                }
                else
                {
                    previousParsed = parsed;
                }

                input = expression.TransformToString();
                sb.AppendLine("(transformed)");
                sb.AppendLine(input);

                if (input == previousInput)
                {
                    sb.AppendLine("(stable)");
                    break;
                }
                else if (!allInput.Add(input))
                {
                    sb.AppendLine("(astable)");
                    break;
                }
                else
                {
                    previousInput = input;
                }
            }

            WriteAndAssertResult(sb.ToString());
        }

        private static string SanitizeName(string testName)
        {
            return Regex.Replace(testName.Replace('"', '\'').Replace('*', '×').Replace('/', '÷'), "[" + Regex.Escape(new string(Path.GetInvalidFileNameChars())) + "]", "_");
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
            Assert.That(File.Exists(expectedPath), Is.True);
            var expected = File.ReadAllText(expectedPath);
            Assert.That(contents, Is.EqualTo(expected));
        }
    }
}
