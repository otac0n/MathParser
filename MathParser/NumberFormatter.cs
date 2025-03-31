namespace MathParser
{
    using System;
    using System.Globalization;
    using System.Linq.Expressions;

    /// <summary>
    /// Provides basic number formatting.
    /// </summary>
    public class NumberFormatter
    {
        /// <summary>
        /// Gets the custom format string used for formatting numbers in full decimal form.
        /// </summary>
        public static readonly string DoubleFormat = "0." + new string('#', 339);

        /// <summary>
        /// Formats a real number as a string.
        /// </summary>
        /// <param name="value">The real number.</param>
        /// <returns>The string representation of the real number.</returns>
        public static string FormatReal(double value) =>
            (value == Math.PI * 2) ? "τ" :
            (value == Math.PI) ? "π" :
            (value == Math.E) ? "e" :
            (value == (1 + Math.Sqrt(5)) / 2) ? "φ" :
            value.ToString(DoubleFormat, CultureInfo.CurrentCulture);

        /// <summary>
        /// Formats a complex number as a string.
        /// </summary>
        /// <param name="real">The real part.</param>
        /// <param name="imaginary">The imaginary part.</param>
        /// <returns>The string representation of the complex number.</returns>
        public static string FormatComplexNumber(double real, double imaginary)
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
        /// Gets the effective type of the real number's notation when converted using <see cref="FormatReal(double)"/>.
        /// </summary>
        /// <param name="value">The real number.</param>
        /// <returns>The effective expression type.</returns>
        public static ExpressionType GetEffectiveTypeReal(double value) =>
            value < 0
                ? ExpressionType.Negate
                : ExpressionType.Constant;

        /// <summary>
        /// Gets the effective type of the complex number's notation when converted using <see cref="FormatComplexNumber(double, double)"/>.
        /// </summary>
        /// <param name="real">The real part.</param>
        /// <param name="imaginary">The imaginary part.</param>
        /// <returns>The effective expression type.</returns>
        public static ExpressionType GetEffectiveTypeComplex(double real, double imaginary) =>
            real != 0 && imaginary != 0
                ? ExpressionType.Add
                : imaginary == 0
                    ? GetEffectiveTypeReal(real)
                    : imaginary == 1
                        ? ExpressionType.Parameter
                        : ExpressionType.Multiply;
    }
}
