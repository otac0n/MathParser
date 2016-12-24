// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser
{
    using System;
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
            return StringTransformer.FormatRealExported(value);
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
                    return FormatRealExported(real);
                }

                var realPart = emptyReal ? string.Empty : FormatRealExported(real);
                var imaginaryPart = emptyImaginary
                    ? string.Empty
                    : (imaginary == 1 ? string.Empty : FormatRealExported(imaginary)) + "i";

                return realPart + (!emptyReal && imaginary > 0 ? "+" : string.Empty) + imaginaryPart;
            }

            /// <summary>
            /// Formats a real number as a string.
            /// </summary>
            /// <param name="value">The real number.</param>
            /// <returns>The string representation of the real number.</returns>
            protected internal static string FormatRealExported(double value) =>
                (value == Math.PI * 2) ? "τ" :
                (value == Math.PI) ? "π" :
                (value == Math.E) ? "e" :
                (value == (1 + Math.Sqrt(5)) / 2) ? "φ" :
                value.ToString("R");

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
                        ? GetEffectiveTypeRealExported(real)
                        : imaginary == 1
                            ? ExpressionType.Parameter
                            : ExpressionType.Multiply;

            /// <summary>
            /// Gets the effective type of the real number's notation when converted using <see cref="FormatRealExported(double)"/>.
            /// </summary>
            /// <param name="value">The real number.</param>
            /// <returns>The effective expression type.</returns>
            protected internal static ExpressionType GetEffectiveTypeRealExported(double value) =>
                value < 0
                    ? ExpressionType.Negate
                    : ExpressionType.Constant;

            /// <inheritdoc />
            protected override string AddBrackets(string node) => "(" + node + ")";

            /// <inheritdoc />
            protected override string CreateAdd(string left, string right) => left + "+" + right;

            /// <inheritdoc />
            protected override string CreateDivide(string left, string right) => left + "*" + right;

            /// <inheritdoc />
            protected override string CreateMultiply(string left, string right) => left + "*" + right;

            /// <inheritdoc />
            protected override string CreateNegate(string node) => "-" + node;

            /// <inheritdoc />
            protected override string CreatePower(string @base, string exponent) => @base + "^" + exponent;

            /// <inheritdoc />
            protected override string CreateSubtract(string left, string right) => left + "-" + right;

            /// <inheritdoc />
            protected override string FormatComplex(double real, double imaginary) => FormatComplexExported(real, imaginary);

            /// <inheritdoc />
            protected override string FormatReal(double value) => FormatRealExported(value);

            /// <inheritdoc />
            protected override string FormatVariable(string name) => name;

            /// <inheritdoc />
            protected override ExpressionType GetEffectiveTypeComplex(double real, double imaginary) => GetEffectiveTypeComplexInternal(real, imaginary);

            /// <inheritdoc />
            protected override ExpressionType GetEffectiveTypeReal(double value) => GetEffectiveTypeRealExported(value);
        }

        /// <summary>
        /// Converts expressions to their visual representation. Can be overridden.
        /// </summary>
        public class VisualNodeTransformer : ExpressionTransformer<VisualNode>
        {
            /// <inheritdoc />
            protected override VisualNode AddBrackets(VisualNode node) => new BracketedVisualNode("(", node, ")");

            /// <inheritdoc />
            protected override VisualNode CreateAdd(VisualNode left, VisualNode right) => CreateInlineBinary(left, "+", right);

            /// <inheritdoc />
            protected override VisualNode CreateDivide(VisualNode left, VisualNode right) => CreateInlineBinary(left, "÷", right);

            /// <inheritdoc />
            protected override VisualNode CreateMultiply(VisualNode left, VisualNode right) => CreateInlineBinary(left, "·", right);

            /// <inheritdoc />
            protected override VisualNode CreateNegate(VisualNode node) => new BaselineAlignedVisualNode(new StringVisualNode("-"), node);

            /// <inheritdoc />
            protected override VisualNode CreatePower(VisualNode @base, VisualNode exponent) => new PowerVisualNode(@base, exponent);

            /// <inheritdoc />
            protected override VisualNode CreateSubtract(VisualNode left, VisualNode right) => CreateInlineBinary(left, "-", right);

            /// <inheritdoc />
            protected override VisualNode FormatComplex(double real, double imaginary) => new StringVisualNode(StringTransformer.FormatComplexExported(real, imaginary));

            /// <inheritdoc />
            protected override VisualNode FormatReal(double value) => new StringVisualNode(StringTransformer.FormatRealExported(value));

            /// <inheritdoc />
            protected override VisualNode FormatVariable(string name) => new StringVisualNode(name);

            /// <inheritdoc />
            protected override ExpressionType GetEffectiveTypeComplex(double real, double imaginary) => StringTransformer.GetEffectiveTypeComplexInternal(real, imaginary);

            /// <inheritdoc />
            protected override ExpressionType GetEffectiveTypeReal(double value) => StringTransformer.GetEffectiveTypeRealExported(value);

            /// <inheritdoc />
            protected override bool NeedsRightBrackets(ExpressionType outerSimpleType, Expression inner) => outerSimpleType != ExpressionType.Power && base.NeedsRightBrackets(outerSimpleType, inner);

            private static VisualNode CreateInlineBinary(VisualNode left, string op, VisualNode right) => new BaselineAlignedVisualNode(left, new StringVisualNode(op), right);
        }
    }
}
