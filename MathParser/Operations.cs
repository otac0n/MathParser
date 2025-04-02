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

        public static Expression Abs(Expression expression)
        {
            return Expression.Call(typeof(Math).GetMethod(nameof(Math.Abs), [expression.Type]) ?? typeof(Complex).GetMethod(nameof(Complex.Abs), [expression.Type]), expression);
        }

        public static Expression Conditional(Expression condition, Expression consequent, Expression alternative)
        {
            return Expression.Condition(condition, ConvertIfLower(consequent, to: alternative), ConvertIfLower(alternative, to: consequent));
        }

        public static Expression Ceiling(Expression expression)
        {
            return Expression.Call(typeof(Math).GetMethod(nameof(Math.Ceiling), [expression.Type]), expression);
        }

        public static Expression Floor(Expression expression)
        {
            return Expression.Call(typeof(Math).GetMethod(nameof(Math.Floor), [expression.Type]), expression);
        }

        public static Expression Negate(Expression operand)
        {
            if (IsZero(operand))
            {
                return operand;
            }

            return Expression.Negate(operand);
        }

        public static Expression Add(Expression augend, Expression addend)
        {
            return Expression.Add(ConvertIfLower(augend, to: addend), ConvertIfLower(addend, to: augend));
        }

        public static Expression Subtract(Expression minuend, Expression subtrahend)
        {
            return Expression.Subtract(ConvertIfLower(minuend, to: subtrahend), ConvertIfLower(subtrahend, to: minuend));
        }

        public static Expression Multiply(Expression multiplicand, Expression multiplier)
        {
            return Expression.Multiply(ConvertIfLower(multiplicand, to: multiplier), ConvertIfLower(multiplier, to: multiplicand));
        }

        public static Expression Divide(Expression dividend, Expression divisor)
        {
            return Expression.Divide(ConvertIfLower(dividend, to: divisor), ConvertIfLower(divisor, to: dividend));
        }

        public static Expression Pow(Expression @base, Expression exponent)
        {
            if (@base.Type == typeof(double) && exponent.Type == typeof(double))
            {
                return Expression.Power(@base, exponent);
            }

            @base = ConvertIfLower(@base, to: typeof(Complex));
            return Expression.Call(typeof(Complex).GetMethod(nameof(Complex.Pow), [@base.Type, exponent.Type]), @base, exponent);
        }

        public static Expression Exp(Expression exponent)
        {
            return Expression.Call(typeof(Math).GetMethod(nameof(Math.Exp), [exponent.Type]) ?? typeof(Complex).GetMethod(nameof(Complex.Exp), [exponent.Type]), exponent);
        }

        public static Expression Sqrt(Expression @base)
        {
            if (TryConvert(@base, false, (double value) => value > 0))
            {
                return MathMethod(nameof(Math.Sqrt), @base);
            }

            @base = ConvertIfLower(@base, to: typeof(Complex));
            return Expression.Call(typeof(Complex).GetMethod(nameof(Complex.Sqrt), [@base.Type]), @base);
        }

        public static Expression Log(Expression expression)
        {
            return Expression.Call(typeof(Math).GetMethod(nameof(Math.Log), [expression.Type]) ?? typeof(Complex).GetMethod(nameof(Complex.Log), [expression.Type]), expression);
        }

        public static Expression Compare(Expression left, ExpressionType op, Expression right)
        {
            return op is ExpressionType.Equal or ExpressionType.NotEqual
                ? Expression.MakeBinary(op, ConvertIfLower(left, to: right), ConvertIfLower(right, to: left))
                : Expression.MakeBinary(op, LowerToReal(left), LowerToReal(right));
        }

        public static Expression Function(string name, IList<Expression> arguments)
        {
            if (arguments.Count == 1)
            {
                if (name.Equals("Re", StringComparison.CurrentCultureIgnoreCase))
                {
                    return Expression.Property(ConvertIfLower(arguments[0], to: typeof(Complex)), typeof(Complex).GetProperty(nameof(Complex.Real), BindingFlags.Public | BindingFlags.Instance));
                }

                if (name.Equals("Im", StringComparison.CurrentCultureIgnoreCase))
                {
                    return Expression.Property(ConvertIfLower(arguments[0], to: typeof(Complex)), typeof(Complex).GetProperty(nameof(Complex.Imaginary), BindingFlags.Public | BindingFlags.Instance));
                }
            }

            Expression[] mappedArguments;
            var found = FindFunction(typeof(Complex), name, arguments.Select(a => a.Type).ToArray());
            if (found == null)
            {
                found = FindFunction(typeof(Complex), name, arguments.Select(_ => typeof(Complex)).ToArray());
                if (found == null)
                {
                    throw new MissingMethodException(typeof(Complex).FullName, name + "(" + string.Join(", ", arguments.Select(a => a.Type.FullName)) + ")");
                }
                else
                {
                    mappedArguments = arguments.Select(a => ConvertIfLower(a, to: typeof(Complex))).ToArray();
                }
            }
            else
            {
                mappedArguments = arguments.ToArray();
            }

            return Expression.Call(found, mappedArguments);
        }

        public static MethodInfo FindFunction(Type type, string name, Type[] argTypes)
        {
            return (from m in type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    where m.DeclaringType == type
                    where m.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)
                    let parameters = m.GetParameters()
                    where parameters.Length == argTypes.Length
                    where Enumerable.Range(0, argTypes.Length).All(i => parameters[i].ParameterType.IsAssignableFrom(argTypes[i]))
                    select m).FirstOrDefault();
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
                methodCall.Object is null && methodCall.Method.Name == nameof(Math.Pow) && methodCall.Arguments.Count == 2 &&
                (methodCall.Method.DeclaringType == typeof(Math) || methodCall.Method.DeclaringType == typeof(Complex)))
            {
                @base = methodCall.Arguments[0];
                exponent = methodCall.Arguments[1];
                return true;
            }

            @base = null;
            exponent = null;
            return false;
        }

        public static bool IsSqrt(Expression expression, [NotNullWhen(true)] out Expression? @base)
        {
            if (expression.NodeType == ExpressionType.Call && expression is MethodCallExpression methodCall &&
                methodCall.Object is null && methodCall.Method.Name == nameof(Math.Sqrt) && methodCall.Arguments.Count == 1 &&
                (methodCall.Method.DeclaringType == typeof(Math) || methodCall.Method.DeclaringType == typeof(Complex)))
            {
                @base = methodCall.Arguments[0];
                return true;
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

        private static MethodCallExpression MathMethod(string name, params Expression[] arguments)
        {
            var types = Array.ConvertAll(arguments, a => a.Type);
            var method = typeof(Math).GetMethod(name, BindingFlags.Public | BindingFlags.Static, types);
            return Expression.Call(null, method, arguments);
        }
    }
}
