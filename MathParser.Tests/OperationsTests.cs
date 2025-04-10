// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser.Tests
{
    using System.Linq.Expressions;
    using System.Text;
    using MathParser.Testing;
    using MathParser.Text;
    using NUnit.Framework;

    [TestFixture]
    public class OperationsTests
    {
        [TestCaseSource(typeof(TestData), nameof(TestData.LambdaExpressions))]
        public void Derivative_Always_ReturnsAnApprovedExpression(LambdaExpression input)
        {
            var scope = DefaultScope.Instance;

            var result = new StringBuilder();
            result.AppendLine(input.TransformToString(scope));

            var current = input;
            for (var i = 0; i < 3; i++)
            {
                var next = scope.Derivative(current);
                result.AppendLine(next.TransformToString(scope));
                current = next;
            }

            WriteAndAssertResult(result.ToString());
        }

        [TestCaseSource(typeof(TestData), nameof(TestData.SimplifyStrings))]
        public void Simplify_Always_ReturnsTheExpectedExpression(string input)
        {
            var scope = DefaultScope.Instance;

            var parser = new Parser(scope);
            var expression = parser.Parse(input);

            var simplified = scope.Simplify(expression);
            var result = simplified.TransformToString(scope);

            WriteAndAssertResult(result);
        }

        private static void WriteAndAssertResult(string contents)
        {
            TestContext.CurrentContext.ApproveFromFile(contents, (expected, actual) =>
                Assert.That(actual, Is.EqualTo(expected)));
        }
    }
}
