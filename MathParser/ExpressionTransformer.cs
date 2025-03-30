// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Numerics;
    using System.Reflection;

    /// <summary>
    /// Indicates the associativity of an operator.
    /// </summary>
    public enum Associativity
    {
        /// <summary>
        /// The operator is not associative.
        /// </summary>
        None = 0,

        /// <summary>
        /// The operator is left-associative.
        /// </summary>
        Left,

        /// <summary>
        /// The operator is right-associative.
        /// </summary>
        Right,
    }

    /// <summary>
    /// An expression visitor that formats a mathematical expression as it goes.
    /// </summary>
    /// <typeparam name="T">The type of nodes in the mathematical expression.</typeparam>
    /// <remarks>
    /// The <see cref="ExpressionExtensions"/> class contains default implementations.
    /// </remarks>
    public abstract class ExpressionTransformer<T> : ExpressionVisitor
    {
        private static readonly MethodList<ExpressionType> KnownMethods = new()
        {
            { (Complex l, double r) => Complex.Add(l, r), ExpressionType.Add },
            { (double l, Complex r) => Complex.Add(l, r), ExpressionType.Add },
            { (Complex l, Complex r) => Complex.Add(l, r), ExpressionType.Add },
            { (Complex l, double r) => Complex.Subtract(l, r), ExpressionType.Subtract },
            { (double l, Complex r) => Complex.Subtract(l, r), ExpressionType.Subtract },
            { (Complex l, Complex r) => Complex.Subtract(l, r), ExpressionType.Subtract },
            { (Complex l, double r) => Complex.Multiply(l, r), ExpressionType.Multiply },
            { (double l, Complex r) => Complex.Multiply(l, r), ExpressionType.Multiply },
            { (Complex l, Complex r) => Complex.Multiply(l, r), ExpressionType.Multiply },
            { (Complex l, double r) => Complex.Divide(l, r), ExpressionType.Divide },
            { (double l, Complex r) => Complex.Divide(l, r), ExpressionType.Divide },
            { (Complex l, Complex r) => Complex.Divide(l, r), ExpressionType.Divide },
            { (Complex l, Complex r) => Complex.Pow(l, r), ExpressionType.Power },
            { (Complex l, double r) => Complex.Pow(l, r), ExpressionType.Power },
            { Math.Pow, ExpressionType.Power },
            { Complex.Exp, ExpressionType.Power },
            { Math.Exp, ExpressionType.Power },
            { Complex.Sqrt, ExpressionType.Power },
            { Math.Sqrt, ExpressionType.Power },
            { Complex.Negate, ExpressionType.Negate },
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionTransformer{T}"/> class.
        /// </summary>
        protected ExpressionTransformer()
        {
        }

        /// <summary>
        /// Gets the result of the most recent visit operation.
        /// </summary>
        public T Result { get; private set; }

        /// <summary>
        /// Gets the associativity of the operator given its precedence.
        /// </summary>
        /// <param name="precedence">The precedence obtained by calling <see cref="GetPrecedence(ExpressionType)"/>.</param>
        /// <returns>The operator's associativity.</returns>
        protected static Associativity GetAssociativity(int precedence)
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

        /// <summary>
        /// Gets a value indicating the precedence of the specified operator type.
        /// </summary>
        /// <param name="effectiveType">The operator type.</param>
        /// <returns>A value indicating the precedence of the specified operator type.</returns>
        protected static int GetPrecedence(ExpressionType effectiveType)
        {
            switch (effectiveType)
            {
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                    return -1;

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

        /// <summary>
        /// Gets a value indicating the whether or not the two specified operator types are fully associative.
        /// </summary>
        /// <param name="left">The left operator type.</param>
        /// <param name="right">The right operator type.</param>
        /// <returns>A value indicating the whether or not the two specified operator types are fully associative.</returns>
        protected static bool IsFullyAssociative(ExpressionType left, ExpressionType right)
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

        /// <summary>
        /// Constructs a bracketed expression.
        /// </summary>
        /// <param name="expression">The expression that will be bracketed.</param>
        /// <returns>The bracketed expression.</returns>
        protected virtual T AddBrackets(T expression) => this.AddBrackets("(", expression, ")");

        /// <summary>
        /// Constructs a bracketed expression.
        /// </summary>
        /// <param name="left">The left bracket character.</param>
        /// <param name="expression">The expression that will be bracketed.</param>
        /// <param name="right">The right bracket character.</param>
        /// <returns>The bracketed expression.</returns>
        protected abstract T AddBrackets(string left, T expression, string right);

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
        /// Constructs a conditional expression.
        /// </summary>
        /// <param name="conditions">The conditional expressions.</param>
        /// <param name="alternative">The alternative expression.</param>
        /// <returns>The conditional expression.</returns>
        protected abstract T CreateConditional((T condition, T consequent)[] conditions, T alternative);

        /// <summary>
        /// Constructs a function expression.
        /// </summary>
        /// <param name="name">The name of the function.</param>
        /// <param name="arguments">The arguments to the function.</param>
        /// <returns>The function expression.</returns>
        protected abstract T CreateFunction(string name, params T[] arguments);

        /// <summary>
        /// Constructs a radical expression.
        /// </summary>
        /// <param name="expression">The expression under the radical.</param>
        /// <returns>The radical expression.</returns>
        protected abstract T CreateRadical(T expression);

        /// <summary>
        /// Constructs an equality or inequlaity expression.
        /// </summary>
        /// <param name="left">The left side of the equality.</param>
        /// <param name="op">The equality operator.</param>
        /// <param name="right">The right side of the equality.</param>
        /// <returns>The equality expression.</returns>
        protected abstract T CreateEquality(T left, ExpressionType op, T right);

        /// <summary>
        /// Constructs a lambda expression.
        /// </summary>
        /// <param name="name">The name of the function.</param>
        /// <param name="parameters">The parameters to the function.</param>
        /// <param name="body">The the expression defining the function.</param>
        /// <returns>The lambda expression.</returns>
        protected abstract T CreateLambda(string name, T[] parameters, T body);

        /// <summary>
        /// Constructs an expression representing a real number.
        /// </summary>
        /// <param name="real">The real value.</param>
        /// <returns>The real number as an expression.</returns>
        protected abstract T FormatReal(double real);

        /// <summary>
        /// Constructs an expression representing a complex number.
        /// </summary>
        /// <param name="real">The real part.</param>
        /// <param name="imaginary">The imaginary part.</param>
        /// <returns>The complex number as an expression.</returns>
        protected abstract T FormatComplex(double real, double imaginary);

        /// <summary>
        /// Constructs an expression representing a variable.
        /// </summary>
        /// <param name="name">The name of the variable.</param>
        /// <returns>The variable as an expression.</returns>
        protected abstract T FormatVariable(string name);

        /// <summary>
        /// Gets the effective type of the real number's notation when converted using <see cref="FormatReal(double)"/>.
        /// </summary>
        /// <param name="real">The real value.</param>
        /// <returns>The effective expression type.</returns>
        protected abstract ExpressionType GetEffectiveTypeReal(double real);

        /// <summary>
        /// Gets the effective type of the complex number's notation when converted using <see cref="FormatComplex(double, double)"/>.
        /// </summary>
        /// <param name="real">The real part.</param>
        /// <param name="imaginary">The imaginary part.</param>
        /// <returns>The effective expression type.</returns>
        protected abstract ExpressionType GetEffectiveTypeComplex(double real, double imaginary);

        /// <summary>
        /// Determines whether or not the inner expression should be surrounded with brackets.
        /// </summary>
        /// <param name="outerEffectiveType">The effective node type of the outer expression.</param>
        /// <param name="outer">The outer expression.</param>
        /// <param name="innerEffectiveType">The effective node type of the inner expression.</param>
        /// <param name="inner">The inner expression.</param>
        /// <returns><c>true</c>, if brackets should be used; <c>false</c>, otherwise.</returns>
        protected virtual bool NeedsLeftBrackets(ExpressionType outerEffectiveType, Expression outer, ExpressionType innerEffectiveType, Expression inner)
        {
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
        /// <param name="outer">The outer expression.</param>
        /// <param name="innerEffectiveType">The effective node type of the inner expression.</param>
        /// <param name="inner">The inner expression.</param>
        /// <returns><c>true</c>, if brackets should be used; <c>false</c>, otherwise.</returns>
        protected virtual bool NeedsRightBrackets(ExpressionType outerEffectiveType, Expression outer, ExpressionType innerEffectiveType, Expression inner)
        {
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
            ArgumentNullException.ThrowIfNull(node);

            this.Visit(node.Left);
            var left = this.Result;
            this.Visit(node.Right);
            var right = this.Result;

            var effectiveType = this.GetEffectiveNodeType(node);
            var leftEffectiveType = this.GetEffectiveNodeType(node.Left);
            var rightEffectiveType = this.GetEffectiveNodeType(node.Right);

            if (this.NeedsLeftBrackets(effectiveType, node, leftEffectiveType, node.Left))
            {
                left = this.AddBrackets(left);
            }

            if (this.NeedsRightBrackets(effectiveType, node, rightEffectiveType, node.Right))
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

                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                    this.Result = this.CreateEquality(left, effectiveType, right);
                    break;
            }

            return node;
        }

        /// <inheritdoc />
        protected override Expression VisitConditional(ConditionalExpression node)
        {
            var conditions = new List<(T condition, T consequent)>();

            var next = (Expression)node;
            while (next is ConditionalExpression inner)
            {
                this.Visit(inner.Test);
                var condition = this.Result;
                this.Visit(inner.IfTrue);
                var consequent = this.Result;
                conditions.Add((condition, consequent));

                next = inner.IfFalse;
            }

            this.Visit(next);
            var alternative = this.Result;

            this.Result = this.CreateConditional(conditions.ToArray(), alternative);

            return node;
        }

        /// <inheritdoc />
        protected override Expression VisitConstant(ConstantExpression node)
        {
            ArgumentNullException.ThrowIfNull(node);

            if (node.Value is double || node.Value is float || node.Value is int || node.Value is uint || node.Value is long || node.Value is ulong || node.Value is short || node.Value is ushort)
            {
                this.Result = this.FormatComplex(Convert.ToDouble(node.Value, CultureInfo.InvariantCulture), 0);
            }
            else if (node.Value is Complex value)
            {
                this.Result = this.FormatComplex(value.Real, value.Imaginary);
            }

            return node;
        }

        /// <inheritdoc />
        protected override Expression VisitLambda<TFunc>(Expression<TFunc> node)
        {
            this.Visit(node.Body);
            var body = this.Result;

            var parameters = new T[node.Parameters.Count];
            for (var i = 0; i < node.Parameters.Count; i++)
            {
                this.Visit(node.Parameters[i]);
                parameters[i] = this.Result;
            }

            this.Result = this.CreateLambda(node.Name, parameters, body);
            return node;
        }

        /// <inheritdoc />
        protected override Expression VisitMember(MemberExpression node)
        {
            ArgumentNullException.ThrowIfNull(node);

            if (node.Member.DeclaringType == typeof(Complex))
            {
                switch (node.Member.Name)
                {
                    case nameof(Complex.ImaginaryOne):
                        this.Result = this.FormatComplex(0, 1);
                        return node;

                    case nameof(Complex.Zero):
                        this.Result = this.FormatComplex(0, 0);
                        return node;

                    case nameof(Complex.One):
                        this.Result = this.FormatComplex(1, 0);
                        return node;

                    case nameof(Complex.Real):
                    case nameof(Complex.Imaginary):
                        this.Visit(node.Expression);
                        this.Result = this.CreateFunction(node.Member.Name.Substring(0, 2), this.Result);
                        return node;
                }
            }

            throw new NotSupportedException($"The member '{node.Member.DeclaringType.FullName}.{node.Member.Name}' is not supported for expression transformation.");
        }

        /// <inheritdoc />
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            ArgumentNullException.ThrowIfNull(node);

            var effectiveType = this.GetEffectiveNodeType(node);
            if (node.Arguments.Count == 2 && effectiveType != ExpressionType.Call)
            {
                var leftArg = node.Arguments[0];
                var rightArg = node.Arguments[1];

                this.Visit(leftArg);
                var left = this.Result;
                this.Visit(rightArg);
                var right = this.Result;

                var leftEffectiveType = this.GetEffectiveNodeType(leftArg);
                var rightEffectiveType = this.GetEffectiveNodeType(rightArg);

                if (this.NeedsLeftBrackets(effectiveType, node, leftEffectiveType, leftArg))
                {
                    left = this.AddBrackets(left);
                }

                if (this.NeedsRightBrackets(effectiveType, node, rightEffectiveType, rightArg))
                {
                    right = this.AddBrackets(right);
                }

                switch (effectiveType)
                {
                    case ExpressionType.Add:
                        this.Result = this.CreateAdd(left, right);
                        return node;

                    case ExpressionType.Subtract:
                        this.Result = this.CreateSubtract(left, right);
                        return node;

                    case ExpressionType.Multiply:
                        this.Result = this.CreateMultiply(left, right);
                        return node;

                    case ExpressionType.Divide:
                        this.Result = this.CreateDivide(left, right);
                        return node;

                    case ExpressionType.Power:
                        this.Result = this.CreatePower(left, right);
                        return node;

                    case ExpressionType.Equal:
                    case ExpressionType.NotEqual:
                    case ExpressionType.GreaterThan:
                    case ExpressionType.GreaterThanOrEqual:
                    case ExpressionType.LessThan:
                    case ExpressionType.LessThanOrEqual:
                        this.Result = this.CreateEquality(left, effectiveType, right);
                        return node;
                }
            }
            else if (node.Method.IsStatic &&
                (node.Method.DeclaringType == typeof(Math) || node.Method.DeclaringType == typeof(Complex)))
            {
                var arguments = new T[node.Arguments.Count];
                for (var i = 0; i < node.Arguments.Count; i++)
                {
                    this.Visit(node.Arguments[i]);
                    arguments[i] = this.Result;
                }

                if (node.Method.Name == nameof(Math.Sqrt) && arguments.Length == 1)
                {
                    var inner = arguments[0];
                    var power = node.Arguments[0];
                    var powerEffectiveType = this.GetEffectiveNodeType(power);

                    if (this.NeedsLeftBrackets(ExpressionType.Power, node, powerEffectiveType, power))
                    {
                        inner = this.AddBrackets(inner);
                    }

                    this.Result = this.CreateRadical(inner);
                }
                else if (node.Method.Name == nameof(Math.Exp) && arguments.Length == 1)
                {
                    var @base = this.FormatReal(double.E);
                    var baseEffectiveType = this.GetEffectiveTypeReal(double.E);

                    if (this.NeedsLeftBrackets(ExpressionType.Power, node, baseEffectiveType, null))
                    {
                        @base = this.AddBrackets(@base);
                    }

                    var inner = arguments[0];
                    var power = node.Arguments[0];
                    var powerEffectiveType = this.GetEffectiveNodeType(power);

                    if (this.NeedsRightBrackets(ExpressionType.Power, node, powerEffectiveType, power))
                    {
                        inner = this.AddBrackets(inner);
                    }

                    this.Result = this.CreatePower(@base, inner);
                }
                else if (node.Method.Name == nameof(Math.Ceiling) && arguments.Length == 1)
                {
                    this.Result = this.AddBrackets("⌈", arguments[0], "⌉");
                }
                else if (node.Method.Name == nameof(Math.Floor) && arguments.Length == 1)
                {
                    this.Result = this.AddBrackets("⌊", arguments[0], "⌋");
                }
                else if (node.Method.Name == nameof(Math.Abs) && arguments.Length == 1)
                {
                    this.Result = this.AddBrackets("|", arguments[0], "|");
                }
                else
                {
                    this.Result = this.CreateFunction(node.Method.Name, arguments);
                }

                return node;
            }
            else if (node.Arguments.Count == 1 && effectiveType == ExpressionType.Negate)
            {
                var operand = node.Arguments[0];

                this.Visit(operand);
                var inner = this.Result;

                var operandEffectiveType = this.GetEffectiveNodeType(operand);

                if (this.NeedsRightBrackets(ExpressionType.Negate, node, operandEffectiveType, operand))
                {
                    inner = this.AddBrackets(inner);
                }

                this.Result = this.CreateNegate(inner);
                return node;
            }

            throw new NotSupportedException($"The method '{node.Method.DeclaringType.FullName}.{node.Method.Name}' is not supported for expression transformation.");
        }

        /// <inheritdoc />
        protected override Expression VisitNew(NewExpression node)
        {
            ArgumentNullException.ThrowIfNull(node);

            if (node.Type == typeof(Complex) && node.Arguments.Count == 2 && node.Arguments[0].NodeType == ExpressionType.Constant && node.Arguments[1].NodeType == ExpressionType.Constant)
            {
                var real = (double)((ConstantExpression)node.Arguments[0]).Value;
                var imaginary = (double)((ConstantExpression)node.Arguments[1]).Value;
                this.Result = this.FormatComplex(real, imaginary);
                return node;
            }

            throw new NotSupportedException($"The constructor '{node.Constructor.DeclaringType.FullName}' is not supported for expression transformation.");
        }

        /// <inheritdoc />
        protected override Expression VisitParameter(ParameterExpression node)
        {
            ArgumentNullException.ThrowIfNull(node);

            this.Result = this.FormatVariable(node.Name);
            return node;
        }

        /// <inheritdoc />
        protected override Expression VisitUnary(UnaryExpression node)
        {
            ArgumentNullException.ThrowIfNull(node);

            if (node.NodeType == ExpressionType.Negate ||
                node.NodeType == ExpressionType.NegateChecked)
            {
                this.Visit(node.Operand);
                var inner = this.Result;

                var operandEffectiveType = this.GetEffectiveNodeType(node.Operand);

                if (this.NeedsRightBrackets(ExpressionType.Negate, node, operandEffectiveType, node.Operand))
                {
                    inner = this.AddBrackets(inner);
                }

                this.Result = this.CreateNegate(inner);
                return node;
            }
            else if (node.NodeType == ExpressionType.Convert)
            {
                this.Visit(node.Operand);
                return node;
            }

            throw new NotSupportedException($"The unary operator '{node.NodeType}' is not supported for expression transformation.");
        }

        /// <summary>
        /// Evaluates the specified expression to determine if it can be more approriately represented as a different operator type.
        /// </summary>
        /// <param name="expression">The expression to evaluate.</param>
        /// <returns>The best operator type to represent the expression.</returns>
        protected ExpressionType GetEffectiveNodeType(Expression expression)
        {
            ArgumentNullException.ThrowIfNull(expression);

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
                var constantExpression = (ConstantExpression)expression;
                if (expression.Type == typeof(double) || expression.Type == typeof(float) || expression.Type == typeof(int) || expression.Type == typeof(uint) || expression.Type == typeof(long) || expression.Type == typeof(ulong) || expression.Type == typeof(short) || expression.Type == typeof(ushort))
                {
                    return this.GetEffectiveTypeComplex(Convert.ToDouble(constantExpression.Value, CultureInfo.InvariantCulture), 0);
                }
                else if (expression.Type == typeof(Complex))
                {
                    var value = (Complex)constantExpression.Value;
                    return this.GetEffectiveTypeComplex(value.Real, value.Imaginary);
                }
            }
            else if (actualType == ExpressionType.Call)
            {
                var node = (MethodCallExpression)expression;

                if (KnownMethods.TryGetValue(node.Method, out var knownType))
                {
                    return knownType;
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
