// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser
{
    using System.Linq.Expressions;
    using System.Numerics;

    /// <summary>
    /// Provides convenience methods for transforming <see cref="Expression">expressions</see>.
    /// </summary>
    public static class ExpressionExtensions
    {
        /// <summary>
        /// Converts a complex value to its string representation.
        /// </summary>
        /// <param name="value">The complex value.</param>
        /// <returns>The string representation of the complex value.</returns>
        public static string TransformToString(this Complex value) =>
            NumberFormatter.FormatComplexNumber(value.Real, value.Imaginary);

        /// <summary>
        /// Converts a real value to its string representation.
        /// </summary>
        /// <param name="value">The real value.</param>
        /// <returns>The string representation of the real value.</returns>
        public static string TransformToString(this double value) =>
            NumberFormatter.FormatReal(value);

        /// <summary>
        /// Converts an expression to its string representation.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="scope">The scope in which the transformations are performed.</param>
        /// <returns>The string representation of the expression.</returns>
        public static string TransformToString(this Expression expression, Scope? scope = null)
        {
            var transformer = new StringTransformer(scope ?? DefaultScope.Instance);
            transformer.Visit(expression);
            return transformer.Result;
        }
    }
}
