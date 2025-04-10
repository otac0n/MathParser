// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser.Text
{
    using System;
    using System.Linq.Expressions;

    /// <summary>
    /// Provides basic operator formatting.
    /// </summary>
    public class OperatorFormatter
    {
        /// <summary>
        /// Formats an operator as a string.
        /// </summary>
        /// <param name="op">The operator.</param>
        /// <returns>The string representation of the operator.</returns>
        public static string FormatOperator(ExpressionType op) =>
            op switch
            {
                ExpressionType.Not => "!",
                ExpressionType.And => "and",
                ExpressionType.Or => "or",
                ExpressionType.Negate => "-",
                ExpressionType.Add => "+",
                ExpressionType.Subtract => "-",
                ExpressionType.Multiply => "·",
                ExpressionType.Divide => "/",
                ExpressionType.Power => "^",
                ExpressionType.Equal => "=",
                ExpressionType.NotEqual => "≠",
                ExpressionType.GreaterThan => ">",
                ExpressionType.GreaterThanOrEqual => "≥",
                ExpressionType.LessThan => "<",
                ExpressionType.LessThanOrEqual => "≤",
                _ => throw new NotSupportedException(),
            };
    }
}
