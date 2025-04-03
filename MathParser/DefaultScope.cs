// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Numerics;
    using System.Reflection;

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
            /// <summary>
            /// <see href="https://github.com/dotnet/csharpstandard/blob/draft-v8/standard/classes.md#153106-method-names-reserved-for-operators"/> §15.3.10.6 Method names reserved for operators.
            /// <seealso href="https://github.com/dotnet/roslyn/blob/main/src/Compilers/Core/Portable/Symbols/WellKnownMemberNames.cs">
            /// Roslyn definition of reserved operator method names, including op_Exponent.
            /// </seealso>
            /// </summary>
            public static readonly Dictionary<string, ExpressionType> OperatorMethods = new()
            {
                ["op_UnaryNegation"] = ExpressionType.Negate,
                ["op_UnaryPlus"] = ExpressionType.UnaryPlus,
                ["op_Addition"] = ExpressionType.Add,
                ["op_Subtraction"] = ExpressionType.Subtract,
                ["op_Multiply"] = ExpressionType.Multiply,
                ["op_Division"] = ExpressionType.Divide,
                ["op_Modulus"] = ExpressionType.Modulo,
                ["op_Exponent"] = ExpressionType.Power, // VB
                ["op_Exponentiation"] = ExpressionType.Power, // F#
                ["op_CheckedAddition"] = ExpressionType.AddChecked,
                ["op_CheckedSubtraction"] = ExpressionType.SubtractChecked,
                ["op_CheckedMultiply"] = ExpressionType.MultiplyChecked,
                ["op_CheckedDivision"] = ExpressionType.Divide, // DivideChecked not available.
                ["op_OnesComplement"] = ExpressionType.OnesComplement,
                ["op_LogicalNot"] = ExpressionType.Not,
                ["op_BitwiseAnd"] = ExpressionType.And,
                ["op_BitwiseOr"] = ExpressionType.Or,
                ["op_ExclusiveOr"] = ExpressionType.ExclusiveOr,
                ["op_Equality"] = ExpressionType.Equal,
                ["op_Inequality"] = ExpressionType.NotEqual,
                ["op_GreaterThan"] = ExpressionType.GreaterThan,
                ["op_GreaterThanOrEqual"] = ExpressionType.GreaterThanOrEqual,
                ["op_LessThan"] = ExpressionType.LessThan,
                ["op_LessThanOrEqual"] = ExpressionType.LessThanOrEqual,
                ["op_LeftShift"] = ExpressionType.LeftShift,
                ["op_RightShift"] = ExpressionType.RightShift,
            };

            private static readonly Dictionary<ExpressionType, KnownFunction> Operators = new()
            {
                [ExpressionType.Negate] = WellKnownFunctions.Negate,
                [ExpressionType.Add] = WellKnownFunctions.Add,
                [ExpressionType.AddChecked] = WellKnownFunctions.Add,
                [ExpressionType.Subtract] = WellKnownFunctions.Subtract,
                [ExpressionType.SubtractChecked] = WellKnownFunctions.Subtract,
                [ExpressionType.Multiply] = WellKnownFunctions.Multiply,
                [ExpressionType.MultiplyChecked] = WellKnownFunctions.Multiply,
                [ExpressionType.Divide] = WellKnownFunctions.Divide, // DivideChecked not available.
                [ExpressionType.Power] = WellKnownFunctions.Pow,
            };

            public void Add(Type numberType)
            {
                var methods = numberType.GetMethods(BindingFlags.Public | BindingFlags.Static).ToLookup(m => m.Name);
                var matches = from methodGroup in methods
                              where OperatorMethods.ContainsKey(methodGroup.Key)
                              let expressionType = OperatorMethods[methodGroup.Key]
                              where Operators.ContainsKey(expressionType)
                              let function = Operators[expressionType]
                              from method in methodGroup
                              let parameters = method.GetParameters()
                              let parameterTypes = Array.ConvertAll(parameters, p => p.ParameterType)
                              select (expression: MakeLambda(parameterTypes, p => p.Length == 1 ? Expression.MakeUnary(expressionType, p[0], null!) : Expression.MakeBinary(expressionType, p[0], p[1])), function);

                foreach (var (expression, function) in matches)
                {
                    this.Add(expression, function);
                }

                var interfaces = numberType.GetInterfaces();
                foreach (var type in interfaces)
                {
                    var definition = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
                    var typeArgs = type.IsGenericType ? type.GetGenericArguments() : [];

                    ////if (definition == typeof(IUnaryPlusOperators<,>))
                    ////{
                    ////    var argTypes = new[] { typeArgs[0] };
                    ////    this.Add(MakeLambda(argTypes, p => Expression.UnaryPlus(p[0])), WellKnownFunctions.Identity);
                    ////}
                    ////else
                    if (definition == typeof(IUnaryNegationOperators<,>))
                    {
                        var argTypes = new[] { typeArgs[0] };
                        this.Add(MakeLambda(argTypes, p => Expression.Negate(p[0])), WellKnownFunctions.Negate);
                        this.Add(MakeLambda(argTypes, p => Expression.NegateChecked(p[0])), WellKnownFunctions.Negate);
                    }
                    else if (definition == typeof(IAdditionOperators<,,>))
                    {
                        var argTypes = typeArgs[..^1];
                        this.Add(MakeLambda(argTypes, p => Expression.Add(p[0], p[1])), WellKnownFunctions.Add);
                        this.Add(MakeLambda(argTypes, p => Expression.AddChecked(p[0], p[1])), WellKnownFunctions.Add);
                    }
                    else if (definition == typeof(ISubtractionOperators<,,>))
                    {
                        var argTypes = typeArgs[..^1];
                        this.Add(MakeLambda(argTypes, p => Expression.Subtract(p[0], p[1])), WellKnownFunctions.Subtract);
                        this.Add(MakeLambda(argTypes, p => Expression.SubtractChecked(p[0], p[1])), WellKnownFunctions.Subtract);
                    }
                    else if (definition == typeof(IMultiplyOperators<,,>))
                    {
                        var argTypes = typeArgs[..^1];
                        this.Add(MakeLambda(argTypes, p => Expression.Multiply(p[0], p[1])), WellKnownFunctions.Multiply);
                        this.Add(MakeLambda(argTypes, p => Expression.MultiplyChecked(p[0], p[1])), WellKnownFunctions.Multiply);
                    }
                    else if (definition == typeof(IDivisionOperators<,,>))
                    {
                        var argTypes = typeArgs[..^1];
                        this.Add(MakeLambda(argTypes, p => Expression.Divide(p[0], p[1])), WellKnownFunctions.Divide);
                        var opDivideChecked = type.GetMethod("op_CheckedDivision", argTypes);
                        this.Add(MakeLambda(argTypes, p => Expression.Divide(p[0], p[1], opDivideChecked)), WellKnownFunctions.Divide);
                    }
                    else if (definition == typeof(IPowerFunctions<>))
                    {
                        var argTypes = new[] { typeArgs[0], typeArgs[0] };
                        var pow = type.GetMethod(nameof(IPowerFunctions<>.Pow), argTypes);
                        this.Add(MakeLambda(argTypes, p => Expression.Call(pow, p[0], p[1])), WellKnownFunctions.Pow);
                    }
                    else if (definition == typeof(IRootFunctions<>))
                    {
                        var argTypes = new[] { typeArgs[0] };
                        var sqrt = type.GetMethod(nameof(IRootFunctions<>.Sqrt), argTypes);
                        this.Add(MakeLambda(argTypes, p => Expression.Call(sqrt, p[0])), WellKnownFunctions.Sqrt);
                    }
                    else if (definition == typeof(ILogarithmicFunctions<>))
                    {
                        var argTypes = new[] { typeArgs[0] };
                        var log = type.GetMethod(nameof(ILogarithmicFunctions<>.Log), argTypes);
                        this.Add(MakeLambda(argTypes, p => Expression.Call(log, p[0])), WellKnownFunctions.Ln);
                    }
                    else if (definition == typeof(IExponentialFunctions<>))
                    {
                        var argTypes = new[] { typeArgs[0] };
                        var exp = type.GetMethod(nameof(IExponentialFunctions<>.Exp), argTypes);
                        this.Add(MakeLambda(argTypes, p => Expression.Call(exp, p[0])), WellKnownFunctions.Exp);
                    }
                }
            }
        }
    }
}
