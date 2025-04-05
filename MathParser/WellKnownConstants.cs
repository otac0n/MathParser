// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser
{
    /// <summary>
    /// The collection of built-in constants.
    /// </summary>
    public static class WellKnownConstants
    {
        /// <summary>
        /// 0, the additive identity.
        /// </summary>
        public static KnownConstant Zero = new("0");

        /// <summary>
        /// 1, the multiplicative identity.
        /// </summary>
        public static KnownConstant One = new("1");

        /// <summary>
        /// -1, the multiplicative identity.
        /// </summary>
        public static KnownConstant NegativeOne = new("-1");

        /// <summary>
        /// i, the imaginary constant.
        /// </summary>
        public static KnownConstant I = new("i");

        /// <summary>
        /// φ, the golden ratio.
        /// </summary>
        public static KnownConstant GoldenRatio = new("φ");

        /// <summary>
        /// e, Euler's number.
        /// </summary>
        public static KnownConstant EulersNumber = new("e");

        /// <summary>
        /// π, half the ratio of a circle's circumference to it's radius.
        /// </summary>
        public static KnownConstant Pi = new("π");

        /// <summary>
        /// τ, the ratio of a circle's circumference to it's radius.
        /// </summary>
        public static KnownConstant Tau = new("τ");

        /// <summary>
        /// ∞, the smallest number greater than every real number.
        /// </summary>
        public static KnownConstant PositiveInfinity = new("∞");

        /// <summary>
        /// -∞, the largest number smaller than every real number.
        /// </summary>
        public static KnownConstant NegativeInfinity = new("-∞");

        /// <summary>
        /// Can take on any value depending on the arrangement.
        /// </summary>
        public static KnownConstant Indeterminate = new("NaN");
    }
}
