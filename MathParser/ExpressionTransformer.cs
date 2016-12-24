﻿// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser
{
    using System;
    using System.Globalization;
    using System.Linq.Expressions;
    using System.Numerics;

    /// <summary>
    /// An expression visitor that formats a mathematical expression as it goes.
    /// </summary>
    /// <typeparam name="T">The type of nodes in the mathematical expression.</typeparam>
    /// <remarks>
    /// The <see cref="ExpressionTransformers"/> class contains default implementations.
    /// </remarks>
    public abstract class ExpressionTransformer<T> : ExpressionVisitor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionTransformer{T}"/> class.
        /// </summary>
        protected ExpressionTransformer()
        {
        }

        private enum Associativity
        {
            None = 0,
            Left,
            Right,
        }

        /// <summary>
        /// Gets the result of the most recent visit operation.
        /// </summary>
        public T Result { get; private set; }

        /// <summary>
        /// Constructs a bracketed expression.
        /// </summary>
        /// <param name="expression">The expression that will be bracketed.</param>
        /// <returns>The bracketed expression.</returns>
        protected abstract T AddBrackets(T expression);

        /// <summary>
        /// Constructs an additive expression.
        /// </summary>
        /// <param name="augend">The left addend expression.</param>
        /// <param name="addend">The right addend expression.</param>
        /// <returns>The additive expression.</returns>
        protected abstract T CreateAdd(T augend, T addend);

        /// <summary>
        /// Constructs a division expression.
        /// </summary>
        /// <param name="dividend">The dividend expression.</param>
        /// <param name="divisor">The divisor expression.</param>
        /// <returns>The division expression.</returns>
        protected abstract T CreateDivide(T dividend, T divisor);

        /// <summary>
        /// Constructs a multiplicative expression.
        /// </summary>
        /// <param name="multiplier">The multiplier expression.</param>
        /// <param name="multiplicand">The multiplicand expression.</param>
        /// <returns>The multiplicative expression.</returns>
        protected abstract T CreateMultiply(T multiplier, T multiplicand);

        /// <summary>
        /// Constructs a negation expression.
        /// </summary>
        /// <param name="expression">The expression that will be negated.</param>
        /// <returns>The negation expression.</returns>
        protected abstract T CreateNegate(T expression);

        /// <summary>
        /// Constructs an exponentiation expression.
        /// </summary>
        /// <param name="base">The base expression.</param>
        /// <param name="exponent">The exponent expression.</param>
        /// <returns>The exponentiation expression.</returns>
        protected abstract T CreatePower(T @base, T exponent);

        /// <summary>
        /// Constructs a subtraction expression.
        /// </summary>
        /// <param name="minuend">The minuend expression.</param>
        /// <param name="subtrahend">The subtrahend expression.</param>
        /// <returns>The subtraction expression.</returns>
        protected abstract T CreateSubtract(T minuend, T subtrahend);

        /// <summary>
        /// Constructs an expression representing a complex number.
        /// </summary>
        /// <param name="real">The real part.</param>
        /// <param name="imaginary">The imaginary part.</param>
        /// <returns>The complex number as an expression.</returns>
        protected abstract T FormatComplex(double real, double imaginary);

        /// <summary>
        /// Constructs an expression representing a real number.
        /// </summary>
        /// <param name="value">The real number.</param>
        /// <returns>The real number as an expression.</returns>
        protected abstract T FormatReal(double value);

        /// <summary>
        /// Constructs an expression representing a variable.
        /// </summary>
        /// <param name="name">The name of the variable.</param>
        /// <returns>The variable as an expression.</returns>
        protected abstract T FormatVariable(string name);

        /// <summary>
        /// Gets the effective type of the complex number's notation when converted using <see cref="FormatComplex(double, double)"/>.
        /// </summary>
        /// <param name="real">The real part.</param>
        /// <param name="imaginary">The imaginary part.</param>
        /// <returns>The effective expression type.</returns>
        protected abstract ExpressionType GetEffectiveTypeComplex(double real, double imaginary);

        /// <summary>
        /// Gets the effective type of the real number's notation when converted using <see cref="FormatReal(double)"/>.
        /// </summary>
        /// <param name="value">The real number.</param>
        /// <returns>The effective expression type.</returns>
        protected abstract ExpressionType GetEffectiveTypeReal(double value);

        /// <summary>
        /// Determines whether or not the inner expression should be surrounded with brackets.
        /// </summary>
        /// <param name="outerEffectiveType">The effective node type of the outer expression.</param>
        /// <param name="inner">The inner expression.</param>
        /// <returns><c>true</c>, if brackets should be used; <c>false</c>, otherwise.</returns>
        protected virtual bool NeedsLeftBrackets(ExpressionType outerEffectiveType, Expression inner)
        {
            var innerEffectiveType = this.GetEffectiveNodeType(inner);
            var outerPrecedence = GetPrecedence(outerEffectiveType);
            var innerPrecedence = GetPrecedence(innerEffectiveType);
            var innerAssociativity = GetAssociativity(innerPrecedence);
            var fullyAssociative = IsFullyAssociative(innerEffectiveType, outerEffectiveType);

            if (outerPrecedence < innerPrecedence || fullyAssociative)
            {
                return false;
            }

            if (outerPrecedence == innerPrecedence)
            {
                if (innerAssociativity == Associativity.Left)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Determines whether or not the inner expression should be surrounded with brackets.
        /// </summary>
        /// <param name="outerEffectiveType">The effective node type of the outer expression.</param>
        /// <param name="inner">The inner expression.</param>
        /// <returns><c>true</c>, if brackets should be used; <c>false</c>, otherwise.</returns>
        protected virtual bool NeedsRightBrackets(ExpressionType outerEffectiveType, Expression inner)
        {
            var innerEffectiveType = this.GetEffectiveNodeType(inner);
            var outerPrecedence = GetPrecedence(outerEffectiveType);
            var innerPrecedence = GetPrecedence(innerEffectiveType);
            var innerAssociativity = GetAssociativity(innerPrecedence);
            var fullyAssociative = IsFullyAssociative(outerEffectiveType, innerEffectiveType);

            if (outerPrecedence < innerPrecedence || fullyAssociative)
            {
                return false;
            }

            if (outerPrecedence == innerPrecedence)
            {
                if (innerAssociativity == Associativity.Right)
                {
                    return false;
                }
            }

            return true;
        }

        /// <inheritdoc />
        protected override Expression VisitBinary(BinaryExpression node)
        {
            this.Visit(node.Left);
            var left = this.Result;
            this.Visit(node.Right);
            var right = this.Result;

            var effectiveType = this.GetEffectiveNodeType(node);

            if (this.NeedsLeftBrackets(effectiveType, node.Left))
            {
                left = this.AddBrackets(left);
            }

            if (this.NeedsRightBrackets(effectiveType, node.Right))
            {
                right = this.AddBrackets(right);
            }

            switch (effectiveType)
            {
                case ExpressionType.Add:
                    this.Result = this.CreateAdd(left, right);
                    break;

                case ExpressionType.Subtract:
                    this.Result = this.CreateSubtract(left, right);
                    break;

                case ExpressionType.Multiply:
                    this.Result = this.CreateMultiply(left, right);
                    break;

                case ExpressionType.Divide:
                    this.Result = this.CreateDivide(left, right);
                    break;

                case ExpressionType.Power:
                    this.Result = this.CreatePower(left, right);
                    break;
            }

            return node;
        }

        /// <inheritdoc />
        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (node.Value is double || node.Value is float || node.Value is int || node.Value is uint || node.Value is long || node.Value is ulong || node.Value is short || node.Value is ushort)
            {
                this.Result = this.FormatReal(Convert.ToDouble(node.Value, CultureInfo.InvariantCulture));
            }
            else if (node.Value is Complex)
            {
                var value = (Complex)node.Value;
                this.Result = this.FormatComplex(value.Real, value.Imaginary);
            }

            return node;
        }

        /// <inheritdoc />
        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Member.DeclaringType == typeof(Complex))
            {
                if (node.Member.Name == nameof(Complex.ImaginaryOne))
                {
                    this.Result = this.FormatComplex(0, 1);
                }
                else if (node.Member.Name == nameof(Complex.Zero))
                {
                    this.Result = this.FormatComplex(0, 0);
                }
                else if (node.Member.Name == nameof(Complex.One))
                {
                    this.Result = this.FormatComplex(1, 0);
                }

                return node;
            }
            else
            {
                return base.VisitMember(node);
            }
        }

        /// <inheritdoc />
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var effectiveType = this.GetEffectiveNodeType(node);
            if (effectiveType == ExpressionType.Add ||
                effectiveType == ExpressionType.Subtract ||
                effectiveType == ExpressionType.Multiply ||
                effectiveType == ExpressionType.Divide ||
                effectiveType == ExpressionType.Power)
            {
                var leftArg = node.Arguments[0];
                var rightArg = node.Arguments[1];

                this.Visit(leftArg);
                var left = this.Result;
                this.Visit(rightArg);
                var right = this.Result;

                if (this.NeedsLeftBrackets(effectiveType, leftArg))
                {
                    left = this.AddBrackets(left);
                }

                if (this.NeedsRightBrackets(effectiveType, rightArg))
                {
                    right = this.AddBrackets(right);
                }

                switch (effectiveType)
                {
                    case ExpressionType.Add:
                        this.Result = this.CreateAdd(left, right);
                        break;

                    case ExpressionType.Subtract:
                        this.Result = this.CreateSubtract(left, right);
                        break;

                    case ExpressionType.Multiply:
                        this.Result = this.CreateMultiply(left, right);
                        break;

                    case ExpressionType.Divide:
                        this.Result = this.CreateDivide(left, right);
                        break;

                    case ExpressionType.Power:
                        this.Result = this.CreatePower(left, right);
                        break;
                }

                return node;
            }
            else if (effectiveType == ExpressionType.Negate)
            {
                var operand = node.Arguments[0];

                this.Visit(operand);
                var inner = this.Result;

                if (this.NeedsRightBrackets(ExpressionType.Negate, operand))
                {
                    inner = this.AddBrackets(inner);
                }

                this.Result = this.CreateNegate(inner);
                return node;
            }
            else
            {
                return base.VisitMethodCall(node);
            }
        }

        /// <inheritdoc />
        protected override Expression VisitNew(NewExpression node)
        {
            if (node.Type == typeof(Complex) && node.Arguments.Count == 2 && node.Arguments[0].NodeType == ExpressionType.Constant && node.Arguments[1].NodeType == ExpressionType.Constant)
            {
                var real = (double)((ConstantExpression)node.Arguments[0]).Value;
                var imaginary = (double)((ConstantExpression)node.Arguments[1]).Value;
                this.Result = this.FormatComplex(real, imaginary);
                return node;
            }
            else
            {
                return base.VisitNew(node);
            }
        }

        /// <inheritdoc />
        protected override Expression VisitParameter(ParameterExpression node)
        {
            base.VisitParameter(node);
            this.Result = this.FormatVariable(node.Name);
            return node;
        }

        /// <inheritdoc />
        protected override Expression VisitUnary(UnaryExpression node)
        {
            if (node.NodeType == ExpressionType.Negate ||
                node.NodeType == ExpressionType.NegateChecked)
            {
                base.VisitUnary(node);
                var inner = this.Result;

                if (this.NeedsRightBrackets(ExpressionType.Negate, node.Operand))
                {
                    inner = this.AddBrackets(inner);
                }

                this.Result = this.CreateNegate(inner);
                return node;
            }
            else
            {
                return base.VisitUnary(node);
            }
        }

        private static Associativity GetAssociativity(int precedence)
        {
            switch (precedence)
            {
                case 0:
                case 1:
                case 2:
                    return Associativity.Left;

                case 3:
                    return Associativity.Right;

                default:
                    return Associativity.None;
            }
        }

        private static int GetPrecedence(ExpressionType effectiveType)
        {
            switch (effectiveType)
            {
                case ExpressionType.Add:
                case ExpressionType.Subtract:
                    return 0;

                case ExpressionType.Multiply:
                case ExpressionType.Divide:
                case ExpressionType.Modulo:
                    return 1;

                case ExpressionType.Negate:
                    return 2;

                case ExpressionType.Power:
                    return 3;

                default:
                    return int.MaxValue;
            }
        }

        private static bool IsFullyAssociative(ExpressionType left, ExpressionType right)
        {
            if (left == ExpressionType.Add && right == ExpressionType.Add)
            {
                return true;
            }

            if (left == ExpressionType.Multiply && right == ExpressionType.Multiply)
            {
                return true;
            }

            if (left == ExpressionType.Negate && right == ExpressionType.Negate)
            {
                return true;
            }

            return false;
        }

        private ExpressionType GetEffectiveNodeType(Expression expression)
        {
            var actualType = expression.NodeType;

            if (actualType == ExpressionType.AddChecked)
            {
                return ExpressionType.Add;
            }
            else if (actualType == ExpressionType.SubtractChecked)
            {
                return ExpressionType.Subtract;
            }
            else if (actualType == ExpressionType.MultiplyChecked)
            {
                return ExpressionType.Multiply;
            }
            else if (actualType == ExpressionType.NegateChecked)
            {
                return ExpressionType.Negate;
            }
            else if (actualType == ExpressionType.New && expression.Type == typeof(Complex))
            {
                var node = (NewExpression)expression;
                if (node.Arguments.Count == 2 && node.Arguments[0].NodeType == ExpressionType.Constant && node.Arguments[1].NodeType == ExpressionType.Constant)
                {
                    var real = (double)((ConstantExpression)node.Arguments[0]).Value;
                    var imaginary = (double)((ConstantExpression)node.Arguments[1]).Value;
                    return this.GetEffectiveTypeComplex(real, imaginary);
                }
            }
            else if (actualType == ExpressionType.Constant)
            {
                if (expression.Type == typeof(double) || expression.Type == typeof(float) || expression.Type == typeof(int) || expression.Type == typeof(uint) || expression.Type == typeof(long) || expression.Type == typeof(ulong) || expression.Type == typeof(short) || expression.Type == typeof(ushort))
                {
                    return this.GetEffectiveTypeReal(Convert.ToDouble(((ConstantExpression)expression).Value));
                }
                else if (expression.Type == typeof(Complex))
                {
                    var value = (Complex)((ConstantExpression)expression).Value;
                    return this.GetEffectiveTypeComplex(value.Real, value.Imaginary);
                }
            }
            else if (actualType == ExpressionType.Call)
            {
                var node = (MethodCallExpression)expression;
                if (node.Method.IsStatic)
                {
                    if (node.Method.DeclaringType == typeof(Complex))
                    {
                        if (node.Method.Name == nameof(Complex.Pow) && node.Arguments.Count == 2)
                        {
                            return ExpressionType.Power;
                        }
                        else if (node.Method.Name == nameof(Complex.Add) && node.Arguments.Count == 2)
                        {
                            return ExpressionType.Add;
                        }
                        else if (node.Method.Name == nameof(Complex.Subtract) && node.Arguments.Count == 2)
                        {
                            return ExpressionType.Subtract;
                        }
                        else if (node.Method.Name == nameof(Complex.Multiply) && node.Arguments.Count == 2)
                        {
                            return ExpressionType.Multiply;
                        }
                        else if (node.Method.Name == nameof(Complex.Divide) && node.Arguments.Count == 2)
                        {
                            return ExpressionType.Divide;
                        }
                        else if (node.Method.Name == nameof(Complex.Negate) && node.Arguments.Count == 1)
                        {
                            return ExpressionType.Negate;
                        }
                    }
                    else if (node.Method.DeclaringType == typeof(Math))
                    {
                        if (node.Method.Name == nameof(Math.Pow) && node.Arguments.Count == 2)
                        {
                            return ExpressionType.Power;
                        }
                    }
                }
            }
            else if (
                actualType == ExpressionType.Convert ||
                actualType == ExpressionType.ConvertChecked)
            {
                return this.GetEffectiveNodeType(((UnaryExpression)expression).Operand);
            }

            return actualType;
        }
    }
}
