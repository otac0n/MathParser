// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser.Drawing
{
    using System;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Runtime.Versioning;
    using MathParser.Drawing.VisualNodes;

    /// <summary>
    /// Converts expressions to their visual representation. Can be overridden.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class VisualNodeTransformer : ExpressionTransformer<VisualNode>
    {
        /// <inheritdoc />
        protected override VisualNode AddBrackets(string left, VisualNode expression, string right) => new BracketedVisualNode(left, expression, right);

        /// <inheritdoc />
        protected override VisualNode CreateAdd(VisualNode augend, VisualNode addend) => CreateInlineBinary(augend, "+", addend);

        /// <inheritdoc />
        protected override VisualNode CreateDivide(VisualNode dividend, VisualNode divisor) => new FractionVisualNode(dividend, divisor);

        /// <inheritdoc />
        protected override VisualNode CreateMultiply(VisualNode multiplier, VisualNode multiplicand) => CreateInlineBinary(multiplier, "·", multiplicand);

        /// <inheritdoc />
        protected override VisualNode CreateNegate(VisualNode expression) => new BaselineAlignedVisualNode(new StringVisualNode("-"), expression);

        /// <inheritdoc />
        protected override VisualNode CreatePower(VisualNode @base, VisualNode exponent) => new PowerVisualNode(@base, exponent);

        /// <inheritdoc />
        protected override VisualNode CreateSubtract(VisualNode minuend, VisualNode subtrahend) => CreateInlineBinary(minuend, "-", subtrahend);

        /// <inheritdoc />
        protected override VisualNode CreateConditional(VisualNode condition, VisualNode consequent, VisualNode alternative) => AddBrackets("{", new TableVisualNode(new[,] { { consequent, new BaselineAlignedVisualNode(new StringVisualNode("if "), condition) }, { alternative, new StringVisualNode("otherwise") } }), null);

        /// <inheritdoc />
        protected override VisualNode CreateFunction(string name, params VisualNode[] arguments)
        {
            var argumentNodes = Enumerable.Range(0, arguments.Length * 2 - 1).Select(i => i % 2 == 0 ? arguments[i / 2] : new StringVisualNode(",")).ToArray();
            return new BaselineAlignedVisualNode(new StringVisualNode(name), new BracketedVisualNode("(", new BaselineAlignedVisualNode(argumentNodes), ")"));
        }

        /// <inheritdoc />
        protected override VisualNode CreateRadical(VisualNode expression) => new RadicalVisualNode(expression);

        /// <inheritdoc />
        protected override VisualNode CreateEquality(VisualNode left, ExpressionType op, VisualNode right) => new BaselineAlignedVisualNode(left, new StringVisualNode(FormatEqualityOperator(op)), right);

        /// <inheritdoc />
        protected override VisualNode CreateLambda(string name, VisualNode[] parameters, VisualNode body)
        {
            var argumentNodes = Enumerable.Range(0, parameters.Length * 2 - 1).Select(i => i % 2 == 0 ? parameters[i / 2] : new StringVisualNode(",")).ToArray();
            return new BaselineAlignedVisualNode(new StringVisualNode(name), new BracketedVisualNode("(", new BaselineAlignedVisualNode(argumentNodes), ")"), new StringVisualNode(FormatEqualityOperator(ExpressionType.Equal)), body);
        }

        /// <inheritdoc />
        protected override VisualNode FormatReal(double real) => new StringVisualNode(NumberFormatter.FormatReal(real));

        /// <inheritdoc />
        protected override VisualNode FormatComplex(double real, double imaginary) => new StringVisualNode(NumberFormatter.FormatComplexNumber(real, imaginary));

        /// <inheritdoc />
        protected override VisualNode FormatVariable(string name) => new StringVisualNode(name);

        /// <inheritdoc />
        protected override ExpressionType GetEffectiveTypeReal(double real) => NumberFormatter.GetEffectiveTypeReal(real);

        /// <inheritdoc />
        protected override ExpressionType GetEffectiveTypeComplex(double real, double imaginary) => NumberFormatter.GetEffectiveTypeComplex(real, imaginary);

        /// <inheritdoc />
        protected override bool NeedsLeftBrackets(ExpressionType outerEffectiveType, Expression outer, ExpressionType innerEffectiveType, Expression inner)
        {
            if (outerEffectiveType == ExpressionType.Power &&
                ((outer is MethodCallExpression outerMethod && outerMethod.Method.Name == nameof(Math.Sqrt)) || (inner is MethodCallExpression innerMethod && innerMethod.Method.Name == nameof(Math.Sqrt))))
            {
                return false;
            }

            if (outerEffectiveType == ExpressionType.Divide)
            {
                // A fraction does not need parentheses around the dividend.
                return false;
            }

            return base.NeedsLeftBrackets(outerEffectiveType, outer, innerEffectiveType, inner);
        }

        /// <inheritdoc />
        protected override bool NeedsRightBrackets(ExpressionType outerEffectiveType, Expression outer, ExpressionType innerEffectiveType, Expression inner)
        {
            if (outerEffectiveType == ExpressionType.Power)
            {
                return false;
            }

            if (outerEffectiveType == ExpressionType.Divide)
            {
                // A fraction does not need parentheses around the divisor.
                return false;
            }

            if (outerEffectiveType == ExpressionType.Negate &&
                innerEffectiveType == ExpressionType.Divide)
            {
                // Negating a fraction does not requre parentheses around the fraction.
                return false;
            }

            return base.NeedsRightBrackets(outerEffectiveType, outer, innerEffectiveType, inner);
        }

        /// <summary>
        /// Formats an equality operator as a string.
        /// </summary>
        /// <param name="op">The equality operator.</param>
        /// <returns>The string representation of the operator.</returns>
        private static string FormatEqualityOperator(ExpressionType op) =>
            op switch
            {
                ExpressionType.Equal => "=",
                ExpressionType.NotEqual => "≠",
                ExpressionType.GreaterThan => ">",
                ExpressionType.GreaterThanOrEqual => "≥",
                ExpressionType.LessThan => "<",
                ExpressionType.LessThanOrEqual => "≤",
            };

        private static VisualNode CreateInlineBinary(VisualNode left, string op, VisualNode right) => new BaselineAlignedVisualNode(left, new StringVisualNode(op), right);
    }
}
