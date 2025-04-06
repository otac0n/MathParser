﻿namespace MathParser
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Linq.Expressions;
    using WKC = WellKnownConstants;
    using WKF = WellKnownFunctions;

    public sealed class SimplifyVisitor(Scope scope) : ExpressionVisitor
    {
        /// <summary>
        /// Gets the scope in which the transformations are performed.
        /// </summary>
        public Scope Scope { get; } = scope;

        /// <inheritdoc/>
        [return: NotNullIfNotNull(nameof(node))]
        public override Expression? Visit(Expression? node)
        {
            if (this.Scope.TryBind(node, out var knownFunction, out var functionArguments))
            {
                return this.VisitKnownFunction(knownFunction, node, functionArguments);
            }
            else if (this.Scope.TryBind(node, out var knownConstant))
            {
                return this.VisitKnownConstant(knownConstant, node);
            }

            return base.Visit(node);
        }

        protected Expression VisitKnownConstant(KnownConstant knownConstant, Expression node)
        {
            if (knownConstant == WKC.Zero)
            {
                return Expression.Constant(0.0);
            }
            else if (knownConstant == WKC.One)
            {
                return Expression.Constant(1.0);
            }
            else if (knownConstant == WKC.NegativeOne)
            {
                return Expression.Constant(-1.0);
            }
            else if (knownConstant == WKC.PositiveInfinity)
            {
                return Expression.Constant(double.PositiveInfinity);
            }
            else if (knownConstant == WKC.NegativeInfinity)
            {
                return Expression.Constant(double.NegativeInfinity);
            }

            return this.Scope.BindConstant(knownConstant);
        }

        protected Expression VisitKnownFunction(KnownFunction function, Expression node, IList<Expression> arguments)
        {
            var converted = new Expression[arguments.Count];
            for (var i = 0; i < arguments.Count; i++)
            {
                converted[i] = this.Visit(arguments[i]);
            }

            if (WKF.ExpressionTypeLookup.TryGetValue(function, out var effectiveType))
            {
                if (arguments.Count == 1)
                {
                    var operand = converted[0];
                    switch (effectiveType)
                    {
                        case ExpressionType.Negate:
                            return this.SimplifyNegate(operand);

                        case ExpressionType.Not:
                            return this.SimplifyNot(operand);
                    }
                }
                else if (arguments.Count == 2)
                {
                    var left = converted[0];
                    var right = converted[1];
                    switch (effectiveType)
                    {
                        case ExpressionType.Add:
                            return this.SimplifyAdd(left, right);

                        case ExpressionType.Subtract:
                            return this.SimplifySubtract(left, right);

                        case ExpressionType.Multiply:
                            return this.SimplifyMultiply(left, right);

                        case ExpressionType.Divide:
                            return this.SimplifyDivide(left, right);

                        case ExpressionType.Power:
                            return this.SimplifyPower(left, right);

                        case ExpressionType.And:
                            return this.SimplifyAnd(left, right);

                        case ExpressionType.Or:
                            return this.SimplifyOr(left, right);

                        case ExpressionType.GreaterThan:
                        case ExpressionType.GreaterThanOrEqual:
                        case ExpressionType.LessThan:
                        case ExpressionType.LessThanOrEqual:
                            return this.SimplifyCompare(left, effectiveType, right);
                    }
                }
            }

            if (function == WKF.Exponential.Pow && arguments.Count == 2)
            {
                var @base = converted[0];
                var exponent = converted[1];
                return this.SimplifyPower(@base, exponent);
            }
            else if (function == WKF.Exponential.Ln && arguments.Count == 1)
            {
                if (this.Scope.IsE(converted[0]))
                {
                    return this.Scope.One();
                }
            }
            else if (function == WKF.Exponential.Exp && arguments.Count == 1)
            {
                return this.Visit(this.Scope.Pow(this.Scope.E(), converted[0]));
            }

            return this.Scope.Bind(function, converted);
        }

        /// <inheritdoc/>
        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            var simpleBody = this.Visit(node.Body);
            return node.Update(this.Scope.ConvertIfLower(simpleBody, to: node.ReturnType), node.Parameters);
        }

        /// <inheritdoc/>
        protected override Expression VisitConditional(ConditionalExpression node)
        {
            var simpleTest = this.Visit(node.Test);
            var simpleTrue = this.Visit(node.IfTrue);
            var simpleFalse = this.Visit(node.IfFalse);

            return this.SimplifyConditional(simpleTest, simpleTrue, simpleFalse);
        }

        /// <inheritdoc/>
        protected override Expression VisitBinary(BinaryExpression node)
        {
            var simpleLeft = this.Visit(node.Left);
            var simpleRight = this.Visit(node.Right);

            return node.Update(simpleLeft, node.Conversion, simpleRight);
        }

        /// <inheritdoc/>
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var simpleObject = node.Object == null ? null : this.Visit(node.Object);
            var simpleArguments = node.Arguments.Select(a => this.Visit(a)).ToList();

            var parameters = node.Method.GetParameters();
            return node.Update(simpleObject, [.. simpleArguments.Select((a, i) => this.Scope.ConvertIfLower(a, to: parameters[i].ParameterType))]);
        }

        /// <inheritdoc/>
        protected override Expression VisitUnary(UnaryExpression node)
        {
            var simpleOperand = this.Visit(node.Operand);
            switch (node.NodeType)
            {
                case ExpressionType.Convert:
                    // TODO: Any integer type to any T : INumber<T>
                    if (node.Type == typeof(int) && node.Operand.Type == typeof(double))
                    {
                        break; // Equivalent to Math.Truncate, and must be preserved.
                    }

                    return simpleOperand; // TODO: This could create a lot of churn, so it may be useful to communicate if the convert is still necessary before removing.
            }

            return node.Update(simpleOperand);
        }

        private Expression SimplifyNot(Expression expression)
        {
            // Convert "not not a" into "a"
            if (this.Scope.MatchNot(expression, out var innerOperand))
            {
                return innerOperand;
            }

            if (this.Scope.IsConstantValue(expression, out var constant))
            {
                // Convert "not true" into "false"
                if (constant.Value is bool constantValue)
                {
                    return Expression.Constant(!constantValue);
                }
            }

            return this.Scope.Not(expression);
        }

        private Expression SimplifyAnd(Expression left, Expression right)
        {
            // Convert "false and a" into "false"
            if (this.Scope.IsFalse(left))
            {
                return left;
            }

            // Convert "true and a" into "a"
            if (this.Scope.IsTrue(left))
            {
                return right;
            }

            // Convert "a and false" into "false"
            if (this.Scope.IsFalse(right))
            {
                return right;
            }

            // Convert "a and true" into "a"
            if (this.Scope.IsTrue(right))
            {
                return left;
            }

            // Convert "a and a" into "a"
            if (new MatchVisitor(left).PatternMatch(right).Success)
            {
                return left;
            }

            return this.Scope.And(left, right);
        }

        private Expression SimplifyOr(Expression left, Expression right)
        {
            // Convert "false or a" into "a"
            if (this.Scope.IsFalse(left))
            {
                return right;
            }

            // Convert "true or a" into "true"
            if (this.Scope.IsTrue(left))
            {
                return left;
            }

            // Convert "a or false" into "a"
            if (this.Scope.IsFalse(right))
            {
                return left;
            }

            // Convert "a or true" into "true"
            if (this.Scope.IsTrue(right))
            {
                return right;
            }

            // Convert "a or a" into "a"
            if (new MatchVisitor(left).PatternMatch(right).Success)
            {
                return left;
            }

            return this.Scope.Or(left, right);
        }

        private Expression SimplifyNegate(Expression operand)
        {
            if (this.Scope.MatchConstraint(operand, out var condition, out var consequent))
            {
                return this.Visit(this.Scope.Constraint(condition, this.Scope.Negate(consequent)));
            }

            // Convert "--a" into "a"
            if (this.Scope.MatchNegate(operand, out var innerOperand))
            {
                return innerOperand;
            }

            // Convert "-(a + b)" into "-a - b"
            if (this.Scope.MatchAdd(operand, out var addLeft, out var addRight))
            {
                return this.Visit(this.Scope.Subtract(this.Scope.Negate(addLeft), addRight));
            }

            // Convert "-(a - b)" into "b - a"
            if (this.Scope.MatchSubtract(operand, out var subtractLeft, out var subtractRight))
            {
                return this.Visit(this.Scope.Subtract(subtractRight, subtractLeft));
            }

            // Convert "-(a / b)" into "-a / b"
            if (this.Scope.MatchDivide(operand, out var divideLeft, out var divideRight))
            {
                return this.Visit(this.Scope.Divide(this.Scope.Negate(divideLeft), divideRight));
            }

            return this.Scope.Negate(operand);
        }

        private Expression SimplifyAdd(Expression augend, Expression addend)
        {
            if (this.Scope.MatchConstraint(augend, out var leftCondition, out var leftConsequent))
            {
                return this.Visit(this.Scope.Constraint(leftCondition, this.Scope.Add(leftConsequent, addend)));
            }

            if (this.Scope.MatchConstraint(addend, out var rightCondition, out var rightConsequent))
            {
                return this.Visit(this.Scope.Constraint(rightCondition, this.Scope.Add(augend, rightConsequent)));
            }

            // Convert "0 + a" into "a"
            if (this.Scope.IsZero(augend))
            {
                return addend;
            }

            // Convert "a + 0" into "a"
            if (this.Scope.IsZero(addend))
            {
                return augend;
            }

            if (this.Scope.IsConstantValue(addend, out var rightConstant))
            {
                if (this.Scope.IsConstantValue(augend, out var leftConstant))
                {
                    // Convert "1 + 1" into "2"
                    // TODO: Support all types.
                    if (leftConstant.Value is double leftValue && rightConstant.Value is double rightValue)
                    {
                        // TODO: Add a configuration option to detect and prevent loss of precision.
                        return Expression.Constant(leftValue + rightValue);
                    }
                }
                else if (this.Scope.MatchAdd(augend, out var augendLeft, out var augendRight) &&
                    this.Scope.IsConstantValue(augendRight, out _))
                {
                    // Convert "a + 1 + 1" into "a + (1 + 1)"
                    return this.Visit(this.Scope.Add(augendLeft, this.Scope.Add(augendRight, addend)));
                }
            }

            // Convert "a + (b + c)" into "a + b + c"
            if (this.Scope.MatchAdd(addend, out var addendLeft, out var addendRight))
            {
                return this.Visit(this.Scope.Add(this.Scope.Add(augend, addendLeft), addendRight));
            }

            // Convert "a + 1 + b" into "a + b + 1"
            if (this.Scope.MatchAdd(augend, out var leftAddLeft, out var leftAddRight) &&
                this.SortNodes(ref leftAddRight, ref addend))
            {
                return this.Visit(this.Scope.Add(this.Scope.Add(leftAddLeft, leftAddRight), addend));
            }

            // Convert "a + (b - c)" into "a + b - c"
            if (this.Scope.MatchSubtract(addend, out var rightSubtractLeft, out var rightSubtractRight))
            {
                return this.Visit(this.Scope.Subtract(this.Scope.Add(augend, rightSubtractLeft), rightSubtractRight));
            }

            // Convert "a + -b" into "a - b"
            if (this.Scope.MatchNegate(addend, out var rightNegateOperand))
            {
                return this.Visit(this.Scope.Subtract(augend, rightNegateOperand));
            }

            // Convert "-a + b" into "b - a"
            if (this.Scope.MatchNegate(augend, out var leftNegateOperand))
            {
                return this.Visit(this.Scope.Subtract(addend, leftNegateOperand));
            }

            if (this.SortNodes(ref augend, ref addend))
            {
                // Convert "1 + a" into "a + 1"
                return this.Visit(this.Scope.Add(augend, addend));
            }

            if (this.CombineLikeAddition(addend, augend, out var combined) ||
                this.CombineLikeAddition(augend, addend, out combined))
            {
                return this.Visit(combined);
            }

            return this.Scope.Add(augend, addend);
        }

        private Expression SimplifySubtract(Expression minuend, Expression subtrahend)
        {
            if (this.Scope.MatchConstraint(minuend, out var leftCondition, out var leftConsequent))
            {
                return this.Visit(this.Scope.Constraint(leftCondition, this.Scope.Subtract(leftConsequent, subtrahend)));
            }

            if (this.Scope.MatchConstraint(subtrahend, out var rightCondition, out var rightConsequent))
            {
                return this.Visit(this.Scope.Constraint(rightCondition, this.Scope.Subtract(minuend, rightConsequent)));
            }

            // Convert "0 - a" into "-a"
            if (this.Scope.IsZero(minuend))
            {
                return Expression.Negate(subtrahend);
            }

            // Convert "a - 0" into "a"
            if (this.Scope.IsZero(subtrahend))
            {
                return minuend;
            }

            if (this.Scope.IsConstantValue(minuend, out var leftConstant))
            {
                if (this.Scope.IsConstantValue(subtrahend, out var rightConstant))
                {
                    // Convert "1 - 1" into "0"
                    // TODO: Support all types.
                    if (leftConstant.Value is double leftValue && rightConstant.Value is double rightValue)
                    {
                        // TODO: Add a configuration option to detect and prevent loss of precision.
                        return Expression.Constant(leftValue - rightValue);
                    }
                }
            }

            // Convert "a - (b + c)" into "a - b - c"
            if (this.Scope.MatchAdd(subtrahend, out var rightAddLeft, out var rightAddRight))
            {
                return this.Visit(this.Scope.Subtract(this.Scope.Subtract(minuend, rightAddLeft), rightAddRight));
            }

            // Convert "a - (b - c)" into "a - b + c"
            if (this.Scope.MatchSubtract(subtrahend, out var rightSubtractLeft, out var rightSubtractRight))
            {
                return this.Visit(this.Scope.Add(this.Scope.Subtract(minuend, rightSubtractLeft), rightSubtractRight));
            }

            // Convert "a - -b" into "a + b"
            if (this.Scope.MatchNegate(subtrahend, out var negateOperand))
            {
                return this.Visit(this.Scope.Add(minuend, negateOperand));
            }

            if (this.CombineLikeAddition(minuend, subtrahend, out var combined, negateRight: true))
            {
                return this.Visit(combined);
            }
            else if (this.CombineLikeAddition(subtrahend, minuend, out combined, negateRight: true))
            {
                return this.Visit(this.Scope.Negate(combined));
            }

            return this.Scope.Subtract(minuend, subtrahend);
        }

        private Expression SimplifyMultiply(Expression multiplicand, Expression multiplier)
        {
            if (this.Scope.MatchConstraint(multiplicand, out var leftCondition, out var leftConsequent))
            {
                return this.Visit(this.Scope.Constraint(leftCondition, this.Scope.Multiply(leftConsequent, multiplier)));
            }

            if (this.Scope.MatchConstraint(multiplier, out var rightCondition, out var rightConsequent))
            {
                return this.Visit(this.Scope.Constraint(rightCondition, this.Scope.Multiply(multiplicand, rightConsequent)));
            }

            // Convert "0 * a" into "0"
            if (this.Scope.IsZero(multiplicand))
            {
                return multiplicand;
            }

            // Convert "1 * a" into "a"
            if (this.Scope.IsOne(multiplicand))
            {
                return multiplier;
            }

            // Convert "a * 0" into "0"
            if (this.Scope.IsZero(multiplier))
            {
                return multiplier;
            }

            // Convert "a * 1" into "a"
            if (this.Scope.IsOne(multiplier))
            {
                return multiplicand;
            }

            if (this.Scope.IsConstantValue(multiplier, out var rightConstant))
            {
                if (this.Scope.IsConstantValue(multiplicand, out var leftConstant))
                {
                    // Convert "2 * 2" into "4"
                    if (leftConstant.Value is double leftValue && rightConstant.Value is double rightValue) // TODO: Support all types.
                    {
                        // TODO: Add a configuration option to detect and prevent loss of precision.
                        return Expression.Constant(leftValue * rightValue);
                    }
                }
            }

            // Convert "a * (b * c)" into "a * b * c"
            if (this.Scope.MatchMultiply(multiplier, out var rightMultiplyLeft, out var rightMultiplyRight))
            {
                return this.Visit(this.Scope.Multiply(this.Scope.Multiply(multiplicand, rightMultiplyLeft), rightMultiplyRight));
            }

            // Convert "a * b * 1" into "a * 1 * b"
            if (this.Scope.MatchMultiply(multiplicand, out var leftMultiplyLeft, out var leftMultiplyRight) &&
                this.SortNodes(ref leftMultiplyRight, ref multiplier, constantsFirst: true))
            {
                return this.Visit(this.Scope.Multiply(this.Scope.Multiply(leftMultiplyLeft, leftMultiplyRight), multiplier));
            }

            // Convert "(a / b) * c" into "a * c / b"
            if (this.Scope.MatchDivide(multiplicand, out var leftDivideLeft, out var leftDivideRight))
            {
                return this.Visit(this.Scope.Divide(this.Scope.Multiply(leftDivideLeft, multiplier), leftDivideRight));
            }

            // Convert "a * (b / c)" into "a * b / c"
            if (this.Scope.MatchDivide(multiplier, out var rightDivideLeft, out var rightDivideRight))
            {
                return this.Visit(this.Scope.Divide(this.Scope.Multiply(multiplicand, rightDivideLeft), rightDivideRight));
            }

            // Convert "-a * b" into "-(a * b)"
            if (this.Scope.MatchNegate(multiplicand, out var leftNegateOperand))
            {
                return this.Visit(this.Scope.Negate(this.Scope.Multiply(leftNegateOperand, multiplier)));
            }

            // Convert "a * -b" into "-(a * b)"
            if (this.Scope.MatchNegate(multiplier, out var rightNegateOperand))
            {
                return this.Visit(this.Scope.Negate(this.Scope.Multiply(multiplicand, rightNegateOperand)));
            }

            // Convert "a * (b + c)" into "a * b + a * c"
            if (this.Scope.MatchAdd(multiplier, out var rightAddLeft, out var rightAddRight))
            {
                return this.Visit(this.Scope.Add(this.Scope.Multiply(multiplicand, rightAddLeft), this.Scope.Multiply(multiplicand, rightAddRight)));
            }

            // Convert "(a + b) * c" into "a * c + b * c"
            if (this.Scope.MatchAdd(multiplicand, out var leftAddLeft, out var leftAddRight))
            {
                return this.Visit(this.Scope.Add(this.Scope.Multiply(leftAddLeft, multiplier), this.Scope.Multiply(leftAddRight, multiplier)));
            }

            // Convert "a * (b - c)" into "a * b - a * c"
            if (this.Scope.MatchSubtract(multiplier, out var rightSubtractLeft, out var rightSubtractRight))
            {
                return this.Visit(this.Scope.Subtract(this.Scope.Multiply(multiplicand, rightSubtractLeft), this.Scope.Multiply(multiplicand, rightSubtractRight)));
            }

            // Convert "(a - b) * c" into "a * c - b * c"
            if (this.Scope.MatchSubtract(multiplicand, out var leftSubtractLeft, out var leftSubtractRight))
            {
                return this.Visit(this.Scope.Subtract(this.Scope.Multiply(leftSubtractLeft, multiplier), this.Scope.Multiply(leftSubtractRight, multiplier)));
            }

            if (this.SortNodes(ref multiplicand, ref multiplier, constantsFirst: true))
            {
                // Convert "a * 2" into "2 * a"
                return this.Visit(this.Scope.Multiply(multiplicand, multiplier));
            }

            if (this.CombineLikeMultiplication(multiplicand, multiplier, out var combined) ||
                this.CombineLikeMultiplication(multiplier, multiplicand, out combined))
            {
                return this.Visit(combined);
            }

            return this.Scope.Multiply(multiplicand, multiplier);
        }

        private Expression SimplifyDivide(Expression dividend, Expression divisor)
        {
            if (this.Scope.MatchConstraint(dividend, out var leftCondition, out var leftConsequent))
            {
                return this.Visit(this.Scope.Constraint(leftCondition, this.Scope.Divide(leftConsequent, divisor)));
            }

            if (this.Scope.MatchConstraint(divisor, out var rightCondition, out var rightConsequent))
            {
                return this.Visit(this.Scope.Constraint(rightCondition, this.Scope.Divide(dividend, rightConsequent)));
            }

            // Maintain "a / 0"
            if (this.Scope.IsZero(divisor))
            {
                return this.Scope.Divide(dividend, divisor);
            }

            // Convert "a / 1" to "a"
            if (this.Scope.IsOne(divisor))
            {
                return dividend;
            }

            // Convert "0 / a" into "0"
            if (this.Scope.IsZero(dividend))
            {
                return dividend;
            }

            // Convert "a / √2" into "a√2 / 2"
            if (this.Scope.MatchSqrt(divisor, out var @base) && this.Scope.IsConstantValue(@base, out var constant) && constant.Value is double value && value >= 0)
            {
                return this.Visit(this.Scope.Divide(this.Scope.Multiply(dividend, divisor), @base));
            }

            // Convert "a / a" into "a; a!=0"
            if (dividend == divisor)
            {
                return this.Visit(this.Scope.Constraint(this.Scope.NotEqual(divisor, this.Scope.Zero()), this.Scope.One()));
            }

            return this.Scope.Divide(dividend, divisor);
        }

        private Expression SimplifyPower(Expression @base, Expression exponent)
        {
            if (this.Scope.MatchConstraint(@base, out var leftCondition, out var leftConsequent))
            {
                return this.Visit(this.Scope.Constraint(leftCondition, this.Scope.Pow(leftConsequent, exponent)));
            }

            if (this.Scope.MatchConstraint(exponent, out var rightCondition, out var rightConsequent))
            {
                return this.Visit(this.Scope.Constraint(rightCondition, this.Scope.Pow(@base, rightConsequent)));
            }

            // Convert "1 ^ a" to "1"
            if (this.Scope.IsOne(@base))
            {
                return @base;
            }

            // Convert "a ^ 1" to "a"
            if (this.Scope.IsOne(exponent))
            {
                return @base;
            }

            // Convert "a ^ 0" to "1"
            if (this.Scope.IsZero(exponent))
            {
                return this.Scope.One();
            }

            if (this.Scope.IsZero(@base))
            {
                // Convert "0 ^ 2" to "0"
                if (this.Scope.IsConstantValue(exponent, out _))
                {
                    return @base;
                }
            }

            if (this.Scope.IsConstantValue(exponent, out var rightConstant))
            {
                if (rightConstant.Value is double rightValue && rightValue > 0)
                {
                    if (this.Scope.IsConstantValue(@base, out var leftConstant))
                    {
                        // Convert "2 ^ 2" into "4"
                        if (leftConstant.Value is double leftValue) // TODO: Support all types.
                        {
                            // TODO: Add a configuration option to detect and prevent loss of precision.
                            return Expression.Constant(Math.Pow(leftValue, rightValue));
                        }
                    }
                    else if (double.IsInteger(rightValue) && rightValue <= 10 &&
                        this.Scope.TryBind(@base, out var knownFunction, out _) &&
                        (knownFunction == WKF.Arithmetic.Add || knownFunction == WKF.Arithmetic.Subtract))
                    {
                        var totalPower = (int)rightValue;
                        var rightPower = totalPower / 2;
                        var leftPower = totalPower - rightPower;

                        return this.Visit(this.Scope.Multiply(this.Scope.Pow(@base, Expression.Constant((double)leftPower)), this.Scope.Pow(@base, Expression.Constant((double)rightPower))));
                    }
                }
            }

            // Convert "(a ^ b) ^ c" into "a ^ (b * c)"
            if (this.Scope.MatchPower(@base, out var leftBase, out var leftExponent))
            {
                return this.Visit(this.Scope.Pow(leftBase, this.Scope.Multiply(leftExponent, exponent)));
            }

            return this.Scope.Pow(@base, exponent);
        }

        private Expression SimplifyConditional(Expression condition, Expression consequent, Expression alternative)
        {
            if (this.Scope.IsNaN(alternative))
            {
                if (consequent.NodeType == ExpressionType.Conditional &&
                    consequent is ConditionalExpression conditionalConsequent &&
                    this.Scope.IsNaN(conditionalConsequent.IfFalse))
                {
                    return this.Visit(this.Scope.Conditional(this.Scope.And(condition, conditionalConsequent.Test), conditionalConsequent.IfTrue, conditionalConsequent.IfFalse));
                }
            }

            return this.Scope.Conditional(condition, consequent, alternative);
        }

        private Expression SimplifyCompare(Expression left, ExpressionType op, Expression right)
        {
            return this.Scope.Compare(left, op, right);
        }

        private bool SortNodes(ref Expression a, ref Expression b, bool constantsFirst = false)
        {
            if (CompareNodes(a, b, constantsFirst) < 0)
            {
                (a, b) = (b, a);
                return true;
            }

            return false;
        }

        private int CompareNodes(Expression a, Expression b, bool constantsFirst = false)
        {
            var constantA = this.Scope.IsConstantValue(a, out _);
            var constantB = this.Scope.IsConstantValue(b, out _);
            if (constantA && constantB)
            {
                return 0;
            }
            else if (constantA ^ constantB)
            {
                return (constantA ? -1 : 1) * (constantsFirst ? -1 : 1);
            }

            return 0;
        }

        private bool CombineLikeAddition(Expression left, Expression right, out Expression combined, bool negateRight = false)
        {
            this.GetCoefficientAndFactor(left, out var leftFactor, out var coefficient);

            Expression? remainder = right;
            if (this.ExtractByFactor(leftFactor, ref coefficient, ref remainder, negateRight))
            {
                var newLeft = this.Scope.Multiply(coefficient, leftFactor);
                combined = remainder == null
                    ? newLeft
                    : negateRight
                        ? this.Scope.Subtract(newLeft, remainder)
                        : this.Scope.Add(newLeft, remainder);
                return true;
            }

            combined = null;
            return false;
        }

        private bool ExtractByFactor(Expression factor, [NotNullWhen(true)] ref Expression? coefficient, ref Expression? remainder, bool negate) =>
            this.ExtractByFactor(new MatchVisitor(factor), ref coefficient, ref remainder, negate);

        private bool ExtractByFactor(MatchVisitor factor, [NotNullWhen(true)] ref Expression? coefficient, ref Expression? remainder, bool negate)
        {
            if (this.Scope.MatchAdd(remainder, out var addLeft, out var addRight))
            {
                bool changed;
                changed = this.ExtractByFactor(factor, ref coefficient, ref addLeft, negate);
                changed |= this.ExtractByFactor(factor, ref coefficient, ref addRight, negate);
                if (changed)
                {
                    remainder =
                        addLeft == null ? addRight :
                        addRight == null ? addLeft : this.Scope.Add(addLeft, addRight);
                }

                return changed;
            }
            else if (this.Scope.MatchSubtract(remainder, out var subLeft, out var subRight))
            {
                bool changed;
                changed = this.ExtractByFactor(factor, ref coefficient, ref subLeft, negate);
                changed |= this.ExtractByFactor(factor, ref coefficient, ref subRight, !negate);
                if (changed)
                {
                    remainder =
                        subLeft == null ? this.Scope.Negate(subRight ?? this.Scope.One()) :
                        subRight == null ? subLeft : this.Scope.Subtract(subLeft, subRight);
                }

                return changed;
            }

            if (remainder != null)
            {
                this.GetCoefficientAndFactor(remainder, out var rFactor, out var rCoefficient);
                if (factor.PatternMatch(rFactor).Success)
                {
                    coefficient = negate
                        ? this.Scope.Subtract(coefficient ?? this.Scope.One(), rCoefficient ?? this.Scope.One())
                        : this.Scope.Add(coefficient ?? this.Scope.One(), rCoefficient ?? this.Scope.One());
                    remainder = null;
                    return true;
                }
            }

            return false;
        }

        private void GetCoefficientAndFactor(Expression expr, out Expression factor, out Expression? coefficient, bool negate = false)
        {
            // Correct, but causes infinte loop with distribution of negate through addition.
            if (false && this.Scope.MatchNegate(expr, out var operand))
            {
                this.GetCoefficientAndFactor(operand, out factor, out coefficient, !negate);
                return;
            }

            if (this.Scope.MatchMultiply(expr, out var left, out var right))
            {
                if (this.Scope.IsConstantValue(left, out _))
                {
                    coefficient = negate ? this.Scope.Negate(left) : left;
                    factor = right;
                    return;
                }
                else if (this.Scope.IsConstantValue(right, out _))
                {
                    coefficient = negate ? this.Scope.Negate(right) : right;
                    factor = left;
                    return;
                }
            }

            factor = expr;
            coefficient = negate ? this.Scope.NegativeOne() : null; // null -> one.
            return;
        }

        private bool CombineLikeMultiplication(Expression left, Expression right, out Expression combined)
        {
            this.GetBaseAndPower(left, out var leftBase, out var exponent);

            Expression? remainder = right;
            if (this.ExtractByBase(leftBase, ref exponent, ref remainder))
            {
                var newLeft = this.Scope.Pow(leftBase, exponent);
                combined = remainder == null ? newLeft : this.Scope.Multiply(newLeft, remainder);
                return true;
            }

            combined = null;
            return false;
        }

        private void GetBaseAndPower(Expression expr, out Expression @base, out Expression? exponent)
        {
            if (this.Scope.MatchPower(expr, out @base, out exponent))
            {
                return;
            }

            @base = expr;
            exponent = null; // null -> one.
            return;
        }

        private bool ExtractByBase(Expression @base, [NotNullWhen(true)] ref Expression? exponent, ref Expression? remainder) =>
            this.ExtractByBase(new MatchVisitor(@base), ref exponent, ref remainder);

        private bool ExtractByBase(MatchVisitor @base, [NotNullWhen(true)] ref Expression? exponent, ref Expression? remainder)
        {
            if (this.Scope.MatchMultiply(remainder, out var left, out var right))
            {
                bool changed;
                changed = this.ExtractByBase(@base, ref exponent, ref left);
                changed |= this.ExtractByBase(@base, ref exponent, ref right);
                if (changed)
                {
                    remainder =
                        left == null ? right :
                        right == null ? left : this.Scope.Multiply(left, right);
                }

                return changed;
            }

            if (remainder != null)
            {
                this.GetBaseAndPower(remainder, out var rBase, out var rExponent);
                if (@base.PatternMatch(rBase).Success)
                {
                    exponent = this.Scope.Add(exponent ?? this.Scope.One(), rExponent ?? this.Scope.One());
                    remainder = null;
                    return true;
                }
            }

            return false;
        }
    }
}
