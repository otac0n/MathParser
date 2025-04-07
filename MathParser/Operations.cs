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
    using WKC = WellKnownConstants;
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
            return Expression.Lambda(scope.Simplify(scope.Derivative(expression.Body, variable)), expression.Name + "'", expression.TailCall, [variable]);
        }

        public static Expression Derivative(this Scope scope, Expression expression, ParameterExpression variable)
        {
            return new DerivativeVisitor(scope, variable).Visit(expression);
        }

        public static Expression Zero(this Scope scope) => scope.BindConstant(WKC.Zero);

        public static Expression One(this Scope scope) => scope.BindConstant(WKC.One);

        public static Expression NegativeOne(this Scope scope) => scope.TryBindConstant(WKC.NegativeOne, out var constant) ? constant : scope.Negate(scope.One());

        public static ConstantExpression NaN(this Scope scope) => Expression.Constant(double.NaN);

        public static Expression Tau(this Scope scope) => scope.BindConstant(WKC.Tau);

        public static Expression Pi(this Scope scope) => scope.BindConstant(WKC.Pi);

        public static Expression E(this Scope scope) => scope.BindConstant(WKC.EulersNumber);

        public static Expression I(this Scope scope) => scope.BindConstant(WKC.I);

        public static Expression Inf(this Scope scope) => scope.BindConstant(WKC.PositiveInfinity);

        public static Expression ConvertIfLower(this Scope scope, Expression expression, Expression to) => scope.ConvertIfLower(expression, to: to.Type);

        public static Expression ConvertIfLower(this Scope scope, Expression expression, Type to)
        {
            var from = expression.Type;

            var target = scope.FindLargest(from, to) ?? throw new NotSupportedException($"Could not find a conversion between '{from}' and '{to}'.");
            if (from == target)
            {
                return expression;
            }

            return Expression.Convert(expression, to);
        }

        public static Expression LowerToReal(this Scope scope, Expression expression)
        {
            var from = expression.Type;
            if (from == typeof(Complex))
            {
                return Expression.MakeMemberAccess(expression, typeof(Complex).GetProperty(nameof(Complex.Real), BindingFlags.Public | BindingFlags.Instance));
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

        public static Expression Conditional(this Scope scope, Expression condition, Expression consequent, Expression alternative) =>
            Expression.Condition(condition, scope.ConvertIfLower(consequent, to: alternative), scope.ConvertIfLower(alternative, to: consequent));

        public static Expression Constraint(this Scope scope, Expression condition, Expression consequent) =>
            scope.Conditional(condition, consequent, scope.NaN());

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

        public static Expression Abs(this Scope scope, Expression expression) => scope.Bind(WKF.Piecewise.Abs, expression);

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

        public static Expression Reciprocal(this Scope scope, Expression denominator) => scope.Bind(WKF.Arithmetic.Reciprocal, denominator);

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

        private static bool MatchKnownConstant(this Scope scope, KnownConstant knownConstant, [NotNullWhen(true)] Expression? result)
        {
            if (scope.TryBind(result, out var foundConstant) && foundConstant == knownConstant)
            {
                return true;
            }

            return false;
        }

        public static bool IsConstant(this Scope scope, Expression expression) =>
            scope.TryBind(expression, out _) ||
            scope.IsConstantValue(expression, out _);

        public static bool IsConstantEqual(this Scope scope, Expression expression, double value) =>
            scope.TryConvert(expression, false, (int x) => x == value) ||
            scope.TryConvert(expression, false, (float x) => x == value) ||
            scope.TryConvert(expression, false, (double x) => x == value) ||
            scope.TryConvert(expression, false, (Complex x) => x == value);

        public static bool IsNaN(this Scope scope, Expression expression) =>
            scope.MatchKnownConstant(WKC.Indeterminate, expression) ||
            scope.TryConvert(expression, false, (float x) => float.IsNaN(x)) ||
            scope.TryConvert(expression, false, (double x) => double.IsNaN(x)) ||
            scope.TryConvert(expression, false, (Complex x) => Complex.IsNaN(x));

        public static bool IsOne(this Scope scope, Expression expression) => scope.MatchKnownConstant(WKC.One, expression) || scope.IsConstantEqual(expression, 1);

        public static bool IsZero(this Scope scope, Expression expression) => scope.MatchKnownConstant(WKC.Zero, expression) || scope.IsConstantEqual(expression, 0);

        public static bool IsE(this Scope scope, Expression expression) => scope.MatchKnownConstant(WKC.EulersNumber, expression) || scope.IsConstantEqual(expression, Math.E);

        public static bool IsTrue(this Scope scope, Expression expression) => scope.TryConvert(expression, false, (bool b) => b);

        public static bool IsFalse(this Scope scope, Expression expression) => scope.TryConvert(expression, false, (bool b) => !b);
    }
}
