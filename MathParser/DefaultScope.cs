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
    public static class DefaultScope
    {
        /// <summary>
        /// Gets the default <see cref="Scope"/> instance.
        /// </summary>
        public static Scope Instance { get; } = new Scope(StringComparer.InvariantCultureIgnoreCase)
            .AddWellKnownObjects()
            .AddBoolean()
            .AddSystemMath()
            .AddSystemDouble()
            .AddSystemNumericsComplex()
            .AddSystemSingle()
            .Freeze();

        public static Scope AddWellKnownObjects(this Scope scope)
        {
            scope
                .Add(WKC.I)
                .Add(WKC.GoldenRatio)
                .Add(WKC.EulersNumber)
                .Add(WKC.Pi)
                .Add(WKC.Tau)
                .Add(WKC.PositiveInfinity)
                .Add(WKF.Exponential.Pow)
                .Add(WKF.Exponential.Exp)
                .Add(WKF.Exponential.Sqrt)
                .Add(WKF.Exponential.Ln)
                .Add("log", WKF.Exponential.Ln)
                .Add(WKF.Trigonometric.Sine)
                .Add(WKF.Trigonometric.Cosine)
                .Add(WKF.Trigonometric.Tangent)
                .Add(WKF.Trigonometric.Arcsine)
                .Add(WKF.Trigonometric.Arcosine)
                .Add(WKF.Trigonometric.Arctangent)
                .Add(WKF.Hyperbolic.Sine)
                .Add(WKF.Hyperbolic.Cosine)
                .Add(WKF.Hyperbolic.Tangent)
                .Add(WKF.Hyperbolic.Arcsine)
                .Add(WKF.Hyperbolic.Arcosine)
                .Add(WKF.Hyperbolic.Arctangent)
                .Add(WKF.Piecewise.Abs)
                .Add(WKF.Piecewise.Ceiling)
                .Add(WKF.Piecewise.Floor)
                .Add(WKF.Piecewise.Sign)
                .Add(WKF.Complex.RealPart)
                .Add(WKF.Complex.ImaginaryPart)
                .Add(WKF.Complex.Argument)
                .Add(WKF.Complex.Conjugate);
            return scope;
        }

        public static Scope AddBoolean(this Scope scope)
        {
            scope
                .Add((bool x) => !x, WKF.Boolean.Not)
                .Add((bool l, bool r) => l & r, WKF.Boolean.And)
                .Add((bool l, bool r) => l && r, WKF.Boolean.And)
                .Add((bool l, bool r) => l | r, WKF.Boolean.Or)
                .Add((bool l, bool r) => l || r, WKF.Boolean.Or)
                .Add((bool l, bool r) => l ^ r, WKF.Boolean.ExclusiveOr);
            return scope;
        }

        public static Scope AddSystemDouble(this Scope scope)
        {
            scope
                .Add(typeof(double))
                .Add(Scope.MakeLambda([typeof(double), typeof(double)], p => Expression.Power(p[0], p[1])), WKF.Exponential.Pow);
            return scope;
        }

        public static Scope AddSystemSingle(this Scope scope)
        {
            scope
                .Add(typeof(float));
            return scope;
        }

        public static Scope AddSystemMath(this Scope scope)
        {
            scope
                .Add(Expression.MakeMemberAccess(null, typeof(Math).GetMember(nameof(Math.E)).Single()), WKC.EulersNumber)
                .Add(Expression.MakeMemberAccess(null, typeof(Math).GetMember(nameof(Math.PI)).Single()), WKC.Pi)
                .Add(Expression.MakeMemberAccess(null, typeof(Math).GetMember(nameof(Math.Tau)).Single()), WKC.Tau)
                .Add((double x) => Math.ReciprocalEstimate(x), WKF.Arithmetic.Reciprocal)
                .Add((double l, double r) => Math.Pow(l, r), WKF.Exponential.Pow)
                .Add((double x) => Math.Exp(x), WKF.Exponential.Exp)
                .Add((double x) => Math.Sqrt(x), WKF.Exponential.Sqrt)
                .Add((double x) => Math.Log(x), WKF.Exponential.Ln)
                .Add((double x) => Math.Sin(x), WKF.Trigonometric.Sine)
                .Add((double x) => Math.Cos(x), WKF.Trigonometric.Cosine)
                .Add((double x) => Math.Tan(x), WKF.Trigonometric.Tangent)
                .Add((double x) => Math.Sinh(x), WKF.Hyperbolic.Sine)
                .Add((double x) => Math.Cosh(x), WKF.Hyperbolic.Cosine)
                .Add((double x) => Math.Tanh(x), WKF.Hyperbolic.Tangent)
                .Add((double x) => Math.Asin(x), WKF.Trigonometric.Arcsine)
                .Add((double x) => Math.Acos(x), WKF.Trigonometric.Arcosine)
                .Add((double x) => Math.Atan(x), WKF.Trigonometric.Arctangent)
                .Add((double x) => Math.Asinh(x), WKF.Hyperbolic.Arcsine)
                .Add((double x) => Math.Acosh(x), WKF.Hyperbolic.Arcosine)
                .Add((double x) => Math.Atanh(x), WKF.Hyperbolic.Arctangent)
                .Add((double x) => Math.Abs(x), WKF.Piecewise.Abs)
                .Add((double x) => Math.Ceiling(x), WKF.Piecewise.Ceiling)
                .Add((double x) => Math.Floor(x), WKF.Piecewise.Floor)
                .Add((double x) => Math.Sign(x), WKF.Piecewise.Sign);
            return scope;
        }

        public static Scope AddSystemNumericsComplex(this Scope scope)
        {
            scope
                .Add(typeof(Complex))
                .Add(Expression.MakeMemberAccess(null, typeof(Complex).GetMember(nameof(Complex.One)).Single()), WKC.One)
                .Add(Expression.MakeMemberAccess(null, typeof(Complex).GetMember(nameof(Complex.ImaginaryOne)).Single()), WKC.I)
                .Add((Complex x) => Complex.Negate(x), WKF.Arithmetic.Negate)
                .Add((Complex l, double r) => Complex.Add(l, r), WKF.Arithmetic.Add)
                .Add((double l, Complex r) => Complex.Add(l, r), WKF.Arithmetic.Add)
                .Add((Complex l, Complex r) => Complex.Add(l, r), WKF.Arithmetic.Add)
                .Add((Complex l, double r) => Complex.Subtract(l, r), WKF.Arithmetic.Subtract)
                .Add((double l, Complex r) => Complex.Subtract(l, r), WKF.Arithmetic.Subtract)
                .Add((Complex l, Complex r) => Complex.Subtract(l, r), WKF.Arithmetic.Subtract)
                .Add((Complex x) => Complex.Reciprocal(x), WKF.Arithmetic.Reciprocal)
                .Add((Complex l, double r) => Complex.Multiply(l, r), WKF.Arithmetic.Multiply)
                .Add((double l, Complex r) => Complex.Multiply(l, r), WKF.Arithmetic.Multiply)
                .Add((Complex l, Complex r) => Complex.Multiply(l, r), WKF.Arithmetic.Multiply)
                .Add((Complex l, double r) => Complex.Divide(l, r), WKF.Arithmetic.Divide)
                .Add((double l, Complex r) => Complex.Divide(l, r), WKF.Arithmetic.Divide)
                .Add((Complex l, Complex r) => Complex.Divide(l, r), WKF.Arithmetic.Divide)
                .Add((Complex l, Complex r) => Complex.Pow(l, r), WKF.Exponential.Pow)
                .Add((Complex l, double r) => Complex.Pow(l, r), WKF.Exponential.Pow)
                .Add((Complex x) => Complex.Exp(x), WKF.Exponential.Exp)
                .Add((Complex x) => Complex.Sqrt(x), WKF.Exponential.Sqrt)
                .Add((Complex x) => Complex.Log(x), WKF.Exponential.Ln)
                .Add((Complex x) => Complex.Sin(x), WKF.Trigonometric.Sine)
                .Add((Complex x) => Complex.Cos(x), WKF.Trigonometric.Cosine)
                .Add((Complex x) => Complex.Tan(x), WKF.Trigonometric.Tangent)
                .Add((Complex x) => Complex.Sinh(x), WKF.Hyperbolic.Sine)
                .Add((Complex x) => Complex.Cosh(x), WKF.Hyperbolic.Cosine)
                .Add((Complex x) => Complex.Tanh(x), WKF.Hyperbolic.Tangent)
                .Add((Complex x) => Complex.Asin(x), WKF.Trigonometric.Arcsine)
                .Add((Complex x) => Complex.Acos(x), WKF.Trigonometric.Arcosine)
                .Add((Complex x) => Complex.Atan(x), WKF.Trigonometric.Arctangent)
                .Add((Complex x) => Complex.Abs(x), WKF.Piecewise.Abs)
                .Add((Complex x) => x.Real, WKF.Complex.RealPart)
                .Add((Complex x) => x.Imaginary, WKF.Complex.ImaginaryPart)
                .Add((Complex x) => x.Phase, WKF.Complex.Argument)
                .Add((Complex x) => Complex.Conjugate(x), WKF.Complex.Conjugate);
            return scope;
        }
    }
}
