// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Numerics;
    using System.Reflection;
    using System.Text.RegularExpressions;

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

        public static readonly ExpressionPatternList<KnownFunction> KnownMethods = new WellKnownFunctionMapping()
        {
            { typeof(float) },
            { typeof(double) },
            { typeof(Complex) },
            { (Complex x) => Complex.Negate(x), WellKnownFunctions.Negate },
            { (Complex l, double r) => Complex.Add(l, r), WellKnownFunctions.Add },
            { (double l, Complex r) => Complex.Add(l, r), WellKnownFunctions.Add },
            { (Complex l, Complex r) => Complex.Add(l, r), WellKnownFunctions.Add },
            { (Complex l, double r) => Complex.Subtract(l, r), WellKnownFunctions.Subtract },
            { (double l, Complex r) => Complex.Subtract(l, r), WellKnownFunctions.Subtract },
            { (Complex l, Complex r) => Complex.Subtract(l, r), WellKnownFunctions.Subtract },
            { (Complex l, double r) => Complex.Multiply(l, r), WellKnownFunctions.Multiply },
            { (double l, Complex r) => Complex.Multiply(l, r), WellKnownFunctions.Multiply },
            { (Complex l, Complex r) => Complex.Multiply(l, r), WellKnownFunctions.Multiply },
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

        private class WellKnownFunctionMapping : ExpressionPatternList<KnownFunction>
        {
            public static Dictionary<string, (KnownFunction function, ExpressionType type)> Operators = new()
            {
                ["op_UnaryNegation"] = (WellKnownFunctions.Negate, ExpressionType.Negate),
                ["op_Addition"] = (WellKnownFunctions.Add, ExpressionType.Add),
                ["op_Subtraction"] = (WellKnownFunctions.Subtract, ExpressionType.Subtract),
                ["op_Multiply"] = (WellKnownFunctions.Multiply, ExpressionType.Multiply),
                ["op_Division"] = (WellKnownFunctions.Divide, ExpressionType.Divide),
            };

            public void Add(Type numberType)
            {
                var methods = numberType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).ToLookup(m => Regex.Match(m.Name, "[^.]+$").Value);
                var matches = from methodGroup in methods
                              where Operators.ContainsKey(methodGroup.Key)
                              let known = Operators[methodGroup.Key]
                              from method in methodGroup
                              let parameters = method.GetParameters()
                              let parameterTypes = Array.ConvertAll(parameters, p => p.ParameterType)
                              select (expression: MakeLambda(parameterTypes, p => p.Length == 1 ? Expression.MakeUnary(known.type, p[0], null!) : Expression.MakeBinary(known.type, p[0], p[1])), known.function);

                foreach (var (expression, function) in matches)
                {
                    this.Add(expression, function);
                }

                var interfaces = numberType.GetInterfaces();
                foreach (var type in interfaces)
                {
                    var definition = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
                    var typeArgs = type.IsGenericType ? type.GetGenericArguments() : [];

                    if (definition == typeof(IPowerFunctions<>))
                    {
                        var argTypes = new[] { typeArgs[0], typeArgs[0] };
                        var pow = type.GetMethod(nameof(WellKnownFunctions.Pow), argTypes);
                        this.Add(MakeLambda(argTypes, p => Expression.Call(pow, p[0], p[1])), WellKnownFunctions.Pow);
                    }
                    else if (definition == typeof(IRootFunctions<>))
                    {
                        var argTypes = new[] { typeArgs[0] };
                        var sqrt = type.GetMethod(nameof(WellKnownFunctions.Sqrt), argTypes);
                        this.Add(MakeLambda(argTypes, p => Expression.Call(sqrt, p[0])), WellKnownFunctions.Sqrt);
                    }
                    else if (definition == typeof(ILogarithmicFunctions<>))
                    {
                        var argTypes = new[] { typeArgs[0] };
                        var log = type.GetMethod(nameof(Math.Log), argTypes);
                        this.Add(MakeLambda(argTypes, p => Expression.Call(log, p[0])), WellKnownFunctions.Ln);
                    }
                    else if (definition == typeof(IExponentialFunctions<>))
                    {
                        var argTypes = new[] { typeArgs[0] };
                        var exp = type.GetMethod(nameof(WellKnownFunctions.Exp), argTypes);
                        this.Add(MakeLambda(argTypes, p => Expression.Call(exp, p[0])), WellKnownFunctions.Exp);
                    }
                }
            }
        }
    }
}
