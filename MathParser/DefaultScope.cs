// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser
{
    using System;
    using System.Linq.Expressions;
    using System.Numerics;

    /// <summary>
    /// A temporary class for refactoring.
    /// </summary>
    internal class DefaultScope
    {
        private static LambdaExpression MakeLambda(Type[] parameters, Func<ParameterExpression[], Expression> builder)
        {
            var parameterExpressions = Array.ConvertAll(parameters, Expression.Parameter);
            return Expression.Lambda(builder(parameterExpressions), parameterExpressions);
        }

        public static readonly ExpressionPatternList<KnownFunction> KnownMethods = new()
        {
            { (double x) => -x, WellKnownFunctions.Negate },
            { (Complex x) => -x, WellKnownFunctions.Negate },
            { (Complex x) => Complex.Negate(x), WellKnownFunctions.Negate },
            { (double l, double r) => l + r, WellKnownFunctions.Add },
            { (Complex l, double r) => l + r, WellKnownFunctions.Add },
            { (double l, Complex r) => l + r, WellKnownFunctions.Add },
            { (Complex l, Complex r) => l + r, WellKnownFunctions.Add },
            { (Complex l, double r) => Complex.Add(l, r), WellKnownFunctions.Add },
            { (double l, Complex r) => Complex.Add(l, r), WellKnownFunctions.Add },
            { (Complex l, Complex r) => Complex.Add(l, r), WellKnownFunctions.Add },
            { (double l, double r) => l - r, WellKnownFunctions.Subtract },
            { (Complex l, double r) => l - r, WellKnownFunctions.Subtract },
            { (double l, Complex r) => l - r, WellKnownFunctions.Subtract },
            { (Complex l, Complex r) => l - r, WellKnownFunctions.Subtract },
            { (Complex l, double r) => Complex.Subtract(l, r), WellKnownFunctions.Subtract },
            { (double l, Complex r) => Complex.Subtract(l, r), WellKnownFunctions.Subtract },
            { (Complex l, Complex r) => Complex.Subtract(l, r), WellKnownFunctions.Subtract },
            { (double l, double r) => l * r, WellKnownFunctions.Multiply },
            { (Complex l, double r) => l * r, WellKnownFunctions.Multiply },
            { (double l, Complex r) => l * r, WellKnownFunctions.Multiply },
            { (Complex l, Complex r) => l * r, WellKnownFunctions.Multiply },
            { (Complex l, double r) => Complex.Multiply(l, r), WellKnownFunctions.Multiply },
            { (double l, Complex r) => Complex.Multiply(l, r), WellKnownFunctions.Multiply },
            { (Complex l, Complex r) => Complex.Multiply(l, r), WellKnownFunctions.Multiply },
            { (double l, double r) => l / r, WellKnownFunctions.Divide },
            { (Complex l, double r) => l / r, WellKnownFunctions.Divide },
            { (double l, Complex r) => l / r, WellKnownFunctions.Divide },
            { (Complex l, Complex r) => l / r, WellKnownFunctions.Divide },
            { (Complex l, double r) => Complex.Divide(l, r), WellKnownFunctions.Divide },
            { (double l, Complex r) => Complex.Divide(l, r), WellKnownFunctions.Divide },
            { (Complex l, Complex r) => Complex.Divide(l, r), WellKnownFunctions.Divide },
            { MakeLambda([typeof(double), typeof(double)], p => Expression.Power(p[0], p[1])), WellKnownFunctions.Pow },
            { (Complex l, Complex r) => Complex.Pow(l, r), WellKnownFunctions.Pow },
            { (Complex l, double r) => Complex.Pow(l, r), WellKnownFunctions.Pow },
            { (double l, double r) => Math.Pow(l, r), WellKnownFunctions.Pow },
            { (Complex x) => Complex.Exp(x), WellKnownFunctions.Exp },
            { (double x) => Math.Exp(x), WellKnownFunctions.Exp },
            { (Complex x) => Complex.Sqrt(x), WellKnownFunctions.Sqrt },
            { (double x) => Math.Sqrt(x), WellKnownFunctions.Sqrt },
            { (Complex x) => Complex.Log(x), WellKnownFunctions.Ln },
            { (double x) => Math.Log(x), WellKnownFunctions.Ln },
        };
    }
}
