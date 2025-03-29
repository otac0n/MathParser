// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser.Tests
{
    using System;
    using System.Linq.Expressions;
    using System.Text;
    using MathParser.Testing;
    using NUnit.Framework;

    [TestFixture]
    internal class OperationsTests
    {
        [TestCaseSource(typeof(TestData), nameof(TestData.LambdaExpressions))]
        public void Derivative_Always_ReturnsAnApprovedExpression(Expression<Func<double, double>> input)
        {
            var result = new StringBuilder();
            result.AppendLine(input.TransformToString());

            var current = input;
            for (var i = 0; i < 3; i++)
            {
                var next = Operations.Derivative(current);
                result.AppendLine(next.TransformToString());
                current = next;
            }

            WriteAndAssertResult(result.ToString());
        }

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
            TestContext.CurrentContext.ApproveFromFile(contents, (expected, actual) =>
                Assert.That(actual, Is.EqualTo(expected)));
        }
    }
}
