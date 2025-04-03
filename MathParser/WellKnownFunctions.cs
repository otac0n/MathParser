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
        /// Basic Arithmetic functions.
        /// </summary>
        public static class Arithmetic
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
        }

        /// <summary>
        /// Exponential functions.
        /// </summary>
        public static class Exponential
        {
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

        /// <summary>
        /// Primitive piecewise functions.
        /// </summary>
        public static class Piecewise
        {
            /// <summary>
            /// Gets an object that represents the absolute value function.
            /// </summary>
            /// <remarks>
            /// Implemented by <see cref="Math.Abs(double)"/>.
            /// </remarks>
            public static KnownFunction Abs { get; } = new("abs");

            /// <summary>
            /// Gets an object that represents the sign function.
            /// </summary>
            /// <remarks>
            /// Implemented by <see cref="Math.Sign(double)"/>.
            /// </remarks>
            public static KnownFunction Sign { get; } = new("sgn");

            /// <summary>
            /// Gets an object that represents the ceiling function.
            /// </summary>
            /// <remarks>
            /// Implemented by <see cref="Math.Ceiling(double)"/>.
            /// </remarks>
            public static KnownFunction Ceiling { get; } = new("ceil");

            /// <summary>
            /// Gets an object that represents the floor function.
            /// </summary>
            /// <remarks>
            /// Implemented by <see cref="Math.Floor(double)"/>.
            /// </remarks>
            public static KnownFunction Floor { get; } = new("floor");
        }

        /// <summary>
        /// Trigonometric functions.
        /// </summary>
        public static class Trigonometric
        {
            /// <summary>
            /// Gets an object that represents the sine function.
            /// </summary>
            /// <remarks>Implemented by <see cref="Math.Sin"/>.</remarks>
            public static KnownFunction Sine { get; } = new("sin");

            /// <summary>
            /// Gets an object that represents the cosine function.
            /// </summary>
            /// <remarks>Implemented by <see cref="Math.Cos"/>.</remarks>
            public static KnownFunction Cosine { get; } = new("cos");

            /// <summary>
            /// Gets an object that represents the tangent function.
            /// </summary>
            /// <remarks>Implemented by <see cref="Math.Tan"/>.</remarks>
            public static KnownFunction Tangent { get; } = new("tan");
        }

        /// <summary>
        /// Hyperbolic functions.
        /// </summary>
        public static class Hyperbolic
        {
            /// <summary>
            /// Gets an object that represents the hyperbolic sine function.
            /// </summary>
            /// <remarks>Implemented by <see cref="Math.Sinh"/>.</remarks>
            public static KnownFunction Sine { get; } = new("sinh");

            /// <summary>
            /// Gets an object that represents the hyperbolic cosine function.
            /// </summary>
            /// <remarks>Implemented by <see cref="Math.Cosh"/>.</remarks>
            public static KnownFunction Cosine { get; } = new("cosh");

            /// <summary>
            /// Gets an object that represents the hyperbolic tangent function.
            /// </summary>
            /// <remarks>Implemented by <see cref="Math.Tanh"/>.</remarks>
            public static KnownFunction Tangent { get; } = new("tanh");
        }
    }
}
