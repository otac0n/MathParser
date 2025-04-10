// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser.Drawing
{
    using System;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Runtime.Versioning;
    using MathParser.Drawing.VisualNodes;
    using MathParser.Text;

    /// <summary>
    /// Converts expressions to their visual representation. Can be overridden.
    /// </summary>
    /// <param name="scope">The scope in which the transformations are performed.</param>
    [SupportedOSPlatform("windows")]
    public class VisualNodeTransformer(Scope scope) : ExpressionTransformer<VisualNode>(scope)
    {
        /// <inheritdoc />
        protected override VisualNode AddBrackets(string left, VisualNode expression, string right) => new BracketedVisualNode(left, expression, right);

        /// <inheritdoc />
        protected override VisualNode CreateNot(VisualNode expression) => new BaselineAlignedVisualNode(new StringVisualNode("¬"), expression);

        /// <inheritdoc />
        protected override VisualNode CreateAnd(VisualNode left, VisualNode right) => CreateInlineBinary(left, "∧", right);

        /// <inheritdoc />
        protected override VisualNode CreateOr(VisualNode left, VisualNode right) => CreateInlineBinary(left, "∨", right);

        /// <inheritdoc />
        protected override VisualNode CreateAdd(VisualNode augend, VisualNode addend) => CreateInlineBinary(augend, "+", addend);

        /// <inheritdoc />
        protected override VisualNode CreateDivide(VisualNode dividend, VisualNode divisor) => new FractionVisualNode(dividend, divisor);

        /// <inheritdoc />
        protected override VisualNode CreateMultiply(VisualNode multiplicand, VisualNode multiplier) => CreateInlineBinary(multiplicand, "·", multiplier);

        /// <inheritdoc />
        protected override VisualNode CreateNegate(VisualNode expression) => new BaselineAlignedVisualNode(new StringVisualNode("-"), expression);

        /// <inheritdoc />
        protected override VisualNode CreatePower(VisualNode @base, VisualNode exponent) => new PowerVisualNode(@base, exponent);

        /// <inheritdoc />
        protected override VisualNode CreateSubtract(VisualNode minuend, VisualNode subtrahend) => CreateInlineBinary(minuend, "-", subtrahend);

        /// <inheritdoc />
        protected override VisualNode CreateConditional((VisualNode condition, VisualNode consequent)[] conditions, VisualNode alternative)
        {
            if (conditions.Length == 1 && alternative == null)
            {
                return new BaselineAlignedVisualNode(conditions[0].consequent, new StringVisualNode("if "), conditions[0].condition);
            }

            var options = new VisualNode[conditions.Length + (alternative == null ? 0 : 1), 2];

            for (var i = 0; i < conditions.Length; i++)
            {
                options[i, 0] = conditions[i].consequent;
                options[i, 1] = new BaselineAlignedVisualNode(new StringVisualNode("if "), conditions[i].condition);
            }

            if (alternative != null)
            {
                options[conditions.Length, 0] = alternative;
                options[conditions.Length, 1] = new StringVisualNode("otherwise");
            }

            return this.AddBrackets("{", new TableVisualNode(options), null);
        }

        /// <inheritdoc />
        protected override VisualNode CreateFunction(string name, params VisualNode[] arguments)
        {
            var argumentNodes = Enumerable.Range(0, arguments.Length * 2 - 1).Select(i => i % 2 == 0 ? arguments[i / 2] : new StringVisualNode(",")).ToArray();
            return new BaselineAlignedVisualNode(new StringVisualNode(name), new BracketedVisualNode("(", new BaselineAlignedVisualNode(argumentNodes), ")"));
        }

        /// <inheritdoc />
        protected override VisualNode CreateRadical(VisualNode expression) => new RadicalVisualNode(expression);

        /// <inheritdoc />
        protected override VisualNode CreateEquality(VisualNode left, ExpressionType op, VisualNode right) => new BaselineAlignedVisualNode(left, new StringVisualNode(OperatorFormatter.FormatOperator(op)), right);

        /// <inheritdoc />
        protected override VisualNode CreateLambda(string name, VisualNode[] parameters, VisualNode body)
        {
            var argumentNodes = Enumerable.Range(0, parameters.Length * 2 - 1).Select(i => i % 2 == 0 ? parameters[i / 2] : new StringVisualNode(",")).ToArray();
            return new BaselineAlignedVisualNode(new StringVisualNode(name), new BracketedVisualNode("(", new BaselineAlignedVisualNode(argumentNodes), ")"), new StringVisualNode(OperatorFormatter.FormatOperator(ExpressionType.Equal)), body);
        }

        /// <inheritdoc />
        protected override VisualNode FormatBoolean(bool boolean) => new StringVisualNode(boolean ? "true" : "false");

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

        /// <inheritdoc/>
        protected override ExpressionType GetRightExposedType(ExpressionType effectiveType, Expression node)
        {
            // A conditional with an alternate doesn't render it's own right bracket.
            if (node is BinaryExpression binary && !(node.NodeType is ExpressionType.Power or ExpressionType.Divide))
            {
                if (binary.Right is ConditionalExpression conditional)
                {
                    if (!this.Scope.IsNaN(conditional.IfFalse))
                    {
                        return ExpressionType.Conditional;
                    }
                }
                else if (this.GetRightExposedType(binary.Right) == ExpressionType.Conditional)
                {
                    return ExpressionType.Conditional;
                }
            }

            return base.GetRightExposedType(effectiveType, node);
        }

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

            if (innerEffectiveType == ExpressionType.Conditional &&
                !this.Scope.MatchConstraint(inner, out _, out _))
            {
                // A conditional with an alternative renders its own left bracket.
                return false;
            }

            return base.NeedsRightBrackets(outerEffectiveType, outer, innerEffectiveType, inner);
        }

        private static VisualNode CreateInlineBinary(VisualNode left, string op, VisualNode right) => new BaselineAlignedVisualNode(left, new StringVisualNode(op), right);
    }
}
