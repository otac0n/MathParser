// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser
{
    using System;
    using System.Globalization;
    using System.Linq.Expressions;
    using System.Numerics;
    using MathParser.VisualNodes;

    /// <summary>
    /// Provides default implementations of the <see cref="ExpressionTransformer{T}"/> class.
    /// </summary>
    public static class ExpressionTransformers
    {
        /// <summary>
        /// Converts a complex value to its string representation.
        /// </summary>
        /// <param name="value">The complex value.</param>
        /// <returns>The string representation of the complex value.</returns>
        public static string TransformToString(this Complex value)
        {
            return StringTransformer.FormatComplexExported(value.Real, value.Imaginary);
        }

        /// <summary>
        /// Converts a real value to its string representation.
        /// </summary>
        /// <param name="value">The real value.</param>
        /// <returns>The string representation of the real value.</returns>
        public static string TransformToString(this double value)
        {
            return StringTransformer.FormatComplexExported(value, 0);
        }

        /// <summary>
        /// Converts an expression to its string representation.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns>The string representation of the expression.</returns>
        public static string TransformToString(this Expression expression)
        {
            var transformer = new StringTransformer();
            transformer.Visit(expression);
            return transformer.Result;
        }

        /// <summary>
        /// Converts an expression to its visual representation.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns>The visual representation of the expression.</returns>
        public static VisualNode TransformToVisualTree(this Expression expression)
        {
            var transformer = new VisualNodeTransformer();
            transformer.Visit(expression);
            return transformer.Result;
        }

        /// <summary>
        /// Converts expressions to their string representation. Can be overridden.
        /// </summary>
        public class StringTransformer : ExpressionTransformer<string>
        {
            /// <summary>
            /// Formats a complex number as a string.
            /// </summary>
            /// <param name="real">The real part.</param>
            /// <param name="imaginary">The imaginary part.</param>
            /// <returns>The string representation of the complex number.</returns>
            protected internal static string FormatComplexExported(double real, double imaginary)
            {
                var emptyReal = double.IsNaN(real) || real == 0;
                var emptyImaginary = double.IsNaN(imaginary) || imaginary == 0;

                if (emptyReal && emptyImaginary)
                {
                    return FormatReal(real);
                }

                var realPart = emptyReal ? string.Empty : FormatReal(real);
                var imaginaryPart = emptyImaginary
                    ? string.Empty
                    : (imaginary == 1 ? string.Empty : FormatReal(imaginary)) + "i";

                return realPart + (!emptyReal && imaginary > 0 ? "+" : string.Empty) + imaginaryPart;
            }

            /// <summary>
            /// Gets the effective type of the complex number's notation when converted using <see cref="FormatComplexExported(double, double)"/>.
            /// </summary>
            /// <param name="real">The real part.</param>
            /// <param name="imaginary">The imaginary part.</param>
            /// <returns>The effective expression type.</returns>
            protected internal static ExpressionType GetEffectiveTypeComplexInternal(double real, double imaginary) =>
                real != 0 && imaginary != 0
                    ? ExpressionType.Add
                    : real != 0
                        ? GetEffectiveTypeReal(real)
                        : imaginary == 1
                            ? ExpressionType.Parameter
                            : ExpressionType.Multiply;

            /// <inheritdoc />
            protected override string AddBrackets(string expression) => "(" + expression + ")";

            /// <inheritdoc />
            protected override string CreateAdd(string augend, string addend) => augend + "+" + addend;

            /// <inheritdoc />
            protected override string CreateDivide(string dividend, string divisor) => dividend + "/" + divisor;

            /// <inheritdoc />
            protected override string CreateMultiply(string multiplier, string multiplicand) => multiplier + " " + multiplicand;

            /// <inheritdoc />
            protected override string CreateNegate(string expression) => "-" + expression;

            /// <inheritdoc />
            protected override string CreatePower(string @base, string exponent) => @base + "^" + exponent;

            /// <inheritdoc />
            protected override string CreateSubtract(string minuend, string subtrahend) => minuend + "-" + subtrahend;

            /// <inheritdoc />
            protected override string FormatComplex(double real, double imaginary) => FormatComplexExported(real, imaginary);

            /// <inheritdoc />
            protected override string FormatVariable(string name) => name;

            /// <inheritdoc />
            protected override ExpressionType GetEffectiveTypeComplex(double real, double imaginary) => GetEffectiveTypeComplexInternal(real, imaginary);

            /// <summary>
            /// Formats a real number as a string.
            /// </summary>
            /// <param name="value">The real number.</param>
            /// <returns>The string representation of the real number.</returns>
            private static string FormatReal(double value) =>
                (value == Math.PI * 2) ? "τ" :
                (value == Math.PI) ? "π" :
                (value == Math.E) ? "e" :
                (value == (1 + Math.Sqrt(5)) / 2) ? "φ" :
                value.ToString("R", CultureInfo.CurrentCulture);

            /// <summary>
            /// Gets the effective type of the real number's notation when converted using <see cref="FormatReal(double)"/>.
            /// </summary>
            /// <param name="value">The real number.</param>
            /// <returns>The effective expression type.</returns>
            private static ExpressionType GetEffectiveTypeReal(double value) =>
                value < 0
                    ? ExpressionType.Negate
                    : ExpressionType.Constant;
        }

        /// <summary>
        /// Converts expressions to their visual representation. Can be overridden.
        /// </summary>
        public class VisualNodeTransformer : ExpressionTransformer<VisualNode>
        {
            /// <inheritdoc />
            protected override VisualNode AddBrackets(VisualNode expression) => new BracketedVisualNode("(", expression, ")");

            /// <inheritdoc />
            protected override VisualNode CreateAdd(VisualNode augend, VisualNode addend) => CreateInlineBinary(augend, "+", addend);

            /// <inheritdoc />
            protected override VisualNode CreateDivide(VisualNode dividend, VisualNode divisor) => CreateInlineBinary(dividend, "÷", divisor);

            /// <inheritdoc />
            protected override VisualNode CreateMultiply(VisualNode multiplier, VisualNode multiplicand) => CreateInlineBinary(multiplier, "·", multiplicand);

            /// <inheritdoc />
            protected override VisualNode CreateNegate(VisualNode expression) => new BaselineAlignedVisualNode(new StringVisualNode("-"), expression);

            /// <inheritdoc />
            protected override VisualNode CreatePower(VisualNode @base, VisualNode exponent) => new PowerVisualNode(@base, exponent);

            /// <inheritdoc />
            protected override VisualNode CreateSubtract(VisualNode minuend, VisualNode subtrahend) => CreateInlineBinary(minuend, "-", subtrahend);

            /// <inheritdoc />
            protected override VisualNode FormatComplex(double real, double imaginary) => new StringVisualNode(StringTransformer.FormatComplexExported(real, imaginary));

            /// <inheritdoc />
            protected override VisualNode FormatVariable(string name) => new StringVisualNode(name);

            /// <inheritdoc />
            protected override ExpressionType GetEffectiveTypeComplex(double real, double imaginary) => StringTransformer.GetEffectiveTypeComplexInternal(real, imaginary);

            /// <inheritdoc />
            protected override bool NeedsRightBrackets(ExpressionType outerEffectiveType, Expression inner) => outerEffectiveType != ExpressionType.Power && base.NeedsRightBrackets(outerEffectiveType, inner);

            private static VisualNode CreateInlineBinary(VisualNode left, string op, VisualNode right) => new BaselineAlignedVisualNode(left, new StringVisualNode(op), right);
        }
    }
}
