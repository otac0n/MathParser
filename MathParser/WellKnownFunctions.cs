// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser
{
    using System;

    /// <summary>
    /// The collection of built-in functions.
    /// </summary>
    public static class WellKnownFunctions
    {
        /// <summary>
        /// Gets an object that represents the negation function.
        /// </summary>
        /// <remarks>
        /// The unary `-` operator.
        /// </remarks>
        public static KnownFunction Negate { get; } = new("neg");

        /// <summary>
        /// Gets an object that represents the addition function.
        /// </summary>
        /// <remarks>
        /// The binary `+` operator.
        /// </remarks>
        public static KnownFunction Add { get; } = new("add");

        /// <summary>
        /// Gets an object that represents the subtraction function.
        /// </summary>
        /// <remarks>
        /// The binary `-` operator.
        /// </remarks>
        public static KnownFunction Subtract { get; } = new("sub");

        /// <summary>
        /// Gets an object that represents the multiplication function.
        /// </summary>
        /// <remarks>
        /// The binary `*` operator.
        /// </remarks>
        public static KnownFunction Multiply { get; } = new("mul");

        /// <summary>
        /// Gets an object that represents the division function.
        /// </summary>
        /// <remarks>
        /// The binary `/` operator.
        /// </remarks>
        public static KnownFunction Divide { get; } = new("div");

        /// <summary>
        /// Gets an object that represents the power function.
        /// </summary>
        /// <remarks>
        /// The binary `^` operator. Implemented by <see cref="Math.Pow"/>.
        /// </remarks>
        public static KnownFunction Pow { get; } = new("pow");

        /// <summary>
        /// Gets an object that represents the exp function.
        /// </summary>
        /// <remarks><see cref="Math.E"/> to the <see cref="Pow">power</see> of the provided argument.</remarks>
        public static KnownFunction Exp { get; } = new("exp");

        /// <summary>
        /// Gets an object that represents the square root function.
        /// </summary>
        /// <remarks>
        /// The unary `√` operator. Implemented by <see cref="Math.Sqrt"/>.
        /// </remarks>
        public static KnownFunction Sqrt { get; } = new("sqrt");

        /// <summary>
        /// Gets an object that represents the ln function.
        /// </summary>
        /// <remarks>The logarithm base <see cref="Math.E"/> of the provided argument. Implemented by <see cref="Math.Log(double)"/>.</remarks>
        public static KnownFunction Ln { get; } = new("ln");
    }
}
