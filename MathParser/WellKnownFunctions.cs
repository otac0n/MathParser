// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Numerics;

    /// <summary>
    /// The collection of built-in functions.
    /// </summary>
    public static class WellKnownFunctions
    {
        /// <summary>
        /// Gets the mapping from <see cref="WellKnownFunctions"/> to <see cref="ExpressionType"/>.
        /// </summary>
        public static readonly IDictionary<KnownFunction, ExpressionType> ExpressionTypeLookup = new Dictionary<KnownFunction, ExpressionType>()
        {
            [Arithmetic.Negate] = ExpressionType.Negate,
            [Arithmetic.Add] = ExpressionType.Add,
            [Arithmetic.Subtract] = ExpressionType.Subtract,
            [Arithmetic.Multiply] = ExpressionType.Multiply,
            [Arithmetic.Divide] = ExpressionType.Divide,
            [Exponential.Pow] = ExpressionType.Power,
            [Comparison.Equal] = ExpressionType.Equal,
            [Comparison.NotEqual] = ExpressionType.NotEqual,
            [Comparison.GreaterThan] = ExpressionType.GreaterThan,
            [Comparison.GreaterThanOrEqual] = ExpressionType.GreaterThanOrEqual,
            [Comparison.LessThan] = ExpressionType.LessThan,
            [Comparison.LessThanOrEqual] = ExpressionType.LessThanOrEqual,
            [Boolean.Not] = ExpressionType.Not,
            [Boolean.And] = ExpressionType.And,
            [Boolean.Or] = ExpressionType.Or,
            [Boolean.ExclusiveOr] = ExpressionType.ExclusiveOr,
        }.AsReadOnly();

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

            /// <summary>
            /// Gets an object that represents the reciprocal function.
            /// </summary>
            /// <remarks>
            /// The `1/x` function. Implemented by <see cref="Math.ReciprocalEstimate"/>.
            /// </remarks>
            public static KnownFunction Reciprocal { get; } = new("inv");
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
        /// Comparison functions.
        /// </summary>
        public static class Comparison
        {
            /// <summary>
            /// Gets an object that represents the equality function.
            /// </summary>
            /// <remarks>
            /// The `=` operator.
            /// </remarks>
            public static KnownFunction Equal { get; } = new("eq");

            /// <summary>
            /// Gets an object that represents the inequality function.
            /// </summary>
            /// <remarks>
            /// The `!=` operator.
            /// </remarks>
            public static KnownFunction NotEqual { get; } = new("neq");

            /// <summary>
            /// Gets an object that represents the greater-than function.
            /// </summary>
            /// <remarks>
            /// The `>` operator.
            /// </remarks>
            public static KnownFunction GreaterThan { get; } = new("gt");

            /// <summary>
            /// Gets an object that represents the greater-than-or-equal function.
            /// </summary>
            /// <remarks>
            /// The `>=` operator.
            /// </remarks>
            public static KnownFunction GreaterThanOrEqual { get; } = new("gte");

            /// <summary>
            /// Gets an object that represents the less-than function.
            /// </summary>
            /// <remarks>
            /// The `&lt;` operator.
            /// </remarks>
            public static KnownFunction LessThan { get; } = new("lt");

            /// <summary>
            /// Gets an object that represents the less-than-or-equal function.
            /// </summary>
            /// <remarks>
            /// The `&lt;=` operator.
            /// </remarks>
            public static KnownFunction LessThanOrEqual { get; } = new("lte");
        }

        /// <summary>
        /// Comparison functions.
        /// </summary>
        public static class Boolean
        {
            /// <summary>
            /// Gets an object that represents the NOT function.
            /// </summary>
            /// <remarks>
            /// The logical NOT operator.
            /// </remarks>
            public static KnownFunction Not { get; } = new("not");

            /// <summary>
            /// Gets an object that represents the AND function.
            /// </summary>
            /// <remarks>
            /// The logical AND operator.
            /// </remarks>
            public static KnownFunction And { get; } = new("and");

            /// <summary>
            /// Gets an object that represents the OR function.
            /// </summary>
            /// <remarks>
            /// The logical OR operator.
            /// </remarks>
            public static KnownFunction Or { get; } = new("or");

            /// <summary>
            /// Gets an object that represents the XOR function.
            /// </summary>
            /// <remarks>
            /// The logical XOR operator.
            /// </remarks>
            public static KnownFunction ExclusiveOr { get; } = new("xor");
        }

        /// <summary>
        /// Complex maniplation functions.
        /// </summary>
        public static class Complex
        {
            /// <summary>
            /// Gets an object that represents the real-part function.
            /// </summary>
            /// <remarks>
            /// Implemented by <see cref="System.Numerics.Complex.Real"/>.
            /// </remarks>
            public static KnownFunction RealPart { get; } = new("Re");

            /// <summary>
            /// Gets an object that represents the imaginary-part function.
            /// </summary>
            /// <remarks>
            /// Implemented by <see cref="System.Numerics.Complex.Imaginary"/>.
            /// </remarks>
            public static KnownFunction ImaginaryPart { get; } = new("Im");

            /// <summary>
            /// Gets an object that represents the argument function.
            /// </summary>
            /// <remarks>
            /// Implemented by <see cref="System.Numerics.Complex.Phase"/>.
            /// </remarks>
            public static KnownFunction Argument { get; } = new("arg");

            /// <summary>
            /// Gets an object that represents the conjugate function.
            /// </summary>
            /// <remarks>
            /// Implemented by <see cref="System.Numerics.Complex.Conjugate"/>.
            /// </remarks>
            public static KnownFunction Conjugate { get; } = new("conj");
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

            /// <summary>
            /// Gets an object that represents the arcsine function.
            /// </summary>
            /// <remarks>Implemented by <see cref="Math.Asin"/>.</remarks>
            public static KnownFunction Arcsine { get; } = new("asin");

            /// <summary>
            /// Gets an object that represents the arccosine function.
            /// </summary>
            /// <remarks>Implemented by <see cref="Math.Acos"/>.</remarks>
            public static KnownFunction Arcosine { get; } = new("acos");

            /// <summary>
            /// Gets an object that represents the arctangent function.
            /// </summary>
            /// <remarks>Implemented by <see cref="Math.Atan"/>.</remarks>
            public static KnownFunction Arctangent { get; } = new("atan");
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

            /// <summary>
            /// Gets an object that represents the hyperbolic arcsine function.
            /// </summary>
            /// <remarks>Implemented by <see cref="Math.Asinh"/>.</remarks>
            public static KnownFunction Arcsine { get; } = new("asinh");

            /// <summary>
            /// Gets an object that represents the hyperbolic arccosine function.
            /// </summary>
            /// <remarks>Implemented by <see cref="Math.Acosh"/>.</remarks>
            public static KnownFunction Arcosine { get; } = new("acosh");

            /// <summary>
            /// Gets an object that represents the hyperbolic arctangent function.
            /// </summary>
            /// <remarks>Implemented by <see cref="Math.Atanh"/>.</remarks>
            public static KnownFunction Arctangent { get; } = new("atanh");
        }
    }
}
