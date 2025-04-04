// Copyright © John & Layla Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Numerics;
    using System.Reflection;
    using WKF = WellKnownFunctions;

    public static class Operations
    {
        public static Expression Simplify(Expression expression)
        {
            return new SimplifyVisitor().Visit(expression);
        }

        public static LambdaExpression Derivative(LambdaExpression expression)
        {
            var variable = expression.Parameters.Single();

            // TODO: Should the name be altered, e.g. `f'`?
            return Expression.Lambda(Simplify(Derivative(expression.Body, variable)), expression.Name, expression.TailCall, [variable]);
        }

        public static Expression Derivative(Expression expression, ParameterExpression variable) =>
            expression.NodeType switch
            {
                ExpressionType.Parameter when expression is ParameterExpression parameter => parameter == variable ? One() : Zero(),
                ExpressionType.MemberAccess when expression is MemberExpression member => Zero(),
                ExpressionType.Constant when expression is ConstantExpression constant => IsNaN(constant) ? constant : Zero(), // TODO: Does constant infinity have a derivative?
                ExpressionType.Convert when expression is UnaryExpression unary => Expression.Convert(Derivative(unary.Operand, variable), unary.Type),
                ExpressionType.Conditional when expression is ConditionalExpression conditional => Conditional(conditional.Test, Derivative(conditional.IfTrue, variable), Derivative(conditional.IfFalse, variable)),
                ExpressionType.Negate when expression is UnaryExpression unary => Negate(Derivative(unary.Operand, variable)),
                ExpressionType.Add when expression is BinaryExpression binary => Add(Derivative(binary.Left, variable), Derivative(binary.Right, variable)),
                ExpressionType.Subtract when expression is BinaryExpression binary => Subtract(Derivative(binary.Left, variable), Derivative(binary.Right, variable)),
                ExpressionType.Multiply when expression is BinaryExpression binary => Add(Multiply(Derivative(binary.Left, variable), binary.Right), Multiply(binary.Left, Derivative(binary.Right, variable))),
                ExpressionType.Divide when expression is BinaryExpression binary => Divide(Subtract(Multiply(Derivative(binary.Left, variable), binary.Right), Multiply(binary.Left, Derivative(binary.Right, variable))), Pow(binary.Right, Expression.Constant(2.0))),
                ExpressionType.Call when expression is MethodCallExpression methodCall && methodCall.Object is null && (methodCall.Method.DeclaringType == typeof(Math) || methodCall.Method.DeclaringType == typeof(Complex)) => methodCall.Method.Name switch
                {
                    nameof(Math.Abs) when methodCall.Arguments.Count == 1 => Multiply(Derivative(methodCall.Arguments[0], variable), Divide(expression, methodCall.Arguments[0])),
                    nameof(Math.Sin) when methodCall.Arguments.Count == 1 => Multiply(Derivative(methodCall.Arguments[0], variable), Cos(methodCall.Arguments[0])),
                    nameof(Math.Cos) when methodCall.Arguments.Count == 1 => Multiply(Derivative(methodCall.Arguments[0], variable), Negate(Sin(methodCall.Arguments[0]))),
                    nameof(Math.Tan) when methodCall.Arguments.Count == 1 => Divide(Derivative(methodCall.Arguments[0], variable), Pow(Cos(methodCall.Arguments[0]), Expression.Constant(2.0))),
                    nameof(Math.Asin) when methodCall.Arguments.Count == 1 => Divide(Derivative(methodCall.Arguments[0], variable), Sqrt(Subtract(Expression.Constant(1.0), Pow(methodCall.Arguments[0], Expression.Constant(2.0))))),
                    nameof(Math.Acos) when methodCall.Arguments.Count == 1 => Divide(Derivative(methodCall.Arguments[0], variable), Negate(Sqrt(Subtract(Expression.Constant(1.0), Pow(methodCall.Arguments[0], Expression.Constant(2.0)))))),
                    nameof(Math.Atan) when methodCall.Arguments.Count == 1 => Divide(Derivative(methodCall.Arguments[0], variable), Add(Pow(methodCall.Arguments[0], Expression.Constant(2.0)), Expression.Constant(1.0))),
                    nameof(Math.Sinh) when methodCall.Arguments.Count == 1 => Multiply(Derivative(methodCall.Arguments[0], variable), Cosh(methodCall.Arguments[0])),
                    nameof(Math.Cosh) when methodCall.Arguments.Count == 1 => Multiply(Derivative(methodCall.Arguments[0], variable), Sinh(methodCall.Arguments[0])),
                    nameof(Math.Tanh) when methodCall.Arguments.Count == 1 => Divide(Derivative(methodCall.Arguments[0], variable), Pow(Cosh(methodCall.Arguments[0]), Expression.Constant(2.0))),
                    nameof(Math.Asinh) when methodCall.Arguments.Count == 1 => Divide(Derivative(methodCall.Arguments[0], variable), Sqrt(Add(Pow(methodCall.Arguments[0], Expression.Constant(2.0)), Expression.Constant(1.0)))),
                    nameof(Math.Acosh) when methodCall.Arguments.Count == 1 => Divide(Derivative(methodCall.Arguments[0], variable), Sqrt(Subtract(Pow(methodCall.Arguments[0], Expression.Constant(2.0)), Expression.Constant(1.0)))), // TODO: Domain of the function is Reals > 1
                    nameof(Math.Atanh) when methodCall.Arguments.Count == 1 => Divide(Derivative(methodCall.Arguments[0], variable), Subtract(Expression.Constant(1.0), Pow(methodCall.Arguments[0], Expression.Constant(2.0)))), // TODO: Domain of the function is |Reals| < 1
                    nameof(Math.Sqrt) when methodCall.Arguments.Count == 1 => Multiply(Derivative(methodCall.Arguments[0], variable), Multiply(Expression.Constant(0.5), Divide(expression, methodCall.Arguments[0]))),
                    nameof(Math.Exp) when methodCall.Arguments.Count == 1 => Multiply(Derivative(methodCall.Arguments[0], variable), expression),
                    nameof(Math.Log) when methodCall.Arguments.Count == 1 => Divide(Derivative(methodCall.Arguments[0], variable), methodCall.Arguments[0]), // TODO: Domain of the function is Reals > 0.
                    nameof(Math.Pow) when methodCall.Arguments.Count == 2 =>
                        IsConstantValue(methodCall.Arguments[1], out var constant)
                            ? Multiply(Derivative(methodCall.Arguments[0], variable), Multiply(constant, Pow(methodCall.Arguments[0], Subtract(constant, One()))))
                            : Multiply(expression, Derivative(Multiply(Log(methodCall.Arguments[0]), methodCall.Arguments[1]), variable)),
                    _ => throw new NotImplementedException($"The method, {methodCall.Method}, is not implemented."),
                },
                ExpressionType.Power when expression is BinaryExpression binary =>
                        IsConstantValue(binary.Right, out var constant)
                            ? Multiply(Derivative(binary.Left, variable), Multiply(constant, Pow(binary.Left, Subtract(constant, One()))))
                            : Multiply(expression, Derivative(Multiply(Log(binary.Left), binary.Right), variable)),
                _ => throw new NotImplementedException($"The {expression.NodeType}, {expression}, is not implemented."),
            };

        public static ConstantExpression Zero() => Expression.Constant(0.0);

        public static ConstantExpression One() => Expression.Constant(1.0);

        public static ConstantExpression NaN() => Expression.Constant(double.NaN);

        public static Expression ConvertIfLower(Expression expression, Expression to)
        {
            return ConvertIfLower(expression, to: to.Type);
        }

        public static Expression ConvertIfLower(Expression expression, Type to)
        {
            var from = expression.Type;
            if (to == typeof(Complex) && from != typeof(Complex))
            {
                return Expression.Convert(expression, to);
            }

            return expression;
        }

        public static Expression And(Expression left, Expression right) => Expression.AndAlso(left, right);

        public static Expression Or(Expression left, Expression right) => Expression.OrElse(left, right);

        public static Expression Not(Expression expression) => Expression.Not(expression);

        public static Expression Equal(Expression left, Expression right) => Expression.Equal(ConvertIfLower(left, to: right), ConvertIfLower(right, to: left));

        public static Expression NotEqual(Expression left, Expression right) => Expression.NotEqual(ConvertIfLower(left, to: right), ConvertIfLower(right, to: left));

        public static Expression LowerToReal(Expression expression)
        {
            var from = expression.Type;
            if (from == typeof(Complex))
            {
                return Expression.MakeMemberAccess(expression, typeof(Complex).GetProperty(nameof(Complex.Real), BindingFlags.Public | BindingFlags.Instance));
            }

            return expression;
        }

        public static Expression Abs(Expression expression) => Bind(WKF.Piecewise.Abs, expression);

        public static Expression Conditional(Expression condition, Expression consequent, Expression alternative)
        {
            return Expression.Condition(condition, ConvertIfLower(consequent, to: alternative), ConvertIfLower(alternative, to: consequent));
        }

        public static Expression Ceiling(Expression expression) => Bind(WKF.Piecewise.Ceiling, expression);

        public static Expression Floor(Expression expression) => Bind(WKF.Piecewise.Floor, expression);

        public static Expression Negate(Expression operand) => Bind(WKF.Arithmetic.Negate, operand);

        public static Expression Add(Expression augend, Expression addend) => Bind(WKF.Arithmetic.Add, augend, addend);

        public static Expression Subtract(Expression minuend, Expression subtrahend) => Bind(WKF.Arithmetic.Subtract, minuend, subtrahend);

        public static Expression Multiply(Expression multiplicand, Expression multiplier) => Bind(WKF.Arithmetic.Multiply, multiplicand, multiplier);

        public static Expression Divide(Expression dividend, Expression divisor) => Bind(WKF.Arithmetic.Divide, dividend, divisor);

        public static Expression Pow(Expression @base, Expression exponent) => Bind(WKF.Exponential.Pow, @base, exponent);

        public static Expression Exp(Expression exponent) => Bind(WKF.Exponential.Exp, exponent);

        public static Expression Sqrt(Expression @base) => Bind(WKF.Exponential.Sqrt, @base);

        public static Expression Log(Expression expression) => Bind(WKF.Exponential.Ln, expression);

        public static Expression Sin(Expression expression) => Bind(WKF.Trigonometric.Sine, expression);

        public static Expression Cos(Expression expression) => Bind(WKF.Trigonometric.Cosine, expression);

        public static Expression Tan(Expression expression) => Bind(WKF.Trigonometric.Tangent, expression);

        public static Expression Sinh(Expression expression) => Bind(WKF.Hyperbolic.Sine, expression);

        public static Expression Cosh(Expression expression) => Bind(WKF.Hyperbolic.Cosine, expression);

        public static Expression Tanh(Expression expression) => Bind(WKF.Hyperbolic.Tangent, expression);

        public static Expression Compare(Expression left, ExpressionType op, Expression right)
        {
            return op is ExpressionType.Equal or ExpressionType.NotEqual
                ? Expression.MakeBinary(op, ConvertIfLower(left, to: right), ConvertIfLower(right, to: left))
                : Expression.MakeBinary(op, LowerToReal(left), LowerToReal(right));
        }

        public static Expression Function(string name, IList<Expression> arguments)
        {
            return Bind(name, arguments);
        }

        public static Expression Bind(string name, params Expression[] arguments) => Bind(name, (IList<Expression>)arguments);

        public static Expression Bind(string name, IList<Expression> arguments)
        {
            if (!DefaultScope.NamedFunctions.TryGetValue(name, out var function))
            {
                throw new MissingMethodException($"Could not find a binding for '{name}'.");
            }

            return Bind(function, arguments);
        }

        public static Expression Bind(KnownFunction function, params Expression[] arguments) => Bind(function, (IList<Expression>)arguments);

        public static Expression Bind(KnownFunction function, IList<Expression> arguments)
        {
            if (function == WKF.Exponential.Sqrt && arguments.Count == 1)
            {
                var @base = arguments[0];
                if (!TryConvert(@base, false, (double value) => value >= 0))
                {
                    arguments = [ConvertIfLower(@base, to: typeof(Complex))];
                }
            }

            var match = (from known in DefaultScope.KnownMethods
                         where known.Value == function
                         let m = known.Key
                         let mp = m.Parameters
                         where mp.Count == arguments.Count
                         let parameterMatches = (from i in Enumerable.Range(0, arguments.Count)
                                                 select new
                                                 {
                                                     ParameterType = mp[i].Type,
                                                     Assignable = mp[i].Type.IsAssignableFrom(arguments[i].Type),
                                                     Convertible = mp[i].Type == typeof(Complex), // TODO: This should probably look for registered converters.
                                                 }).ToList()
                         where parameterMatches.All(p => p.Assignable || p.Convertible)
                         orderby parameterMatches.Count(p => !p.Assignable) ascending,
                                 m.Body is MethodCallExpression ascending
                         select (m, parameterMatches)).First();

            var (lambda, parameters) = match;

            return new ReplaceVisitor(
                Enumerable.Range(0, arguments.Count)
                    .ToDictionary(
                        i => (Expression)lambda.Parameters[i],
                        i => parameters[i].Assignable ? arguments[i] : ConvertIfLower(arguments[i], to: parameters[i].ParameterType)))
                .Visit(lambda.Body);
        }

        public static void Bind(MethodCallExpression methodCall, out KnownFunction knownMethod, out IList<Expression>? arguments)
        {
            if (!TryBind(methodCall, out knownMethod, out arguments))
            {
                throw new MissingMethodException($"Could not find a binding for '{methodCall.Method}'.");
            }
        }

        public static bool TryBind(MethodCallExpression methodCall, [NotNullWhen(true)] out KnownFunction? knownMethod, [NotNullWhen(true)] out IList<Expression>? arguments)
        {
            var method = methodCall.Method;
            if (TryBind(method, out knownMethod))
            {
                arguments = method.IsStatic ? methodCall.Arguments : [methodCall.Object!, .. methodCall.Arguments];
                return true;
            }

            arguments = null;
            return false;
        }

        public static void Bind(MethodInfo method, out KnownFunction knownMethod)
        {
            if (!TryBind(method, out knownMethod))
            {
                throw new MissingMethodException($"Could not find a binding for '{method}'.");
            }
        }

        public static bool TryBind(MethodInfo method, [NotNullWhen(true)] out KnownFunction? knownMethod)
        {
            knownMethod = (from known in DefaultScope.KnownMethods
                           where known.Key.Body is MethodCallExpression methodCall && methodCall.Method == method
                           select known.Value).Distinct().SingleOrDefault();

            return knownMethod != null;
        }

        public static bool IsConstantValue(Expression expression, [NotNullWhen(true)] out ConstantExpression? constantExpression)
        {
            if (expression is ConstantExpression outerConstant)
            {
                constantExpression = outerConstant;
                return true;
            }

            if (expression is UnaryExpression unary && unary.NodeType == ExpressionType.Convert)
            {
                return IsConstantValue(unary.Operand, out constantExpression);
            }

            constantExpression = null;
            return false;
        }

        public static TOut TryConvert<TIn, TOut>(Expression expression, TOut @default, Func<TIn, TOut> convert)
        {
            if (IsConstantValue(expression, out var constant))
            {
                return TryConvert(constant, @default, convert);
            }

            return @default;
        }

        public static TOut TryConvert<TIn, TOut>(ConstantExpression constant, TOut @default, Func<TIn, TOut> convert)
        {
            if (constant.Value is TIn value)
            {
                return convert(value);
            }

            return @default;
        }

        public static bool IsPower(Expression expression, [NotNullWhen(true)] out Expression? @base, [NotNullWhen(true)] out Expression? exponent)
        {
            if (expression.NodeType == ExpressionType.Power && expression is BinaryExpression binary)
            {
                @base = binary.Left;
                exponent = binary.Right;
                return true;
            }

            if (expression.NodeType == ExpressionType.Call && expression is MethodCallExpression methodCall &&
                TryBind(methodCall, out var knowFunction, out var arguments))
            {
                if (knowFunction == WKF.Exponential.Pow && arguments.Count == 2)
                {
                    @base = arguments[0];
                    exponent = arguments[1];
                    return true;
                }
            }

            @base = null;
            exponent = null;
            return false;
        }

        public static bool IsSqrt(Expression expression, [NotNullWhen(true)] out Expression? @base)
        {
            if (expression.NodeType == ExpressionType.Call && expression is MethodCallExpression methodCall &&
                TryBind(methodCall, out var knowFunction, out var arguments))
            {
                if (knowFunction == WKF.Exponential.Sqrt && arguments.Count == 1)
                {
                    @base = arguments[0];
                    return true;
                }
            }

            @base = null;
            return false;
        }

        public static bool IsConstraint(Expression expression, [NotNullWhen(true)] out Expression? condition, [NotNullWhen(true)] out Expression? consequent)
        {
            if (expression.NodeType == ExpressionType.Conditional &&
                expression is ConditionalExpression conditional &&
                IsNaN(conditional.IfFalse))
            {
                condition = conditional.Test;
                consequent = conditional.IfTrue;
                return true;
            }

            condition = null;
            consequent = null;
            return false;
        }

        public static bool IsConstantEqual(Expression expression, double value) =>
            TryConvert(expression, false, (int x) => x == value) ||
            TryConvert(expression, false, (float x) => x == value) ||
            TryConvert(expression, false, (double x) => x == value) ||
            TryConvert(expression, false, (Complex x) => x == value);

        public static bool IsNaN(Expression expression) =>
            TryConvert(expression, false, (float x) => float.IsNaN(x)) ||
            TryConvert(expression, false, (double x) => double.IsNaN(x)) ||
            TryConvert(expression, false, (Complex x) => Complex.IsNaN(x));

        public static bool IsOne(Expression expression) => IsConstantEqual(expression, 1);

        public static bool IsZero(Expression expression) => IsConstantEqual(expression, 0);

        public static bool IsTrue(Expression expression) => TryConvert(expression, false, (bool b) => b);

        public static bool IsFalse(Expression expression) => TryConvert(expression, false, (bool b) => !b);
    }
}
