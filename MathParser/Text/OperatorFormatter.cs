// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser.Text
{
    using System.Linq.Expressions;

    /// <summary>
    /// Provides basic operator formatting.
    /// </summary>
    public class OperatorFormatter
    {
        /// <summary>
        /// Formats an equality operator as a string.
        /// </summary>
        /// <param name="op">The equality operator.</param>
        /// <returns>The string representation of the operator.</returns>
        public static string FormatEqualityOperator(ExpressionType op) =>
            op switch
            {
                ExpressionType.Equal => "=",
                ExpressionType.NotEqual => "≠",
                ExpressionType.GreaterThan => ">",
                ExpressionType.GreaterThanOrEqual => "≥",
                ExpressionType.LessThan => "<",
                ExpressionType.LessThanOrEqual => "≤",
            };
    }
}
