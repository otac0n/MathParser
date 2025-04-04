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
        public static Expression Simplify(this Scope scope, Expression expression)
        {
            return new SimplifyVisitor(scope).Visit(expression);
        }

        public static LambdaExpression Derivative(this Scope scope, LambdaExpression expression)
        {
            var variable = expression.Parameters.Single();

            // TODO: Should the name be altered, e.g. `f'`?
            return Expression.Lambda(scope.Simplify(scope.Derivative(expression.Body, variable)), expression.Name, expression.TailCall, [variable]);
        }

        public static Expression Derivative(this Scope scope, Expression expression, ParameterExpression variable) =>
            expression.NodeType switch
            {
                ExpressionType.Parameter when expression is ParameterExpression parameter => parameter == variable ? scope.One() : scope.Zero(),
                ExpressionType.MemberAccess when expression is MemberExpression member => scope.Zero(),
                ExpressionType.Constant when expression is ConstantExpression constant => scope.IsNaN(constant) ? constant : scope.Zero(), // TODO: Does constant infinity have a derivative?
                ExpressionType.Convert when expression is UnaryExpression unary => Expression.Convert(scope.Derivative(unary.Operand, variable), unary.Type),
                ExpressionType.Conditional when expression is ConditionalExpression conditional => scope.Conditional(conditional.Test, scope.Derivative(conditional.IfTrue, variable), scope.Derivative(conditional.IfFalse, variable)),
                ExpressionType.Negate when expression is UnaryExpression unary => scope.Negate(scope.Derivative(unary.Operand, variable)),
                ExpressionType.Add when expression is BinaryExpression binary => scope.Add(scope.Derivative(binary.Left, variable), scope.Derivative(binary.Right, variable)),
                ExpressionType.Subtract when expression is BinaryExpression binary => scope.Subtract(scope.Derivative(binary.Left, variable), scope.Derivative(binary.Right, variable)),
                ExpressionType.Multiply when expression is BinaryExpression binary => scope.Add(scope.Multiply(scope.Derivative(binary.Left, variable), binary.Right), scope.Multiply(binary.Left, scope.Derivative(binary.Right, variable))),
                ExpressionType.Divide when expression is BinaryExpression binary => scope.Divide(scope.Subtract(scope.Multiply(scope.Derivative(binary.Left, variable), binary.Right), scope.Multiply(binary.Left, scope.Derivative(binary.Right, variable))), scope.Pow(binary.Right, Expression.Constant(2.0))),
                ExpressionType.Call when expression is MethodCallExpression methodCall && methodCall.Object is null && (methodCall.Method.DeclaringType == typeof(Math) || methodCall.Method.DeclaringType == typeof(Complex)) => methodCall.Method.Name switch
                {
                    nameof(Math.Abs) when methodCall.Arguments.Count == 1 => scope.Multiply(scope.Derivative(methodCall.Arguments[0], variable), scope.Divide(expression, methodCall.Arguments[0])),
                    nameof(Math.Sin) when methodCall.Arguments.Count == 1 => scope.Multiply(scope.Derivative(methodCall.Arguments[0], variable), scope.Cos(methodCall.Arguments[0])),
                    nameof(Math.Cos) when methodCall.Arguments.Count == 1 => scope.Multiply(scope.Derivative(methodCall.Arguments[0], variable), scope.Negate(scope.Sin(methodCall.Arguments[0]))),
                    nameof(Math.Tan) when methodCall.Arguments.Count == 1 => scope.Divide(scope.Derivative(methodCall.Arguments[0], variable), scope.Pow(scope.Cos(methodCall.Arguments[0]), Expression.Constant(2.0))),
                    nameof(Math.Asin) when methodCall.Arguments.Count == 1 => scope.Divide(scope.Derivative(methodCall.Arguments[0], variable), scope.Sqrt(scope.Subtract(Expression.Constant(1.0), scope.Pow(methodCall.Arguments[0], Expression.Constant(2.0))))),
                    nameof(Math.Acos) when methodCall.Arguments.Count == 1 => scope.Divide(scope.Derivative(methodCall.Arguments[0], variable), scope.Negate(scope.Sqrt(scope.Subtract(Expression.Constant(1.0), scope.Pow(methodCall.Arguments[0], Expression.Constant(2.0)))))),
                    nameof(Math.Atan) when methodCall.Arguments.Count == 1 => scope.Divide(scope.Derivative(methodCall.Arguments[0], variable), scope.Add(scope.Pow(methodCall.Arguments[0], Expression.Constant(2.0)), Expression.Constant(1.0))),
                    nameof(Math.Sinh) when methodCall.Arguments.Count == 1 => scope.Multiply(scope.Derivative(methodCall.Arguments[0], variable), scope.Cosh(methodCall.Arguments[0])),
                    nameof(Math.Cosh) when methodCall.Arguments.Count == 1 => scope.Multiply(scope.Derivative(methodCall.Arguments[0], variable), scope.Sinh(methodCall.Arguments[0])),
                    nameof(Math.Tanh) when methodCall.Arguments.Count == 1 => scope.Divide(scope.Derivative(methodCall.Arguments[0], variable), scope.Pow(scope.Cosh(methodCall.Arguments[0]), Expression.Constant(2.0))),
                    nameof(Math.Asinh) when methodCall.Arguments.Count == 1 => scope.Divide(scope.Derivative(methodCall.Arguments[0], variable), scope.Sqrt(scope.Add(scope.Pow(methodCall.Arguments[0], Expression.Constant(2.0)), Expression.Constant(1.0)))),
                    nameof(Math.Acosh) when methodCall.Arguments.Count == 1 => scope.Divide(scope.Derivative(methodCall.Arguments[0], variable), scope.Sqrt(scope.Subtract(scope.Pow(methodCall.Arguments[0], Expression.Constant(2.0)), Expression.Constant(1.0)))), // TODO: Domain of the function is Reals > 1
                    nameof(Math.Atanh) when methodCall.Arguments.Count == 1 => scope.Divide(scope.Derivative(methodCall.Arguments[0], variable), scope.Subtract(Expression.Constant(1.0), scope.Pow(methodCall.Arguments[0], Expression.Constant(2.0)))), // TODO: Domain of the function is |Reals| < 1
                    nameof(Math.Sqrt) when methodCall.Arguments.Count == 1 => scope.Multiply(scope.Derivative(methodCall.Arguments[0], variable), scope.Multiply(Expression.Constant(0.5), scope.Divide(expression, methodCall.Arguments[0]))),
                    nameof(Math.Exp) when methodCall.Arguments.Count == 1 => scope.Multiply(scope.Derivative(methodCall.Arguments[0], variable), expression),
                    nameof(Math.Log) when methodCall.Arguments.Count == 1 => scope.Divide(scope.Derivative(methodCall.Arguments[0], variable), methodCall.Arguments[0]), // TODO: Domain of the function is Reals > 0.
                    nameof(Math.Pow) when methodCall.Arguments.Count == 2 =>
                        scope.IsConstantValue(methodCall.Arguments[1], out var constant)
                            ? scope.Multiply(scope.Derivative(methodCall.Arguments[0], variable), scope.Multiply(constant, scope.Pow(methodCall.Arguments[0], scope.Subtract(constant, scope.One()))))
                            : scope.Multiply(expression, scope.Derivative(scope.Multiply(scope.Log(methodCall.Arguments[0]), methodCall.Arguments[1]), variable)),
                    _ => throw new NotImplementedException($"The method, {methodCall.Method}, is not implemented."),
                },
                ExpressionType.Power when expression is BinaryExpression binary =>
                        scope.IsConstantValue(binary.Right, out var constant)
                            ? scope.Multiply(scope.Derivative(binary.Left, variable), scope.Multiply(constant, scope.Pow(binary.Left, scope.Subtract(constant, scope.One()))))
                            : scope.Multiply(expression, scope.Derivative(scope.Multiply(scope.Log(binary.Left), binary.Right), variable)),
                _ => throw new NotImplementedException($"The {expression.NodeType}, {expression}, is not implemented."),
            };

        public static ConstantExpression Zero(this Scope scope) => Expression.Constant(0.0);

        public static ConstantExpression One(this Scope scope) => Expression.Constant(1.0);

        public static ConstantExpression NaN(this Scope scope) => Expression.Constant(double.NaN);

        public static Expression ConvertIfLower(this Scope scope, Expression expression, Expression to) => scope.ConvertIfLower(expression, to: to.Type);

        public static Expression ConvertIfLower(this Scope scope, Expression expression, Type to)
        {
            var from = expression.Type;
            if (to == typeof(Complex) && from != typeof(Complex))
            {
                return Expression.Convert(expression, to);
            }

            return expression;
        }

        public static Expression And(this Scope scope, Expression left, Expression right) => scope.Bind(WKF.Boolean.And, left, right);

        public static bool MatchAnd(this Scope scope, Expression? expression, [NotNullWhen(true)] out Expression? left, [NotNullWhen(true)] out Expression? right) =>
            scope.MatchKnownBinary(WKF.Boolean.And, expression, out left, out right);

        public static Expression Or(this Scope scope, Expression left, Expression right) => scope.Bind(WKF.Boolean.Or, left, right);

        public static bool MatchOr(this Scope scope, Expression? expression, [NotNullWhen(true)] out Expression? left, [NotNullWhen(true)] out Expression? right) =>
            scope.MatchKnownBinary(WKF.Boolean.Or, expression, out left, out right);

        public static Expression Not(this Scope scope, Expression expression) => scope.Bind(WKF.Boolean.Not, expression);

        public static bool MatchNot(this Scope scope, Expression? expression, [NotNullWhen(true)] out Expression? operand) =>
            scope.MatchKnownUnary(WKF.Boolean.Not, expression, out operand);

        public static Expression Equal(this Scope scope, Expression left, Expression right) => Expression.Equal(scope.ConvertIfLower(left, to: right), scope.ConvertIfLower(right, to: left));

        public static Expression NotEqual(this Scope scope, Expression left, Expression right) => Expression.NotEqual(scope.ConvertIfLower(left, to: right), scope.ConvertIfLower(right, to: left));

        public static Expression LowerToReal(this Scope scope, Expression expression)
        {
            var from = expression.Type;
            if (from == typeof(Complex))
            {
                return Expression.MakeMemberAccess(expression, typeof(Complex).GetProperty(nameof(Complex.Real), BindingFlags.Public | BindingFlags.Instance));
            }

            return expression;
        }

        public static Expression Abs(this Scope scope, Expression expression) => scope.Bind(WKF.Piecewise.Abs, expression);

        public static Expression Conditional(this Scope scope, Expression condition, Expression consequent, Expression alternative) =>
            Expression.Condition(condition, scope.ConvertIfLower(consequent, to: alternative), scope.ConvertIfLower(alternative, to: consequent));

        public static Expression Ceiling(this Scope scope, Expression expression) => scope.Bind(WKF.Piecewise.Ceiling, expression);

        public static Expression Floor(this Scope scope, Expression expression) => scope.Bind(WKF.Piecewise.Floor, expression);

        public static Expression Negate(this Scope scope, Expression operand) => scope.Bind(WKF.Arithmetic.Negate, operand);

        public static bool MatchNegate(this Scope scope, Expression? expression, [NotNullWhen(true)] out Expression? operand) =>
            scope.MatchKnownUnary(WKF.Arithmetic.Negate, expression, out operand);

        public static Expression Add(this Scope scope, Expression augend, Expression addend) => scope.Bind(WKF.Arithmetic.Add, augend, addend);

        public static bool MatchAdd(this Scope scope, Expression? sum, [NotNullWhen(true)] out Expression? augend, [NotNullWhen(true)] out Expression? addend) =>
            scope.MatchKnownBinary(WKF.Arithmetic.Add, sum, out augend, out addend);

        public static Expression Subtract(this Scope scope, Expression minuend, Expression subtrahend) => scope.Bind(WKF.Arithmetic.Subtract, minuend, subtrahend);

        public static bool MatchSubtract(this Scope scope, Expression? difference, [NotNullWhen(true)] out Expression? minuend, [NotNullWhen(true)] out Expression? subtrahend) =>
            scope.MatchKnownBinary(WKF.Arithmetic.Subtract, difference, out minuend, out subtrahend);

        public static Expression Multiply(this Scope scope, Expression multiplicand, Expression multiplier) => scope.Bind(WKF.Arithmetic.Multiply, multiplicand, multiplier);

        public static bool MatchMultiply(this Scope scope, Expression? product, [NotNullWhen(true)] out Expression? multiplicand, [NotNullWhen(true)] out Expression? multiplier) =>
            scope.MatchKnownBinary(WKF.Arithmetic.Multiply, product, out multiplicand, out multiplier);

        public static Expression Divide(this Scope scope, Expression dividend, Expression divisor) => scope.Bind(WKF.Arithmetic.Divide, dividend, divisor);

        public static bool MatchDivide(this Scope scope, Expression? quotient, [NotNullWhen(true)] out Expression? dividend, [NotNullWhen(true)] out Expression? divisor) =>
            scope.MatchKnownBinary(WKF.Arithmetic.Divide, quotient, out dividend, out divisor);

        public static Expression Pow(this Scope scope, Expression @base, Expression exponent) => scope.Bind(WKF.Exponential.Pow, @base, exponent);

        public static bool MatchPower(this Scope scope, Expression? expression, [NotNullWhen(true)] out Expression? @base, [NotNullWhen(true)] out Expression? exponent) =>
            scope.MatchKnownBinary(WKF.Exponential.Pow, expression, out @base, out exponent);

        public static Expression Exp(this Scope scope, Expression exponent) => scope.Bind(WKF.Exponential.Exp, exponent);

        public static bool MatchExp(this Scope scope, Expression? expression, [NotNullWhen(true)] out Expression? exponent) =>
            scope.MatchKnownUnary(WKF.Exponential.Exp, expression, out exponent);

        public static Expression Sqrt(this Scope scope, Expression @base) => scope.Bind(WKF.Exponential.Sqrt, @base);

        public static bool MatchSqrt(this Scope scope, Expression? expression, [NotNullWhen(true)] out Expression? @base) =>
            scope.MatchKnownUnary(WKF.Exponential.Sqrt, expression, out @base);

        public static Expression Log(this Scope scope, Expression expression) => scope.Bind(WKF.Exponential.Ln, expression);

        public static Expression Sin(this Scope scope, Expression expression) => scope.Bind(WKF.Trigonometric.Sine, expression);

        public static Expression Cos(this Scope scope, Expression expression) => scope.Bind(WKF.Trigonometric.Cosine, expression);

        public static Expression Tan(this Scope scope, Expression expression) => scope.Bind(WKF.Trigonometric.Tangent, expression);

        public static Expression Sinh(this Scope scope, Expression expression) => scope.Bind(WKF.Hyperbolic.Sine, expression);

        public static Expression Cosh(this Scope scope, Expression expression) => scope.Bind(WKF.Hyperbolic.Cosine, expression);

        public static Expression Tanh(this Scope scope, Expression expression) => scope.Bind(WKF.Hyperbolic.Tangent, expression);

        public static Expression Compare(this Scope scope, Expression left, ExpressionType op, Expression right)
        {
            return op is ExpressionType.Equal or ExpressionType.NotEqual
                ? Expression.MakeBinary(op, scope.ConvertIfLower(left, to: right), scope.ConvertIfLower(right, to: left))
                : Expression.MakeBinary(op, scope.LowerToReal(left), scope.LowerToReal(right));
        }

        public static Expression Function(this Scope scope, string name, IList<Expression> arguments) => scope.Bind(name, arguments);

        public static bool IsConstantValue(this Scope scope, Expression expression, [NotNullWhen(true)] out ConstantExpression? constantExpression)
        {
            if (expression is ConstantExpression outerConstant)
            {
                constantExpression = outerConstant;
                return true;
            }

            if (expression is UnaryExpression unary && unary.NodeType == ExpressionType.Convert)
            {
                return scope.IsConstantValue(unary.Operand, out constantExpression);
            }

            constantExpression = null;
            return false;
        }

        public static TOut TryConvert<TIn, TOut>(this Scope scope, Expression expression, TOut @default, Func<TIn, TOut> convert)
        {
            if (scope.IsConstantValue(expression, out var constant))
            {
                return scope.TryConvert(constant, @default, convert);
            }

            return @default;
        }

        public static TOut TryConvert<TIn, TOut>(this Scope scope, ConstantExpression constant, TOut @default, Func<TIn, TOut> convert)
        {
            if (constant.Value is TIn value)
            {
                return convert(value);
            }

            return @default;
        }

        private static bool MatchKnownBinary(this Scope scope, KnownFunction knownFunction, [NotNullWhen(true)] Expression? result, [NotNullWhen(true)] out Expression? left, [NotNullWhen(true)] out Expression? right)
        {
            if (scope.TryBind(result, out var foundFunction, out var arguments) && foundFunction == knownFunction && arguments.Count == 2)
            {
                left = arguments[0];
                right = arguments[1];
                return true;
            }

            (left, right) = (null, null);
            return false;
        }

        private static bool MatchKnownUnary(this Scope scope, KnownFunction knownFunction, [NotNullWhen(true)] Expression? result, [NotNullWhen(true)] out Expression? operand)
        {
            if (scope.TryBind(result, out var foundFunction, out var arguments) && foundFunction == knownFunction && arguments.Count == 1)
            {
                operand = arguments[0];
                return true;
            }

            operand = null;
            return false;
        }

        public static bool MatchConstraint(this Scope scope, Expression expression, [NotNullWhen(true)] out Expression? condition, [NotNullWhen(true)] out Expression? consequent)
        {
            if (expression.NodeType == ExpressionType.Conditional &&
                expression is ConditionalExpression conditional &&
                scope.IsNaN(conditional.IfFalse))
            {
                condition = conditional.Test;
                consequent = conditional.IfTrue;
                return true;
            }

            condition = null;
            consequent = null;
            return false;
        }

        public static bool IsConstantEqual(this Scope scope, Expression expression, double value) =>
            scope.TryConvert(expression, false, (int x) => x == value) ||
            scope.TryConvert(expression, false, (float x) => x == value) ||
            scope.TryConvert(expression, false, (double x) => x == value) ||
            scope.TryConvert(expression, false, (Complex x) => x == value);

        public static bool IsNaN(this Scope scope, Expression expression) =>
            scope.TryConvert(expression, false, (float x) => float.IsNaN(x)) ||
            scope.TryConvert(expression, false, (double x) => double.IsNaN(x)) ||
            scope.TryConvert(expression, false, (Complex x) => Complex.IsNaN(x));

        public static bool IsOne(this Scope scope, Expression expression) => scope.IsConstantEqual(expression, 1);

        public static bool IsZero(this Scope scope, Expression expression) => scope.IsConstantEqual(expression, 0);

        public static bool IsTrue(this Scope scope, Expression expression) => scope.TryConvert(expression, false, (bool b) => b);

        public static bool IsFalse(this Scope scope, Expression expression) => scope.TryConvert(expression, false, (bool b) => !b);
    }
}
