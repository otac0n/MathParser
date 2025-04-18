﻿// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser
{
    /// <summary>
    /// Indicates the order in which operators are parsed.
    /// </summary>
    public enum Precedence : byte
    {
        /// <summary>
        /// No precedence specified.
        /// </summary>
        None = 0,

        /// <summary>
        /// A conditional statement.
        /// </summary>
        Conditional,

        /// <summary>
        /// OR operators.
        /// </summary>
        Disjunction,

        /// <summary>
        /// AND operators.
        /// </summary>
        Conjunction,

        /// <summary>
        /// Equality and Relational operators.
        /// </summary>
        Comparison,

        /// <summary>
        /// Additive operators.
        /// </summary>
        Additive,

        /// <summary>
        /// Multiplicative operators.
        /// </summary>
        Multiplicative,

        /// <summary>
        /// Negation and NOT operators.
        /// </summary>
        Unary,

        /// <summary>
        /// Power operators.
        /// </summary>
        Exponential,

        /// <summary>
        /// Values and function calls.
        /// </summary>
        Primary,

        /// <summary>
        /// Unknown precedence.
        /// </summary>
        Unknown = byte.MaxValue,
    }
}
