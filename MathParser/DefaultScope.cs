// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Numerics;
    using System.Reflection;
    using WKF = WellKnownFunctions;

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

        public static readonly IDictionary<string, KnownFunction> NamedFunctions = new NamedFunctionMapping()
        {
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
        };

        public static readonly ExpressionPatternList<KnownFunction> KnownMethods = new WellKnownFunctionMapping()
        {
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
            { MakeLambda([typeof(double), typeof(double)], p => Expression.Power(p[0], p[1])), WKF.Exponential.Pow },
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
        };

        private class NamedFunctionMapping : Dictionary<string, KnownFunction>
        {
            public NamedFunctionMapping()
                : base(StringComparer.InvariantCultureIgnoreCase)
            {
            }

            public void Add(KnownFunction knownFunction) => this.Add(knownFunction.Name, knownFunction);
        }

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
                [ExpressionType.Negate] = WKF.Arithmetic.Negate,
                [ExpressionType.Add] = WKF.Arithmetic.Add,
                [ExpressionType.AddChecked] = WKF.Arithmetic.Add,
                [ExpressionType.Subtract] = WKF.Arithmetic.Subtract,
                [ExpressionType.SubtractChecked] = WKF.Arithmetic.Subtract,
                [ExpressionType.Multiply] = WKF.Arithmetic.Multiply,
                [ExpressionType.MultiplyChecked] = WKF.Arithmetic.Multiply,
                [ExpressionType.Divide] = WKF.Arithmetic.Divide, // DivideChecked not available.
                [ExpressionType.Power] = WKF.Exponential.Pow,
                [ExpressionType.Equal] = WKF.Comparison.Equal,
                [ExpressionType.NotEqual] = WKF.Comparison.NotEqual,
                [ExpressionType.GreaterThan] = WKF.Comparison.GreaterThan,
                [ExpressionType.GreaterThanOrEqual] = WKF.Comparison.GreaterThanOrEqual,
                [ExpressionType.LessThan] = WKF.Comparison.LessThan,
                [ExpressionType.LessThanOrEqual] = WKF.Comparison.LessThanOrEqual,
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

                    if (definition == typeof(IEqualityOperators<,,>))
                    {
                        var argTypes = typeArgs[..^1];
                        this.Add(MakeLambda(argTypes, p => Expression.Equal(p[0], p[1])), WKF.Comparison.Equal);
                        this.Add(MakeLambda(argTypes, p => Expression.NotEqual(p[0], p[1])), WKF.Comparison.NotEqual);
                    }
                    else if (definition == typeof(IComparisonOperators<,,>))
                    {
                        var argTypes = typeArgs[..^1];
                        this.Add(MakeLambda(argTypes, p => Expression.GreaterThan(p[0], p[1])), WKF.Comparison.GreaterThan);
                        this.Add(MakeLambda(argTypes, p => Expression.GreaterThanOrEqual(p[0], p[1])), WKF.Comparison.GreaterThanOrEqual);
                        this.Add(MakeLambda(argTypes, p => Expression.LessThan(p[0], p[1])), WKF.Comparison.LessThan);
                        this.Add(MakeLambda(argTypes, p => Expression.LessThanOrEqual(p[0], p[1])), WKF.Comparison.LessThanOrEqual);
                    }
                    ////else if (definition == typeof(IUnaryPlusOperators<,>))
                    ////{
                    ////    var argTypes = new[] { typeArgs[0] };
                    ////    this.Add(MakeLambda(argTypes, p => Expression.UnaryPlus(p[0])), WellKnownFunctions.Identity);
                    ////}
                    else if (definition == typeof(IUnaryNegationOperators<,>))
                    {
                        var argTypes = new[] { typeArgs[0] };
                        this.Add(MakeLambda(argTypes, p => Expression.Negate(p[0])), WKF.Arithmetic.Negate);
                        this.Add(MakeLambda(argTypes, p => Expression.NegateChecked(p[0])), WKF.Arithmetic.Negate);
                    }
                    else if (definition == typeof(IAdditionOperators<,,>))
                    {
                        var argTypes = typeArgs[..^1];
                        this.Add(MakeLambda(argTypes, p => Expression.Add(p[0], p[1])), WKF.Arithmetic.Add);
                        this.Add(MakeLambda(argTypes, p => Expression.AddChecked(p[0], p[1])), WKF.Arithmetic.Add);
                    }
                    else if (definition == typeof(ISubtractionOperators<,,>))
                    {
                        var argTypes = typeArgs[..^1];
                        this.Add(MakeLambda(argTypes, p => Expression.Subtract(p[0], p[1])), WKF.Arithmetic.Subtract);
                        this.Add(MakeLambda(argTypes, p => Expression.SubtractChecked(p[0], p[1])), WKF.Arithmetic.Subtract);
                    }
                    else if (definition == typeof(IMultiplyOperators<,,>))
                    {
                        var argTypes = typeArgs[..^1];
                        this.Add(MakeLambda(argTypes, p => Expression.Multiply(p[0], p[1])), WKF.Arithmetic.Multiply);
                        this.Add(MakeLambda(argTypes, p => Expression.MultiplyChecked(p[0], p[1])), WKF.Arithmetic.Multiply);
                    }
                    else if (definition == typeof(IDivisionOperators<,,>))
                    {
                        var argTypes = typeArgs[..^1];
                        this.Add(MakeLambda(argTypes, p => Expression.Divide(p[0], p[1])), WKF.Arithmetic.Divide);
                        var opDivideChecked = type.GetMethod("op_CheckedDivision", argTypes);
                        this.Add(MakeLambda(argTypes, p => Expression.Divide(p[0], p[1], opDivideChecked)), WKF.Arithmetic.Divide);
                    }
                    else if (definition == typeof(IPowerFunctions<>))
                    {
                        var argTypes = new[] { typeArgs[0], typeArgs[0] };
                        var pow = type.GetMethod(nameof(IPowerFunctions<>.Pow), argTypes);
                        this.Add(MakeLambda(argTypes, p => Expression.Call(pow, p[0], p[1])), WKF.Exponential.Pow);
                    }
                    else if (definition == typeof(IRootFunctions<>))
                    {
                        var argTypes = new[] { typeArgs[0] };
                        var sqrt = type.GetMethod(nameof(IRootFunctions<>.Sqrt), argTypes);
                        this.Add(MakeLambda(argTypes, p => Expression.Call(sqrt, p[0])), WKF.Exponential.Sqrt);
                    }
                    else if (definition == typeof(ILogarithmicFunctions<>))
                    {
                        var argTypes = new[] { typeArgs[0] };
                        var log = type.GetMethod(nameof(ILogarithmicFunctions<>.Log), argTypes);
                        this.Add(MakeLambda(argTypes, p => Expression.Call(log, p[0])), WKF.Exponential.Ln);
                    }
                    else if (definition == typeof(IExponentialFunctions<>))
                    {
                        var argTypes = new[] { typeArgs[0] };
                        var exp = type.GetMethod(nameof(IExponentialFunctions<>.Exp), argTypes);
                        this.Add(MakeLambda(argTypes, p => Expression.Call(exp, p[0])), WKF.Exponential.Exp);
                    }
                }
            }
        }
    }
}
