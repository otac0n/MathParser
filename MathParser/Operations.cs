// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Numerics;
    using System.Reflection;

    public static class Operations
    {
        public static Expression Simplify(Expression expression)
        {
            return new SimplifyVisitor().Visit(expression);
        }

        public static Expression<Func<T, T>> Derivative<T>(Expression<Func<T, T>> expression)
        {
            var variable = expression.Parameters.Single();
            return (Expression<Func<T, T>>)Expression.Lambda(Simplify(Derivative(expression.Body, variable)), variable);
        }

        public static Expression Derivative(Expression expression, ParameterExpression variable) =>
            expression.NodeType switch
            {
                // TODO: We may need a way to communicate the higher order derivatives of parameters, fields, properties, etc. that change over time.
                ExpressionType.Parameter when expression is ParameterExpression parameter => parameter == variable ? One() : Zero(), // TODO: Check for NaN, Inf, etc.
                ExpressionType.MemberAccess when expression is MemberExpression member => Zero(),
                ExpressionType.Constant when expression is ConstantExpression constant => Zero(),
                ExpressionType.Negate when expression is UnaryExpression unary => Negate(Derivative(unary.Operand, variable)),
                ExpressionType.Add when expression is BinaryExpression binary => Add(Derivative(binary.Left, variable), Derivative(binary.Right, variable)),
                ExpressionType.Subtract when expression is BinaryExpression binary => Subtract(Derivative(binary.Left, variable), Derivative(binary.Right, variable)),
                ExpressionType.Multiply when expression is BinaryExpression binary => Add(Multiply(Derivative(binary.Left, variable), binary.Right), Multiply(binary.Left, Derivative(binary.Right, variable))),
                ExpressionType.Divide when expression is BinaryExpression binary => Divide(Subtract(Multiply(Derivative(binary.Left, variable), binary.Right), Multiply(binary.Left, Derivative(binary.Right, variable))), Pow(binary.Right, Expression.Constant(2.0))),
                ExpressionType.Call when expression is MethodCallExpression methodCall && methodCall.Object is null && (methodCall.Method.DeclaringType == typeof(Math) || methodCall.Method.DeclaringType == typeof(Complex)) => methodCall.Method.Name switch
                {
                    nameof(Math.Abs) when methodCall.Arguments.Count == 1 => Multiply(Derivative(methodCall.Arguments[0], variable), Divide(expression, methodCall.Arguments[0])),
                    nameof(Math.Sin) when methodCall.Arguments.Count == 1 => Multiply(Derivative(methodCall.Arguments[0], variable), MathMethod(nameof(Math.Cos), methodCall.Arguments[0])),
                    nameof(Math.Cos) when methodCall.Arguments.Count == 1 => Multiply(Derivative(methodCall.Arguments[0], variable), Negate(MathMethod(nameof(Math.Sin), methodCall.Arguments[0]))),
                    nameof(Math.Tan) when methodCall.Arguments.Count == 1 => Divide(Derivative(methodCall.Arguments[0], variable), Pow(MathMethod(nameof(Math.Cos), methodCall.Arguments[0]), Expression.Constant(2.0))),
                    nameof(Math.Asin) when methodCall.Arguments.Count == 1 => Divide(Derivative(methodCall.Arguments[0], variable), Sqrt(Subtract(Expression.Constant(1.0), Pow(methodCall.Arguments[0], Expression.Constant(2.0))))),
                    nameof(Math.Acos) when methodCall.Arguments.Count == 1 => Divide(Derivative(methodCall.Arguments[0], variable), Negate(Sqrt(Subtract(Expression.Constant(1.0), Pow(methodCall.Arguments[0], Expression.Constant(2.0)))))),
                    nameof(Math.Atan) when methodCall.Arguments.Count == 1 => Divide(Derivative(methodCall.Arguments[0], variable), Add(Pow(methodCall.Arguments[0], Expression.Constant(2.0)), Expression.Constant(1.0))),
                    nameof(Math.Sinh) when methodCall.Arguments.Count == 1 => Multiply(Derivative(methodCall.Arguments[0], variable), MathMethod(nameof(Math.Cosh), methodCall.Arguments[0])),
                    nameof(Math.Cosh) when methodCall.Arguments.Count == 1 => Multiply(Derivative(methodCall.Arguments[0], variable), MathMethod(nameof(Math.Sinh), methodCall.Arguments[0])),
                    nameof(Math.Tanh) when methodCall.Arguments.Count == 1 => Divide(Derivative(methodCall.Arguments[0], variable), Pow(MathMethod(nameof(Math.Cosh), methodCall.Arguments[0]), Expression.Constant(2.0))),
                    nameof(Math.Asinh) when methodCall.Arguments.Count == 1 => Divide(Derivative(methodCall.Arguments[0], variable), Sqrt(Add(Pow(methodCall.Arguments[0], Expression.Constant(2.0)), Expression.Constant(1.0)))),
                    nameof(Math.Acosh) when methodCall.Arguments.Count == 1 => Divide(Derivative(methodCall.Arguments[0], variable), Sqrt(Subtract(Pow(methodCall.Arguments[0], Expression.Constant(2.0)), Expression.Constant(1.0)))), // TODO: Domain of the function is Reals > 1
                    nameof(Math.Atanh) when methodCall.Arguments.Count == 1 => Divide(Derivative(methodCall.Arguments[0], variable), Subtract(Expression.Constant(1.0), Pow(methodCall.Arguments[0], Expression.Constant(2.0)))), // TODO: Domain of the function is |Reals| < 1
                    nameof(Math.Sqrt) when methodCall.Arguments.Count == 1 => Multiply(Derivative(methodCall.Arguments[0], variable), Multiply(Expression.Constant(0.5), Divide(expression, methodCall.Arguments[0]))),
                    nameof(Math.Exp) when methodCall.Arguments.Count == 1 => Multiply(Derivative(methodCall.Arguments[0], variable), expression),
                    nameof(Math.Log) when methodCall.Arguments.Count == 1 => Divide(Derivative(methodCall.Arguments[0], variable), methodCall.Arguments[0]), // TODO: Domain of the function is Reals > 0.
                    nameof(Math.Pow) when methodCall.Arguments.Count == 2 => Multiply(expression, Add(Multiply(Derivative(methodCall.Arguments[1], variable), MathMethod(nameof(Math.Log), methodCall.Arguments[0])), Multiply(Derivative(methodCall.Arguments[0], variable), Divide(methodCall.Arguments[1], methodCall.Arguments[0])))),
                    _ => throw new NotImplementedException($"The method, {methodCall.Method}, is not implemented."),
                },
                _ => throw new NotImplementedException($"The {expression.NodeType}, {expression}, is not implemented."),
            };

        public static ConstantExpression Zero() => Expression.Constant(0.0);

        public static ConstantExpression One() => Expression.Constant(1.0);

        public static Expression Negate(Expression a)
        {
            if (IsZero(a))
            {
                return a;
            }

            return Expression.Negate(a);
        }

        public static Expression Add(Expression a, Expression b)
        {
            if (IsZero(a))
            {
                return b;
            }

            if (IsZero(b))
            {
                return a;
            }

            return Expression.Add(a, b);
        }

        public static Expression Subtract(Expression a, Expression b)
        {
            if (IsZero(a))
            {
                return Expression.Negate(b);
            }

            if (IsZero(b))
            {
                return a;
            }

            return Expression.Subtract(a, b);
        }

        public static Expression Multiply(Expression a, Expression b)
        {
            if (IsZero(a))
            {
                return a;
            }
            else if (IsOne(a))
            {
                return b;
            }

            if (IsZero(b))
            {
                return b;
            }
            else if (IsOne(b))
            {
                return a;
            }

            return Expression.Multiply(a, b);
        }

        public static Expression Divide(Expression a, Expression b)
        {
            if (IsZero(b))
            {
                return Expression.Divide(a, b);
            }
            else if (IsOne(b))
            {
                return a;
            }

            if (IsZero(a))
            {
                return a;
            }

            return Expression.Divide(a, b);
        }

        public static Expression Pow(Expression @base, Expression exponent)
        {
            return Expression.Power(@base, exponent);
        }

        public static Expression Sqrt(Expression inner)
        {
            return MathMethod(nameof(Math.Sqrt), inner);
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

        public static bool IsConstantEqual(Expression expression, double value) =>
            TryConvert(expression, false, (int x) => x == value) ||
            TryConvert(expression, false, (double x) => x == value) ||
            TryConvert(expression, false, (Complex x) => x == value);

        public static bool IsOne(Expression expression) => IsConstantEqual(expression, 1);

        public static bool IsZero(Expression expression) => IsConstantEqual(expression, 0);

        private static MethodCallExpression MathMethod(string name, params Expression[] arguments)
        {
            var types = Array.ConvertAll(arguments, a => a.Type);
            var method = typeof(Math).GetMethod(name, BindingFlags.Public | BindingFlags.Static, types);
            return Expression.Call(null, method, arguments);
        }
    }
}
