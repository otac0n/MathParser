// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser
{
    using System;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Numerics;
    using WKC = WellKnownConstants;
    using WKF = WellKnownFunctions;

    /// <summary>
    /// A container class for the default <see cref="Scope"/> instance.
    /// </summary>
    public class DefaultScope
    {
        /// <summary>
        /// Gets the default <see cref="Scope"/> instance.
        /// </summary>
        public static Scope Instance { get; } = new Scope(StringComparer.InvariantCultureIgnoreCase)
        {
            { WKC.I },
            { WKC.GoldenRatio },
            { WKC.EulersNumber },
            { WKC.Pi },
            { WKC.Tau },
            { WKC.PositiveInfinity },
            { WKF.Exponential.Pow },
            { WKF.Exponential.Exp },
            { WKF.Exponential.Sqrt },
            { "log", WKF.Exponential.Ln },
            { WKF.Trigonometric.Sine },
            { WKF.Trigonometric.Cosine },
            { WKF.Trigonometric.Tangent },
            { WKF.Trigonometric.Arcsine },
            { WKF.Trigonometric.Arcosine },
            { WKF.Trigonometric.Arctangent },
            { WKF.Hyperbolic.Sine },
            { WKF.Hyperbolic.Cosine },
            { WKF.Hyperbolic.Tangent },
            { WKF.Hyperbolic.Arcsine },
            { WKF.Hyperbolic.Arcosine },
            { WKF.Hyperbolic.Arctangent },
            { WKF.Piecewise.Abs },
            { WKF.Piecewise.Ceiling },
            { WKF.Piecewise.Floor },
            { WKF.Piecewise.Sign },
            { WKF.Complex.RealPart },
            { WKF.Complex.ImaginaryPart },
            { WKF.Complex.Argument },
            { WKF.Complex.Conjugate },
            { Expression.MakeMemberAccess(null, typeof(Math).GetMember(nameof(Math.E)).Single()), WKC.EulersNumber },
            { Expression.MakeMemberAccess(null, typeof(Math).GetMember(nameof(Math.PI)).Single()), WKC.Pi },
            { Expression.MakeMemberAccess(null, typeof(Math).GetMember(nameof(Math.Tau)).Single()), WKC.Tau },
            { Expression.MakeMemberAccess(null, typeof(Complex).GetMember(nameof(Complex.ImaginaryOne)).Single()), WKC.I },
            { (bool x) => !x, WKF.Boolean.Not },
            { (bool l, bool r) => l & r, WKF.Boolean.And },
            { (bool l, bool r) => l && r, WKF.Boolean.And },
            { (bool l, bool r) => l | r, WKF.Boolean.Or },
            { (bool l, bool r) => l || r, WKF.Boolean.Or },
            { (bool l, bool r) => l ^ r, WKF.Boolean.ExclusiveOr },
            { typeof(float) },
            { typeof(double) },
            { typeof(Complex) },
            { (Complex x) => Complex.Negate(x), WKF.Arithmetic.Negate },
            { (Complex l, double r) => Complex.Add(l, r), WKF.Arithmetic.Add },
            { (double l, Complex r) => Complex.Add(l, r), WKF.Arithmetic.Add },
            { (Complex l, Complex r) => Complex.Add(l, r), WKF.Arithmetic.Add },
            { (Complex l, double r) => Complex.Subtract(l, r), WKF.Arithmetic.Subtract },
            { (double l, Complex r) => Complex.Subtract(l, r), WKF.Arithmetic.Subtract },
            { (Complex l, Complex r) => Complex.Subtract(l, r), WKF.Arithmetic.Subtract },
            { (Complex l, double r) => Complex.Multiply(l, r), WKF.Arithmetic.Multiply },
            { (double l, Complex r) => Complex.Multiply(l, r), WKF.Arithmetic.Multiply },
            { (Complex l, Complex r) => Complex.Multiply(l, r), WKF.Arithmetic.Multiply },
            { (Complex l, double r) => Complex.Divide(l, r), WKF.Arithmetic.Divide },
            { (double l, Complex r) => Complex.Divide(l, r), WKF.Arithmetic.Divide },
            { (Complex l, Complex r) => Complex.Divide(l, r), WKF.Arithmetic.Divide },
            { Scope.MakeLambda([typeof(double), typeof(double)], p => Expression.Power(p[0], p[1])), WKF.Exponential.Pow },
            { (Complex l, Complex r) => Complex.Pow(l, r), WKF.Exponential.Pow },
            { (Complex l, double r) => Complex.Pow(l, r), WKF.Exponential.Pow },
            { (double l, double r) => Math.Pow(l, r), WKF.Exponential.Pow },
            { (Complex x) => Complex.Exp(x), WKF.Exponential.Exp },
            { (double x) => Math.Exp(x), WKF.Exponential.Exp },
            { (Complex x) => Complex.Sqrt(x), WKF.Exponential.Sqrt },
            { (double x) => Math.Sqrt(x), WKF.Exponential.Sqrt },
            { (Complex x) => Complex.Log(x), WKF.Exponential.Ln },
            { (double x) => Math.Log(x), WKF.Exponential.Ln },
            { (double x) => Math.Sin(x), WKF.Trigonometric.Sine },
            { (Complex x) => Complex.Sin(x), WKF.Trigonometric.Sine },
            { (double x) => Math.Cos(x), WKF.Trigonometric.Cosine },
            { (Complex x) => Complex.Cos(x), WKF.Trigonometric.Cosine },
            { (double x) => Math.Tan(x), WKF.Trigonometric.Tangent },
            { (Complex x) => Complex.Tan(x), WKF.Trigonometric.Tangent },
            { (double x) => Math.Sinh(x), WKF.Hyperbolic.Sine },
            { (Complex x) => Complex.Sinh(x), WKF.Hyperbolic.Sine },
            { (double x) => Math.Cosh(x), WKF.Hyperbolic.Cosine },
            { (Complex x) => Complex.Cosh(x), WKF.Hyperbolic.Cosine },
            { (double x) => Math.Tanh(x), WKF.Hyperbolic.Tangent },
            { (Complex x) => Complex.Tanh(x), WKF.Hyperbolic.Tangent },
            { (double x) => Math.Asin(x), WKF.Trigonometric.Arcsine },
            { (Complex x) => Complex.Asin(x), WKF.Trigonometric.Arcsine },
            { (double x) => Math.Acos(x), WKF.Trigonometric.Arcosine },
            { (Complex x) => Complex.Acos(x), WKF.Trigonometric.Arcosine },
            { (double x) => Math.Atan(x), WKF.Trigonometric.Arctangent },
            { (Complex x) => Complex.Atan(x), WKF.Trigonometric.Arctangent },
            { (double x) => Math.Asinh(x), WKF.Hyperbolic.Arcsine },
            { (double x) => Math.Acosh(x), WKF.Hyperbolic.Arcosine },
            { (double x) => Math.Atanh(x), WKF.Hyperbolic.Arctangent },
            { (double x) => Math.Abs(x), WKF.Piecewise.Abs },
            { (Complex x) => Complex.Abs(x), WKF.Piecewise.Abs },
            { (double x) => Math.Ceiling(x), WKF.Piecewise.Ceiling },
            { (double x) => Math.Floor(x), WKF.Piecewise.Floor },
            { (double x) => Math.Sign(x), WKF.Piecewise.Sign },
            { (Complex x) => x.Real, WKF.Complex.RealPart },
            { (Complex x) => x.Imaginary, WKF.Complex.ImaginaryPart },
            { (Complex x) => x.Phase, WKF.Complex.Argument },
            { (Complex x) => Complex.Conjugate(x), WKF.Complex.Conjugate },
        }.Freeze();
    }
}
