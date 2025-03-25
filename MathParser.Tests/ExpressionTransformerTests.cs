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
    using static TestExtensions;

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
            "e^(iτ)-1",
            "√(1+√√√2+3/4+√(1/3)+√(i^2))",
            "m·v/√(1-v^2/c^2)",
            "1/sqrt(τ)e^-(x^2/2)",
            "4v^2/√π(m/(2k T))^(3/2)e^-(m v^2/(2 k T))",
            "((1+i)/(1-i))^3-((1-i)/(1+i))^3",
            "⌈3⌉/⌊2⌋",
            "f(x)=1/x",
            "g(x):=sin(x)*x",
            "y==log(z)",
            "a>b",
            "π≥3",
            "e>=1",
            "x*y<x+y",
            "√(x^2+b^2)≤2",
            "cos(τ)<=sin(x)",
            "1≠2",
        };

        [TestCaseSource(typeof(ExpressionTransformerTests), nameof(ExpressionTransformerTests.ExpressionTestCases))]
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
