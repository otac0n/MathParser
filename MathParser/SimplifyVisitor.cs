namespace MathParser
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Linq.Expressions;
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

            if (function == WKF.Exponential.Pow && arguments.Count == 2)
            {
                var @base = converted[0];
                var exponent = converted[1];
                return this.SimplifyPower(@base, exponent);
            }
            else if (function == WKF.Exponential.Ln && arguments.Count == 1)
            {
                if (this.Scope.IsConstantEqual(converted[0], Math.E))
                {
                    return this.Scope.One();
                }
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
            if (expression.NodeType == ExpressionType.Not && expression is UnaryExpression inner)
            {
                return inner.Operand;
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
            if (left == right)
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
            if (left == right)
            {
                return left;
            }

            return this.Scope.Or(left, right);
        }

        private Expression SimplifyNegate(Expression operand)
        {
            if (this.Scope.IsConstraint(operand, out var condition, out var consequent))
            {
                return this.Visit(this.Scope.Conditional(condition, this.Scope.Negate(consequent), this.Scope.NaN()));
            }

            // Convert "--a" into "a"
            if (operand.NodeType == ExpressionType.Negate && operand is UnaryExpression inner)
            {
                return inner.Operand;
            }

            // Convert "-(a + b)" into "-a - b"
            if (operand.NodeType == ExpressionType.Add && operand is BinaryExpression add)
            {
                return this.Visit(this.Scope.Subtract(this.Scope.Negate(add.Left), add.Right));
            }

            // Convert "-(a - b)" into "b - a"
            if (operand.NodeType == ExpressionType.Subtract && operand is BinaryExpression subtract)
            {
                return this.Visit(this.Scope.Subtract(subtract.Right, subtract.Left));
            }

            // Convert "-(a / b)" into "-a / b"
            if (operand.NodeType == ExpressionType.Divide && operand is BinaryExpression divide)
            {
                return this.Visit(this.Scope.Divide(this.Scope.Negate(divide.Left), divide.Right));
            }

            return this.Scope.Negate(operand);
        }

        private Expression SimplifyAdd(Expression augend, Expression addend)
        {
            if (this.Scope.IsConstraint(augend, out var leftCondition, out var leftConsequent))
            {
                return this.Visit(this.Scope.Conditional(leftCondition, this.Scope.Add(leftConsequent, addend), this.Scope.NaN()));
            }

            if (this.Scope.IsConstraint(addend, out var rightCondition, out var rightConsequent))
            {
                return this.Visit(this.Scope.Conditional(rightCondition, this.Scope.Add(augend, rightConsequent), this.Scope.NaN()));
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

            // Convert "a + (b + c)" into "a + b + c"
            if (addend.NodeType == ExpressionType.Add && addend is BinaryExpression rightAdd)
            {
                return this.Visit(this.Scope.Add(this.Scope.Add(augend, rightAdd.Left), rightAdd.Right));
            }

            // Convert "a + (b - c)" into "a + b - c"
            if (addend.NodeType == ExpressionType.Subtract && addend is BinaryExpression rightSubtract)
            {
                return this.Visit(this.Scope.Subtract(this.Scope.Add(augend, rightSubtract.Left), rightSubtract.Right));
            }

            // Convert "a + -b" into "a - b"
            if (addend.NodeType == ExpressionType.Negate && addend is UnaryExpression rightNegate)
            {
                return this.Visit(this.Scope.Subtract(augend, rightNegate.Operand));
            }

            // Convert "-a + b" into "b - a"
            if (augend.NodeType == ExpressionType.Negate && augend is UnaryExpression leftNegate)
            {
                return this.Visit(this.Scope.Subtract(addend, leftNegate.Operand));
            }

            // Convert "a + a" into "2 * a"
            if (augend == addend)
            {
                return this.Visit(this.Scope.Multiply(Expression.Constant(2.0), augend));
            }

            if (this.Scope.IsConstantValue(augend, out var leftConstant))
            {
                if (this.Scope.IsConstantValue(addend, out var rightConstant))
                {
                    // Convert "1 + 1" into "2"
                    // TODO: Support all types.
                    if (leftConstant.Value is double leftValue && rightConstant.Value is double rightValue)
                    {
                        // TODO: Add a configuration option to detect and prevent loss of precision.
                        return Expression.Constant(leftValue + rightValue);
                    }
                }
                else
                {
                    // Convert "1 + a" into "a + 1"
                    (augend, addend) = (addend, augend);
                }
            }

            return this.Scope.Add(augend, addend);
        }

        private Expression SimplifySubtract(Expression minuend, Expression subtrahend)
        {
            if (this.Scope.IsConstraint(minuend, out var leftCondition, out var leftConsequent))
            {
                return this.Visit(this.Scope.Conditional(leftCondition, this.Scope.Subtract(leftConsequent, subtrahend), this.Scope.NaN()));
            }

            if (this.Scope.IsConstraint(subtrahend, out var rightCondition, out var rightConsequent))
            {
                return this.Visit(this.Scope.Conditional(rightCondition, this.Scope.Subtract(minuend, rightConsequent), this.Scope.NaN()));
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

            // Convert "a - (b + c)" into "a - b - c"
            if (subtrahend.NodeType == ExpressionType.Add && subtrahend is BinaryExpression rightAdd)
            {
                return this.Visit(this.Scope.Subtract(this.Scope.Subtract(minuend, rightAdd.Left), rightAdd.Right));
            }

            // Convert "a - (b - c)" into "a - b + c"
            if (subtrahend.NodeType == ExpressionType.Subtract && subtrahend is BinaryExpression rightSubtract)
            {
                return this.Visit(this.Scope.Add(this.Scope.Subtract(minuend, rightSubtract.Left), rightSubtract.Right));
            }

            // Convert "a - -b" into "a + b"
            if (subtrahend.NodeType == ExpressionType.Negate && subtrahend is UnaryExpression negate)
            {
                return this.Visit(this.Scope.Add(minuend, negate.Operand));
            }

            // Convert "a - a" into "0"
            if (subtrahend == minuend)
            {
                return this.Scope.Zero();
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

            return this.Scope.Subtract(minuend, subtrahend);
        }

        private Expression SimplifyMultiply(Expression multiplicand, Expression multiplier)
        {
            if (this.Scope.IsConstraint(multiplicand, out var leftCondition, out var leftConsequent))
            {
                return this.Visit(this.Scope.Conditional(leftCondition, this.Scope.Multiply(leftConsequent, multiplier), this.Scope.NaN()));
            }

            if (this.Scope.IsConstraint(multiplier, out var rightCondition, out var rightConsequent))
            {
                return this.Visit(this.Scope.Conditional(rightCondition, this.Scope.Multiply(multiplicand, rightConsequent), this.Scope.NaN()));
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

            // Convert "a * (b * c)" into "a * b * c"
            if (multiplier.NodeType == ExpressionType.Multiply && multiplier is BinaryExpression rightMultiply)
            {
                return this.Visit(this.Scope.Multiply(this.Scope.Multiply(multiplicand, rightMultiply.Left), rightMultiply.Right));
            }

            // Convert "(a / b) * c" into "a * c / b"
            if (multiplicand.NodeType == ExpressionType.Divide && multiplicand is BinaryExpression leftDivide)
            {
                return this.Visit(this.Scope.Divide(this.Scope.Multiply(leftDivide.Left, multiplier), leftDivide.Right));
            }

            // Convert "a * (b / c)" into "a * b / c"
            if (multiplier.NodeType == ExpressionType.Divide && multiplier is BinaryExpression rightDivide)
            {
                return this.Visit(this.Scope.Divide(this.Scope.Multiply(multiplicand, rightDivide.Left), rightDivide.Right));
            }

            // Convert "-a * b" into "-(a * b)"
            if (multiplicand.NodeType == ExpressionType.Negate && multiplicand is UnaryExpression leftNegate)
            {
                return this.Visit(this.Scope.Negate(this.Scope.Multiply(leftNegate.Operand, multiplier)));
            }

            // Convert "a * -b" into "-(a * b)"
            if (multiplier.NodeType == ExpressionType.Negate && multiplier is UnaryExpression rightNegate)
            {
                return this.Visit(this.Scope.Negate(this.Scope.Multiply(multiplicand, rightNegate.Operand)));
            }

            // Convert "a * (b + c)" into "a * b + a * c"
            if (multiplier.NodeType == ExpressionType.Add && multiplier is BinaryExpression rightAdd)
            {
                return this.Visit(this.Scope.Add(this.Scope.Multiply(multiplicand, rightAdd.Left), this.Scope.Multiply(multiplicand, rightAdd.Right)));
            }

            // Convert "(a + b) * c" into "a * c + b * c"
            if (multiplicand.NodeType == ExpressionType.Add && multiplicand is BinaryExpression leftAdd)
            {
                return this.Visit(this.Scope.Add(this.Scope.Multiply(leftAdd.Left, multiplier), this.Scope.Multiply(leftAdd.Right, multiplier)));
            }

            // Convert "a * (b - c)" into "a * b - a * c"
            if (multiplier.NodeType == ExpressionType.Subtract && multiplier is BinaryExpression rightSubtract)
            {
                return this.Visit(this.Scope.Subtract(this.Scope.Multiply(multiplicand, rightSubtract.Left), this.Scope.Multiply(multiplicand, rightSubtract.Right)));
            }

            // Convert "(a - b) * c" into "a * c - b * c"
            if (multiplicand.NodeType == ExpressionType.Subtract && multiplicand is BinaryExpression leftSubtract)
            {
                return this.Visit(this.Scope.Subtract(this.Scope.Multiply(leftSubtract.Left, multiplier), this.Scope.Multiply(leftSubtract.Right, multiplier)));
            }

            // Convert "a * a" into "a ^ 2"
            if (multiplicand == multiplier)
            {
                return this.Visit(this.Scope.Pow(multiplicand, Expression.Constant(2.0)));
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
                else
                {
                    // Convert "a * 2" into "2 * a"
                    (multiplicand, multiplier) = (multiplier, multiplicand);
                }
            }

            return this.Scope.Multiply(multiplicand, multiplier);
        }

        private Expression SimplifyDivide(Expression dividend, Expression divisor)
        {
            if (this.Scope.IsConstraint(dividend, out var leftCondition, out var leftConsequent))
            {
                return this.Visit(this.Scope.Conditional(leftCondition, this.Scope.Divide(leftConsequent, divisor), this.Scope.NaN()));
            }

            if (this.Scope.IsConstraint(divisor, out var rightCondition, out var rightConsequent))
            {
                return this.Visit(this.Scope.Conditional(rightCondition, this.Scope.Divide(dividend, rightConsequent), this.Scope.NaN()));
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
            if (this.Scope.IsSqrt(divisor, out var @base) && this.Scope.IsConstantValue(@base, out var constant) && constant.Value is double value && value >= 0)
            {
                return this.Visit(this.Scope.Divide(this.Scope.Multiply(dividend, divisor), @base));
            }

            // Convert "a / a" into "a; a!=0"
            if (dividend == divisor)
            {
                return this.Visit(this.Scope.Conditional(this.Scope.NotEqual(divisor, this.Scope.Zero()), this.Scope.One(), this.Scope.NaN()));
            }

            return this.Scope.Divide(dividend, divisor);
        }

        private Expression SimplifyPower(Expression @base, Expression exponent)
        {
            if (this.Scope.IsConstraint(@base, out var leftCondition, out var leftConsequent))
            {
                return this.Visit(this.Scope.Conditional(leftCondition, this.Scope.Pow(leftConsequent, exponent), this.Scope.NaN()));
            }

            if (this.Scope.IsConstraint(exponent, out var rightCondition, out var rightConsequent))
            {
                return this.Visit(this.Scope.Conditional(rightCondition, this.Scope.Pow(@base, rightConsequent), this.Scope.NaN()));
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

            // Convert "(a ^ b) ^ c" into "a ^ (b * c)"
            if (this.Scope.IsPower(@base, out var leftBase, out var leftExponent))
            {
                return this.Visit(this.Scope.Pow(leftBase, this.Scope.Multiply(leftExponent, exponent)));
            }

            if (this.Scope.IsConstantEqual(@base, Math.E))
            {
                return this.Visit(this.Scope.Exp(exponent));
            }

            if (this.Scope.IsConstantValue(exponent, out var rightConstant))
            {
                if (this.Scope.IsConstantValue(@base, out var leftConstant))
                {
                    // Convert "2 ^ 2" into "4"
                    if (leftConstant.Value is double leftValue && rightConstant.Value is double rightValue && rightValue >= 0) // TODO: Support all types.
                    {
                        // TODO: Add a configuration option to detect and prevent loss of precision.
                        return Expression.Constant(Math.Pow(leftValue, rightValue));
                    }
                }
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
    }
}
