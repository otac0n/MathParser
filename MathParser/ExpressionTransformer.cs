// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq.Expressions;
    using System.Numerics;
    using WKF = WellKnownFunctions;

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
        private static readonly Dictionary<KnownFunction, ExpressionType> MethodEquivalence = new()
        {
            [WKF.Arithmetic.Negate] = ExpressionType.Negate,
            [WKF.Arithmetic.Add] = ExpressionType.Add,
            [WKF.Arithmetic.Subtract] = ExpressionType.Subtract,
            [WKF.Arithmetic.Multiply] = ExpressionType.Multiply,
            [WKF.Arithmetic.Divide] = ExpressionType.Divide,
            [WKF.Exponential.Pow] = ExpressionType.Power,
            [WKF.Exponential.Exp] = ExpressionType.Power,
            [WKF.Exponential.Sqrt] = ExpressionType.Power,
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
        protected static Associativity GetAssociativity(Precedence precedence)
        {
            switch (precedence)
            {
                case Precedence.Disjunction:
                case Precedence.Conjunction:
                case Precedence.Additive:
                case Precedence.Multiplicative:
                    return Associativity.Left;

                case Precedence.Exponential:
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
        protected static Precedence GetPrecedence(ExpressionType effectiveType)
        {
            switch (effectiveType)
            {
                case ExpressionType.Conditional:
                    return Precedence.Conditional;

                case ExpressionType.Or:
                    return Precedence.Disjunction;

                case ExpressionType.And:
                    return Precedence.Conjunction;

                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                    return Precedence.Comparison;

                case ExpressionType.Add:
                case ExpressionType.Subtract:
                    return Precedence.Additive;

                case ExpressionType.Multiply:
                case ExpressionType.Divide:
                case ExpressionType.Modulo:
                    return Precedence.Multiplicative;

                case ExpressionType.Negate:
                case ExpressionType.Not:
                    return Precedence.Unary;

                case ExpressionType.Power:
                    return Precedence.Exponential;

                default:
                    return Precedence.Unknown;
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
            if (left == ExpressionType.And && right == ExpressionType.And)
            {
                return true;
            }

            if (left == ExpressionType.Or && right == ExpressionType.Or)
            {
                return true;
            }

            if (left == ExpressionType.Not && right == ExpressionType.Not)
            {
                return true;
            }

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
        /// Constructs a logical not expression.
        /// </summary>
        /// <param name="expression">The expression that will be negated.</param>
        /// <returns>The negation expression.</returns>
        protected abstract T CreateNot(T expression);

        /// <summary>
        /// Constructs a logical and expression.
        /// </summary>
        /// <param name="left">The left conjunct.</param>
        /// <param name="right">The right conjunct.</param>
        /// <returns>The conjunction expression.</returns>
        protected abstract T CreateAnd(T left, T right);

        /// <summary>
        /// Constructs a logical or expression.
        /// </summary>
        /// <param name="left">The left disjunct.</param>
        /// <param name="right">The right disjunct.</param>
        /// <returns>The disjunction expression.</returns>
        protected abstract T CreateOr(T left, T right);

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
        /// <param name="multiplicand">The multiplicand expression.</param>
        /// <param name="multiplier">The multiplier expression.</param>
        /// <returns>The multiplicative expression.</returns>
        protected abstract T CreateMultiply(T multiplicand, T multiplier);

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
        protected abstract T CreateConditional((T condition, T consequent)[] conditions, T? alternative);

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
        /// Constructs an expression representing a boolean value.
        /// </summary>
        /// <param name="boolean">The truth value.</param>
        /// <returns>The boolean value as an expression.</returns>
        protected abstract T FormatBoolean(bool boolean);

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
        /// Called recursively through <see cref="GetLeftExposedType(ExpressionType, Expression)"/>.
        /// </summary>
        /// <param name="node">The node to inspect recursively.</param>
        /// <returns>The left exposed type of the node.</returns>
        protected ExpressionType GetLeftExposedType(Expression node) => this.GetLeftExposedType(this.GetEffectiveNodeType(node), node);

        /// <summary>
        /// Allows for the proper handling of operations that are exposed on the left.
        /// </summary>
        /// <param name="effectiveType">The effective type of the node.</param>
        /// <param name="node">The node to inspect.</param>
        /// <returns>The effective type of the leftmost exposed operator.</returns>
        protected virtual ExpressionType GetLeftExposedType(ExpressionType effectiveType, Expression node) => effectiveType;

        /// <summary>
        /// Called recursively through <see cref="GetRightExposedType(ExpressionType, Expression)"/>.
        /// </summary>
        /// <param name="node">The node to inspect recursively.</param>
        /// <returns>The right exposed type of the node.</returns>
        protected ExpressionType GetRightExposedType(Expression node) => this.GetRightExposedType(this.GetEffectiveNodeType(node), node);

        /// <summary>
        /// Allows for the proper handling of operations that are exposed on the right.
        /// </summary>
        /// <param name="effectiveType">The effective type of the node.</param>
        /// <param name="node">The node to inspect.</param>
        /// <returns>The effective type of the rightmost exposed operator.</returns>
        /// <remarks>Conditionals may be rendered with half-open syntax like `{x, x!=0`.</remarks>
        protected virtual ExpressionType GetRightExposedType(ExpressionType effectiveType, Expression node) => effectiveType;

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
            var exposed = this.GetRightExposedType(innerEffectiveType, inner); // Assumed to be a lower precedence than innerEffectiveType if changed.
            var exposedPrecedence = GetPrecedence(exposed);

            if (exposedPrecedence != innerPrecedence)
            {
                return true;
            }

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
            var exposed = this.GetLeftExposedType(innerEffectiveType, inner); // Assumed to be a lower precedence than innerEffectiveType if changed.
            var exposedPrecedence = GetPrecedence(exposed);

            if (exposedPrecedence != innerPrecedence)
            {
                return true;
            }

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
                case ExpressionType.And:
                    this.Result = this.CreateAnd(left, right);
                    break;

                case ExpressionType.Or:
                    this.Result = this.CreateOr(left, right);
                    break;

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

            T? alternative = default;
            if (!Operations.IsNaN(next))
            {
                this.Visit(next);
                alternative = this.Result;
            }
            else
            {
                if (conditions.Count == 1)
                {
                    var left = conditions[0].condition;
                    var right = conditions[0].consequent;

                    var effectiveType = this.GetEffectiveNodeType(node);
                    var leftEffectiveType = this.GetEffectiveNodeType(node.Test);
                    var rightEffectiveType = this.GetEffectiveNodeType(node.IfTrue);

                    if (this.NeedsLeftBrackets(effectiveType, node, leftEffectiveType, node.Test))
                    {
                        left = this.AddBrackets(left);
                    }

                    if (this.NeedsLeftBrackets(effectiveType, node, rightEffectiveType, node.IfTrue))
                    {
                        right = this.AddBrackets(right);
                    }

                    conditions[0] = (left, right);
                }
            }

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
            else if (node.Value is bool boolean)
            {
                this.Result = this.FormatBoolean(boolean);
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
                }
            }

            if (Scope.TryBind(node, out var function, out var arguments))
            {
                return this.VisitKnownFunction(function, node, arguments);
            }

            throw new NotSupportedException($"The member '{node.Member.DeclaringType.FullName}.{node.Member.Name}' is not supported for expression transformation.");
        }

        protected virtual Expression VisitKnownFunction(KnownFunction function, Expression node, IList<Expression> arguments)
        {
            var converted = new T[arguments.Count];
            for (var i = 0; i < arguments.Count; i++)
            {
                this.Visit(arguments[i]);
                converted[i] = this.Result;
            }

            if (function == WKF.Piecewise.Abs && arguments.Count == 1)
            {
                this.Result = this.AddBrackets("|", converted[0], "|");
                return node;
            }
            else if (function == WKF.Piecewise.Ceiling && arguments.Count == 1)
            {
                this.Result = this.AddBrackets("⌈", converted[0], "⌉");
                return node;
            }
            else if (function == WKF.Piecewise.Floor && arguments.Count == 1)
            {
                this.Result = this.AddBrackets("⌊", converted[0], "⌋");
                return node;
            }
            else if (function == WKF.Exponential.Sqrt && arguments.Count == 1)
            {
                var inner = converted[0];
                var @base = arguments[0];
                var baseEffectiveType = this.GetEffectiveNodeType(@base);

                if (this.NeedsLeftBrackets(ExpressionType.Power, node, baseEffectiveType, @base))
                {
                    inner = this.AddBrackets(inner);
                }

                this.Result = this.CreateRadical(inner);
                return node;
            }
            else if (function == WKF.Exponential.Exp && arguments.Count == 1)
            {
                var @base = this.FormatReal(double.E);
                var baseEffectiveType = this.GetEffectiveTypeReal(double.E);

                if (this.NeedsLeftBrackets(ExpressionType.Power, node, baseEffectiveType, null))
                {
                    @base = this.AddBrackets(@base);
                }

                var inner = converted[0];
                var power = arguments[0];
                var powerEffectiveType = this.GetEffectiveNodeType(power);

                if (this.NeedsRightBrackets(ExpressionType.Power, node, powerEffectiveType, power))
                {
                    inner = this.AddBrackets(inner);
                }

                this.Result = this.CreatePower(@base, inner);
                return node;
            }

            this.Result = this.CreateFunction(Scope.BindName(function), converted);
            return node;
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
            else if (Scope.TryBind(node, out var knownFunction, out var functionArguments))
            {
                return this.VisitKnownFunction(knownFunction, node, functionArguments);
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
            else if (node.NodeType == ExpressionType.Not)
            {
                this.Visit(node.Operand);
                var inner = this.Result;

                var operandEffectiveType = this.GetEffectiveNodeType(node.Operand);

                if (this.NeedsRightBrackets(ExpressionType.Not, node, operandEffectiveType, node.Operand))
                {
                    inner = this.AddBrackets(inner);
                }

                this.Result = this.CreateNot(inner);
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
            else if (actualType == ExpressionType.AndAlso)
            {
                return ExpressionType.And;
            }
            else if (actualType == ExpressionType.OrElse)
            {
                return ExpressionType.Or;
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
                if (Scope.TryBind(expression, out var knownMethod, out _))
                {
                    if (MethodEquivalence.TryGetValue(knownMethod, out var knownType))
                    {
                        return knownType;
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
