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
        private readonly MatchVisitor matchVisitor = new MatchVisitor();

        /// <summary>
        /// Gets the scope in which the transformations are performed.
        /// </summary>
        //public Scope Scope => scope;

        /// <inheritdoc/>
        [return: NotNullIfNotNull(nameof(node))]
        public override Expression? Visit(Expression? node)
        {
            if (scope.TryBindFunction(node, out var knownFunction, out var functionArguments))
            {
                return this.VisitKnownFunction(knownFunction, node, functionArguments);
            }

            return base.Visit(node);
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

            if (function == WKF.Exponential.Ln && arguments.Count == 1)
            {
                if (scope.IsE(converted[0]))
                {
                    return scope.One();
                }
            }
            else if (function == WKF.Exponential.Exp && arguments.Count == 1)
            {
                return this.SimplifyPower(scope.E(), converted[0]);
            }
            else if (function == WKF.Arithmetic.Reciprocal && arguments.Count == 1)
            {
                return this.SimplifyDivide(scope.One(), converted[0]);
            }

            return scope.BindFunction(function, converted);
        }

        /// <inheritdoc/>
        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            var simpleBody = this.Visit(node.Body);
            return node.Update(scope.ConvertIfLower(simpleBody, to: node.ReturnType), node.Parameters);
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
            return node.Update(simpleObject, [.. simpleArguments.Select((a, i) => scope.ConvertIfLower(a, to: parameters[i].ParameterType))]);
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
            if (scope.MatchNot(expression, out var innerOperand))
            {
                return innerOperand;
            }

            if (scope.IsConstantValue(expression, out var constant))
            {
                // Convert "not true" into "false"
                if (constant.Value is bool constantValue)
                {
                    return Expression.Constant(!constantValue);
                }
            }

            return scope.Not(expression);
        }

        private Expression SimplifyAnd(Expression left, Expression right)
        {
            // Convert "false and a" into "false"
            if (scope.IsFalse(left))
            {
                return left;
            }

            // Convert "true and a" into "a"
            if (scope.IsTrue(left))
            {
                return right;
            }

            // Convert "a and false" into "false"
            if (scope.IsFalse(right))
            {
                return right;
            }

            // Convert "a and true" into "a"
            if (scope.IsTrue(right))
            {
                return left;
            }

            // Convert "a and a" into "a"
            if (this.matchVisitor.PatternMatch(left, right).Success)
            {
                return left;
            }

            return scope.And(left, right);
        }

        private Expression SimplifyOr(Expression left, Expression right)
        {
            // Convert "false or a" into "a"
            if (scope.IsFalse(left))
            {
                return right;
            }

            // Convert "true or a" into "true"
            if (scope.IsTrue(left))
            {
                return left;
            }

            // Convert "a or false" into "a"
            if (scope.IsFalse(right))
            {
                return left;
            }

            // Convert "a or true" into "true"
            if (scope.IsTrue(right))
            {
                return right;
            }

            // Convert "a or a" into "a"
            if (this.matchVisitor.PatternMatch(left, right).Success)
            {
                return left;
            }

            return scope.Or(left, right);
        }

        private Expression SimplifyNegate(Expression operand)
        {
            if (scope.MatchConstraint(operand, out var condition, out var consequent))
            {
                return this.SimplifyConditional(condition, this.SimplifyNegate(consequent), scope.NaN());
            }

            // Convert "--a" into "a"
            if (scope.MatchNegate(operand, out var innerOperand))
            {
                return innerOperand;
            }

            // Convert "-(a + b)" into "-a - b"
            if (scope.MatchAdd(operand, out var addLeft, out var addRight))
            {
                return this.SimplifySubtract(this.SimplifyNegate(addLeft), addRight);
            }

            // Convert "-(a - b)" into "b - a"
            if (scope.MatchSubtract(operand, out var subtractLeft, out var subtractRight))
            {
                return this.SimplifySubtract(subtractRight, subtractLeft);
            }

            return scope.Negate(operand);
        }

        private Expression SimplifyAdd(Expression augend, Expression addend)
        {
            if (scope.MatchConstraint(augend, out var leftCondition, out var leftConsequent))
            {
                return this.SimplifyConditional(leftCondition, this.SimplifyAdd(leftConsequent, addend), scope.NaN());
            }

            if (scope.MatchConstraint(addend, out var rightCondition, out var rightConsequent))
            {
                return this.SimplifyConditional(rightCondition, this.SimplifyAdd(augend, rightConsequent), scope.NaN());
            }

            // Convert "0 + a" into "a"
            if (scope.IsZero(augend))
            {
                return addend;
            }

            // Convert "a + 0" into "a"
            if (scope.IsZero(addend))
            {
                return augend;
            }

            if (scope.IsConstant(addend))
            {
                if (scope.IsConstant(augend))
                {
                    // Convert "1 + 1" into "2"
                    return this.EvaluateAsConstant(scope.Add(addend, augend));
                }
                else if (scope.MatchAdd(augend, out var augendLeft, out var augendRight)
                    && scope.IsConstant(augendRight))
                {
                    // Convert "a + 1 + 1" into "a + (1 + 1)"
                    return this.SimplifyAdd(augendLeft, this.SimplifyAdd(augendRight, addend));
                }
            }

            // Convert "a + (b + c)" into "a + b + c"
            if (scope.MatchAdd(addend, out var addendLeft, out var addendRight))
            {
                return this.SimplifyAdd(this.SimplifyAdd(augend, addendLeft), addendRight);
            }

            // Convert "a + 1 + b" into "a + b + 1"
            if (scope.MatchAdd(augend, out var leftAddLeft, out var leftAddRight) &&
                this.SortNodes(ref leftAddRight, ref addend))
            {
                return this.SimplifyAdd(this.SimplifyAdd(leftAddLeft, leftAddRight), addend);
            }

            // Convert "a + (b - c)" into "a + b - c"
            if (scope.MatchSubtract(addend, out var rightSubtractLeft, out var rightSubtractRight))
            {
                return this.SimplifySubtract(this.SimplifyAdd(augend, rightSubtractLeft), rightSubtractRight);
            }

            // Convert "a + -b" into "a - b"
            if (scope.MatchNegate(addend, out var rightNegateOperand))
            {
                return this.SimplifySubtract(augend, rightNegateOperand);
            }

            // Convert "-a + b" into "b - a"
            if (scope.MatchNegate(augend, out var leftNegateOperand))
            {
                return this.SimplifySubtract(addend, leftNegateOperand);
            }

            if (this.CombineLikeAddition(addend, augend, out var combined) ||
                this.CombineLikeAddition(addend, augend, out combined, relativeRight: true))
            {
                return combined;
            }

            this.SortNodes(ref augend, ref addend);
            return scope.Add(augend, addend);
        }

        private Expression SimplifySubtract(Expression minuend, Expression subtrahend)
        {
            if (scope.MatchConstraint(minuend, out var leftCondition, out var leftConsequent))
            {
                return this.SimplifyConditional(leftCondition, this.SimplifySubtract(leftConsequent, subtrahend), scope.NaN());
            }

            if (scope.MatchConstraint(subtrahend, out var rightCondition, out var rightConsequent))
            {
                return this.SimplifyConditional(rightCondition, this.SimplifySubtract(minuend, rightConsequent), scope.NaN());
            }

            // Convert "0 - a" into "-a"
            if (scope.IsZero(minuend))
            {
                return Expression.Negate(subtrahend);
            }

            // Convert "a - 0" into "a"
            if (scope.IsZero(subtrahend))
            {
                return minuend;
            }

            if (scope.IsConstant(minuend))
            {
                if (scope.IsConstant(subtrahend))
                {
                    // Convert "1 - 1" into "0"
                    return this.EvaluateAsConstant(scope.Subtract(minuend, subtrahend));
                }
            }

            // Convert "a - (b + c)" into "a - b - c"
            if (scope.MatchAdd(subtrahend, out var rightAddLeft, out var rightAddRight))
            {
                return this.SimplifySubtract(this.SimplifySubtract(minuend, rightAddLeft), rightAddRight);
            }

            // Convert "a - (b - c)" into "a - b + c"
            if (scope.MatchSubtract(subtrahend, out var rightSubtractLeft, out var rightSubtractRight))
            {
                return this.SimplifyAdd(this.SimplifySubtract(minuend, rightSubtractLeft), rightSubtractRight);
            }

            // Convert "a - -b" into "a + b"
            if (scope.MatchNegate(subtrahend, out var negateOperand))
            {
                return this.SimplifyAdd(minuend, negateOperand);
            }

            if (this.CombineLikeAddition(minuend, subtrahend, out var combined, negateRight: true) ||
                this.CombineLikeAddition(minuend, subtrahend, out combined, relativeRight: true, negateRight: true))
            {
                return combined;
            }

            return scope.Subtract(minuend, subtrahend);
        }

        private Expression SimplifyMultiply(Expression multiplicand, Expression multiplier)
        {
            if (scope.MatchConstraint(multiplicand, out var leftCondition, out var leftConsequent))
            {
                return this.SimplifyConditional(leftCondition, this.SimplifyMultiply(leftConsequent, multiplier), scope.NaN());
            }

            if (scope.MatchConstraint(multiplier, out var rightCondition, out var rightConsequent))
            {
                return this.SimplifyConditional(rightCondition, this.SimplifyMultiply(multiplicand, rightConsequent), scope.NaN());
            }

            // Convert "0 * a" into "0"
            if (scope.IsZero(multiplicand))
            {
                return multiplicand;
            }

            // Convert "1 * a" into "a"
            if (scope.IsOne(multiplicand))
            {
                return multiplier;
            }

            // Convert "a * 0" into "0"
            if (scope.IsZero(multiplier))
            {
                return multiplier;
            }

            // Convert "a * 1" into "a"
            if (scope.IsOne(multiplier))
            {
                return multiplicand;
            }

            if (scope.IsConstant(multiplier))
            {
                if (scope.IsConstant(multiplicand))
                {
                    // Convert "2 * 2" into "4"
                    return this.EvaluateAsConstant(scope.Multiply(multiplicand, multiplier));
                }
            }

            // Convert "a * (b * c)" into "a * b * c"
            if (scope.MatchMultiply(multiplier, out var rightMultiplyLeft, out var rightMultiplyRight))
            {
                return this.SimplifyMultiply(this.SimplifyMultiply(multiplicand, rightMultiplyLeft), rightMultiplyRight);
            }

            // Convert "a * b * 1" into "a * 1 * b"
            if (scope.MatchMultiply(multiplicand, out var leftMultiplyLeft, out var leftMultiplyRight) &&
                this.SortNodes(ref leftMultiplyRight, ref multiplier, constantsFirst: true))
            {
                return this.SimplifyMultiply(this.SimplifyMultiply(leftMultiplyLeft, leftMultiplyRight), multiplier);
            }

            // Convert "(a / b) * c" into "a * c / b"
            if (scope.MatchDivide(multiplicand, out var leftDivideLeft, out var leftDivideRight))
            {
                return this.SimplifyDivide(this.SimplifyMultiply(leftDivideLeft, multiplier), leftDivideRight);
            }

            // Convert "a * (b / c)" into "a * b / c"
            if (scope.MatchDivide(multiplier, out var rightDivideLeft, out var rightDivideRight))
            {
                return this.SimplifyDivide(this.SimplifyMultiply(multiplicand, rightDivideLeft), rightDivideRight);
            }

            // Convert "-a * b" into "-(a * b)"
            if (scope.MatchNegate(multiplicand, out var leftNegateOperand))
            {
                return this.SimplifyNegate(this.SimplifyMultiply(leftNegateOperand, multiplier));
            }

            // Convert "a * -b" into "-(a * b)"
            if (scope.MatchNegate(multiplier, out var rightNegateOperand))
            {
                return this.SimplifyNegate(this.SimplifyMultiply(multiplicand, rightNegateOperand));
            }

            // Convert "a * (b + c)" into "a * b + a * c"
            if (scope.MatchAdd(multiplier, out var rightAddLeft, out var rightAddRight))
            {
                return this.SimplifyAdd(this.SimplifyMultiply(multiplicand, rightAddLeft), this.SimplifyMultiply(multiplicand, rightAddRight));
            }

            // Convert "(a + b) * c" into "a * c + b * c"
            if (scope.MatchAdd(multiplicand, out var leftAddLeft, out var leftAddRight))
            {
                return this.SimplifyAdd(this.SimplifyMultiply(leftAddLeft, multiplier), this.SimplifyMultiply(leftAddRight, multiplier));
            }

            // Convert "a * (b - c)" into "a * b - a * c"
            if (scope.MatchSubtract(multiplier, out var rightSubtractLeft, out var rightSubtractRight))
            {
                return this.SimplifySubtract(this.SimplifyMultiply(multiplicand, rightSubtractLeft), this.SimplifyMultiply(multiplicand, rightSubtractRight));
            }

            // Convert "(a - b) * c" into "a * c - b * c"
            if (scope.MatchSubtract(multiplicand, out var leftSubtractLeft, out var leftSubtractRight))
            {
                return this.SimplifySubtract(this.SimplifyMultiply(leftSubtractLeft, multiplier), this.SimplifyMultiply(leftSubtractRight, multiplier));
            }

            if (this.CombineLikeMultiplication(multiplicand, multiplier, out var combined) ||
                this.CombineLikeMultiplication(multiplicand, multiplier, out combined, relativeRight: true))
            {
                return combined;
            }

            // Convert "a * 2" into "2 * a"
            this.SortNodes(ref multiplicand, ref multiplier, constantsFirst: true);
            return scope.Multiply(multiplicand, multiplier);
        }

        private Expression SimplifyDivide(Expression dividend, Expression divisor)
        {
            if (scope.MatchConstraint(dividend, out var leftCondition, out var leftConsequent))
            {
                return this.SimplifyConditional(leftCondition, this.SimplifyDivide(leftConsequent, divisor), scope.NaN());
            }

            if (scope.MatchConstraint(divisor, out var rightCondition, out var rightConsequent))
            {
                return this.SimplifyConditional(rightCondition, this.SimplifyDivide(dividend, rightConsequent), scope.NaN());
            }

            // Maintain "a / 0"
            if (scope.IsZero(divisor))
            {
                return scope.Divide(dividend, divisor);
            }

            // Convert "a / 1" to "a"
            if (scope.IsOne(divisor))
            {
                return dividend;
            }

            // Convert "0 / a" into "0"
            if (scope.IsZero(dividend))
            {
                return dividend;
            }

            // Convert "-a / b" into "-(a / b)"
            if (scope.MatchNegate(dividend, out var leftNegateOperand))
            {
                return this.SimplifyNegate(this.SimplifyDivide(leftNegateOperand, divisor));
            }

            // Convert "a / -b" into "-(a / b)"
            if (scope.MatchNegate(divisor, out var rightNegateOperand))
            {
                return this.SimplifyNegate(this.SimplifyDivide(dividend, rightNegateOperand));
            }

            // Convert "(a / b) / c" into "a / (b * c)"
            if (scope.MatchDivide(dividend, out var leftDivideLeft, out var leftDivideRight))
            {
                return this.SimplifyDivide(leftDivideLeft, this.SimplifyMultiply(leftDivideRight, divisor));
            }

            // Convert "a / (b / c)" into "(a * c) / b; c != 0"
            if (scope.MatchDivide(divisor, out var rightDivideLeft, out var rightDivideRight))
            {
                return this.SimplifyConditional(this.SimplifyCompare(rightDivideRight, ExpressionType.NotEqual, scope.Zero()), this.SimplifyDivide(this.SimplifyMultiply(dividend, rightDivideRight), rightDivideLeft), scope.NaN());
            }

            // Convert "a / √2" into "a√2 / 2"
            if (scope.MatchSqrt(divisor, out var @base) && scope.IsConstantValue(@base, out var constant) && constant.Value is double value && value >= 0)
            {
                return this.SimplifyDivide(this.SimplifyMultiply(dividend, divisor), @base);
            }

            if (this.CombineLikeMultiplication(dividend, divisor, out var combined, invertRight: true) ||
                this.CombineLikeMultiplication(dividend, divisor, out combined, relativeRight: true, invertRight: true))
            {
                return combined;
            }

            return scope.Divide(dividend, divisor);
        }

        private Expression SimplifyPower(Expression @base, Expression exponent)
        {
            if (scope.MatchConstraint(@base, out var leftCondition, out var leftConsequent))
            {
                return this.SimplifyConditional(leftCondition, this.SimplifyPower(leftConsequent, exponent), scope.NaN());
            }

            if (scope.MatchConstraint(exponent, out var rightCondition, out var rightConsequent))
            {
                return this.SimplifyConditional(rightCondition, this.SimplifyPower(@base, rightConsequent), scope.NaN());
            }

            // Convert "1 ^ a" to "1"
            if (scope.IsOne(@base))
            {
                return @base;
            }

            // Convert "a ^ 1" to "a"
            if (scope.IsOne(exponent))
            {
                return @base;
            }

            // Convert "a ^ 0" to "1"
            if (scope.IsZero(exponent))
            {
                return scope.One();
            }

            if (scope.IsZero(@base))
            {
                // Convert "0 ^ 2" to "0"
                if (scope.IsConstantValue(exponent, out _))
                {
                    return @base;
                }
            }

            if (scope.IsConstantValue(exponent, out var rightConstant))
            {
                // TODO: Support all types.
                if (rightConstant.Value is double rightValue && rightValue > 0)
                {
                    if (scope.IsConstant(@base))
                    {
                        // Convert "2 ^ 2" into "4"
                        return this.EvaluateAsConstant(scope.Pow(@base, exponent));
                    }
                    else if (double.IsInteger(rightValue) && rightValue <= 10 &&
                        scope.TryBindFunction(@base, out var knownFunction, out _) &&
                        (knownFunction == WKF.Arithmetic.Add || knownFunction == WKF.Arithmetic.Subtract))
                    {
                        var totalPower = (int)rightValue;
                        var rightPower = totalPower / 2;
                        var leftPower = totalPower - rightPower;

                        return this.SimplifyMultiply(this.SimplifyPower(@base, Expression.Constant((double)leftPower)), this.SimplifyPower(@base, Expression.Constant((double)rightPower)));
                    }
                }
            }

            // Convert "(a ^ b) ^ c" into "a ^ (b * c)"
            if (scope.MatchPower(@base, out var leftBase, out var leftExponent))
            {
                return this.SimplifyPower(leftBase, this.SimplifyMultiply(leftExponent, exponent));
            }

            return scope.Pow(@base, exponent);
        }

        private Expression SimplifyConditional(Expression condition, Expression consequent, Expression alternative)
        {
            if (scope.IsNaN(alternative))
            {
                if (consequent.NodeType == ExpressionType.Conditional &&
                    consequent is ConditionalExpression conditionalConsequent &&
                    scope.IsNaN(conditionalConsequent.IfFalse))
                {
                    return this.SimplifyConditional(this.SimplifyAnd(condition, conditionalConsequent.Test), conditionalConsequent.IfTrue, conditionalConsequent.IfFalse);
                }
            }

            return scope.Conditional(condition, consequent, alternative);
        }

        private Expression SimplifyCompare(Expression left, ExpressionType op, Expression right)
        {
            return scope.Compare(left, op, right);
        }

        private ConstantExpression EvaluateAsConstant(Expression expression)
        {
            // TODO: Add a configuration option to detect and prevent loss of precision.
            return Expression.Constant(Expression.Lambda<Func<object>>(Expression.Convert(expression, typeof(object)), []).Compile()());
        }

        private bool SortNodes(ref Expression a, ref Expression b, bool constantsFirst = false)
        {
            if (this.CompareNodes(a, b, constantsFirst) < 0)
            {
                (a, b) = (b, a);
                return true;
            }

            return false;
        }

        private int CompareNodes(Expression a, Expression b, bool constantsFirst = false)
        {
            var constantA = scope.IsConstantValue(a, out _);
            var constantB = scope.IsConstantValue(b, out _);
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

        private bool CombineLikeAddition(Expression left, Expression right, out Expression combined, bool relativeRight = false, bool negateRight = false)
        {
            if (relativeRight)
            {
                this.GetFactorAndCoefficient(right, out var rightFactor, out var coefficient);
                if (negateRight)
                {
                    coefficient = this.SimplifyNegate(coefficient ?? scope.One());
                }

                Expression? remainder = left;
                if (this.ExtractByFactor(rightFactor, ref coefficient, ref remainder, false))
                {
                    var newRight = this.SimplifyMultiply(coefficient, rightFactor);
                    combined = remainder == null
                        ? newRight
                        : negateRight
                            ? this.SimplifySubtract(remainder, newRight)
                            : this.SimplifyAdd(remainder, newRight);
                    return true;
                }
            }
            else
            {
                this.GetFactorAndCoefficient(left, out var leftFactor, out var coefficient);

                Expression? remainder = right;
                if (this.ExtractByFactor(leftFactor, ref coefficient, ref remainder, negateRight))
                {
                    var newLeft = this.SimplifyMultiply(coefficient, leftFactor);
                    combined = remainder == null
                        ? newLeft
                        : negateRight
                            ? this.SimplifySubtract(newLeft, remainder)
                            : this.SimplifyAdd(newLeft, remainder);
                    return true;
                }
            }

            combined = null;
            return false;
        }

        private bool ExtractByFactor(Expression factor, [NotNullWhen(true)] ref Expression? coefficient, ref Expression? remainder, bool negate)
        {
            if (scope.MatchAdd(remainder, out var addLeft, out var addRight))
            {
                bool changed;
                changed = this.ExtractByFactor(factor, ref coefficient, ref addLeft, negate);
                changed |= this.ExtractByFactor(factor, ref coefficient, ref addRight, negate);
                if (changed)
                {
                    remainder =
                        addLeft == null ? addRight :
                        addRight == null ? addLeft : this.SimplifyAdd(addLeft, addRight);
                }

                return changed;
            }
            else if (scope.MatchSubtract(remainder, out var subLeft, out var subRight))
            {
                bool changed;
                changed = this.ExtractByFactor(factor, ref coefficient, ref subLeft, negate);
                changed |= this.ExtractByFactor(factor, ref coefficient, ref subRight, !negate);
                if (changed)
                {
                    remainder =
                        subLeft == null ? this.SimplifyNegate(subRight ?? scope.One()) :
                        subRight == null ? subLeft : this.SimplifySubtract(subLeft, subRight);
                }

                return changed;
            }

            if (remainder != null)
            {
                this.GetFactorAndCoefficient(remainder, out var rFactor, out var rCoefficient);
                if (this.matchVisitor.PatternMatch(factor, rFactor).Success)
                {
                    coefficient ??= scope.One();
                    rCoefficient ??= scope.One();
                    coefficient = negate
                        ? this.SimplifySubtract(coefficient, rCoefficient)
                        : this.SimplifyAdd(coefficient, rCoefficient);
                    remainder = null;
                    return true;
                }
            }

            return false;
        }

        private void GetFactorAndCoefficient(Expression expr, out Expression factor, out Expression? coefficient, bool negate = false)
        {
            // Correct, but causes infinte loop with distribution of negate through addition.
            if (false && scope.MatchNegate(expr, out var operand))
            {
                this.GetFactorAndCoefficient(operand, out factor, out coefficient, !negate);
                return;
            }

            if (scope.MatchMultiply(expr, out var left, out var right))
            {
                if (scope.IsConstantValue(left, out _))
                {
                    coefficient = negate ? this.SimplifyNegate(left) : left;
                    factor = right;
                    return;
                }
                else if (scope.IsConstantValue(right, out _))
                {
                    coefficient = negate ? this.SimplifyNegate(right) : right;
                    factor = left;
                    return;
                }
            }

            factor = expr;
            coefficient = negate ? scope.NegativeOne() : null; // null -> one.
        }

        private bool CombineLikeMultiplication(Expression left, Expression right, out Expression combined, bool relativeRight = false, bool invertRight = false)
        {
            if (relativeRight)
            {
                this.GetBaseAndExponent(right, out var rightBase, out var exponent);
                if (invertRight)
                {
                    exponent = exponent == null
                        ? scope.NegativeOne()
                        : this.SimplifyNegate(exponent);
                }

                Expression? remainder = left;
                if (this.ExtractByBase(rightBase, ref exponent, ref remainder, false))
                {
                    var newRight = this.SimplifyPower(rightBase, exponent);
                    combined = remainder == null
                        ? newRight
                        : this.SimplifyMultiply(remainder, newRight);
                    return true;
                }
            }
            else
            {
                this.GetBaseAndExponent(left, out var leftBase, out var exponent);

                Expression? remainder = right;
                if (this.ExtractByBase(leftBase, ref exponent, ref remainder, invertRight))
                {
                    var newLeft = this.SimplifyPower(leftBase, exponent);
                    combined = remainder == null
                        ? newLeft
                        : invertRight
                            ? this.SimplifyDivide(newLeft, remainder)
                            : this.SimplifyMultiply(newLeft, remainder);
                    return true;
                }
            }

            combined = null;
            return false;
        }

        private void GetBaseAndExponent(Expression expr, out Expression @base, out Expression? exponent, bool invert = false)
        {
            if (scope.MatchPower(expr, out var b, out var e))
            {
                @base = b;
                exponent = invert ? this.SimplifyNegate(e) : e;
                return;
            }

            @base = expr;
            exponent = null; // null -> one.
        }

        private bool ExtractByBase(Expression @base, [NotNullWhen(true)] ref Expression? exponent, ref Expression? remainder, bool invert)
        {
            if (scope.MatchMultiply(remainder, out var multiplyLeft, out var multiplyRight))
            {
                bool changed;
                changed = this.ExtractByBase(@base, ref exponent, ref multiplyLeft, invert);
                changed |= this.ExtractByBase(@base, ref exponent, ref multiplyRight, invert);
                if (changed)
                {
                    remainder =
                        multiplyLeft == null ? multiplyRight :
                        multiplyRight == null ? multiplyLeft : this.SimplifyMultiply(multiplyLeft, multiplyRight);
                }

                return changed;
            }
            else if (scope.MatchDivide(remainder, out var divLeft, out var divRight))
            {
                bool changed;
                changed = this.ExtractByBase(@base, ref exponent, ref divLeft, invert);
                changed |= this.ExtractByBase(@base, ref exponent, ref divRight, !invert);
                if (changed)
                {
                    remainder = this.SimplifyDivide(divLeft ?? scope.One(), divRight ?? scope.One());
                }

                return changed;
            }

            if (remainder != null)
            {
                this.GetBaseAndExponent(remainder, out var rBase, out var rExponent);
                if (this.matchVisitor.PatternMatch(@base, rBase).Success)
                {
                    exponent ??= scope.One();
                    rExponent ??= scope.One();
                    exponent = invert
                        ? this.SimplifyConditional(this.SimplifyCompare(@base, ExpressionType.NotEqual, scope.Zero()), this.SimplifySubtract(exponent, rExponent), scope.NaN())
                        : this.SimplifyAdd(exponent, rExponent);
                    remainder = null;
                    return true;
                }
            }

            return false;
        }
    }
}
