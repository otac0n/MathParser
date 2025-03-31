namespace MathParser
{
    using System;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Numerics;
    using static Operations;

    public class SimplifyVisitor : ExpressionVisitor
    {
        /// <inheritdoc/>
        protected override Expression VisitBinary(BinaryExpression node)
        {
            var simpleLeft = this.Visit(node.Left);
            var simpleRight = this.Visit(node.Right);

            switch (node.NodeType)
            {
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                    return this.SimplifyCompare(simpleLeft, node.NodeType, simpleRight);

                case ExpressionType.And:
                case ExpressionType.AndAlso:
                    return this.SimplifyAnd(simpleLeft, simpleRight);

                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    return this.SimplifyOr(simpleLeft, simpleRight);

                case ExpressionType.Add:
                    return this.SimplifyAdd(simpleLeft, simpleRight);

                case ExpressionType.Subtract:
                    return this.SimplifySubtract(simpleLeft, simpleRight);

                case ExpressionType.Multiply:
                    return this.SimplifyMultiply(simpleLeft, simpleRight);

                case ExpressionType.Divide:
                    return this.SimplifyDivide(simpleLeft, simpleRight);

                case ExpressionType.Power:
                    return this.SimplifyPower(simpleLeft, simpleRight);
            }

            return node.Update(simpleLeft, node.Conversion, simpleRight);
        }

        /// <inheritdoc/>
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var simpleObject = node.Object == null ? null : this.Visit(node.Object);
            var simpleArguments = node.Arguments.Select(a => this.Visit(a)).ToList();

            if (node.Object is null && (node.Method.DeclaringType == typeof(Math) || node.Method.DeclaringType == typeof(Complex)))
            {
                switch (node.Method.Name)
                {
                    case nameof(Math.Pow) when simpleArguments.Count == 2:
                        return this.SimplifyPower(simpleArguments[0], simpleArguments[1]);

                    case nameof(Math.Log) when simpleArguments.Count == 1:
                        if (IsConstantEqual(simpleArguments[0], Math.E))
                        {
                            return One();
                        }

                        break;
                }
            }

            return node.Update(simpleObject, simpleArguments);
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

                case ExpressionType.Not:
                    return this.SimplifyNot(simpleOperand);

                case ExpressionType.Negate:
                    return this.SimplifyNegate(simpleOperand);
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

            if (IsConstantValue(expression, out var constant))
            {
                // Convert "not true" into "false"
                if (constant.Value is bool constantValue)
                {
                    return Expression.Constant(!constantValue);
                }
            }

            return Not(expression);
        }

        private Expression SimplifyAnd(Expression left, Expression right)
        {
            // Convert "false and a" into "false"
            if (IsFalse(left))
            {
                return left;
            }

            // Convert "true and a" into "a"
            if (IsTrue(left))
            {
                return right;
            }

            // Convert "a and false" into "false"
            if (IsFalse(right))
            {
                return right;
            }

            // Convert "a and true" into "a"
            if (IsTrue(right))
            {
                return left;
            }

            return And(left, right);
        }

        private Expression SimplifyOr(Expression left, Expression right)
        {
            // Convert "false or a" into "a"
            if (IsFalse(left))
            {
                return right;
            }

            // Convert "true or a" into "true"
            if (IsTrue(left))
            {
                return left;
            }

            // Convert "a or false" into "a"
            if (IsFalse(right))
            {
                return left;
            }

            // Convert "a or true" into "true"
            if (IsTrue(right))
            {
                return right;
            }

            return Or(left, right);
        }

        private Expression SimplifyNegate(Expression operand)
        {
            // Convert "--a" into "a"
            if (operand.NodeType == ExpressionType.Negate && operand is UnaryExpression inner)
            {
                return inner.Operand;
            }

            // Convert "-(a + b)" into "-a - b"
            if (operand.NodeType == ExpressionType.Add && operand is BinaryExpression add)
            {
                return this.Visit(Subtract(Negate(add.Left), add.Right));
            }

            // Convert "-(a - b)" into "b - a"
            if (operand.NodeType == ExpressionType.Subtract && operand is BinaryExpression subtract)
            {
                return this.Visit(Subtract(subtract.Right, subtract.Left));
            }

            // Convert "-(a / b)" into "-a / b"
            if (operand.NodeType == ExpressionType.Divide && operand is BinaryExpression divide)
            {
                return this.Visit(Divide(Negate(divide.Left), divide.Right));
            }

            return Negate(operand);
        }

        private Expression SimplifyAdd(Expression augend, Expression addend)
        {
            // Convert "0 + a" into "a"
            if (IsZero(augend))
            {
                return addend;
            }

            // Convert "a + 0" into "a"
            if (IsZero(addend))
            {
                return augend;
            }

            // Convert "a + (b + c)" into "a + b + c"
            if (addend.NodeType == ExpressionType.Add && addend is BinaryExpression rightAdd)
            {
                return this.Visit(Add(Add(augend, rightAdd.Left), rightAdd.Right));
            }

            // Convert "a + (b - c)" into "a + b - c"
            if (addend.NodeType == ExpressionType.Subtract && addend is BinaryExpression rightSubtract)
            {
                return this.Visit(Subtract(Add(augend, rightSubtract.Left), rightSubtract.Right));
            }

            // Convert "a + -b" into "a - b"
            if (addend.NodeType == ExpressionType.Negate && addend is UnaryExpression rightNegate)
            {
                return this.Visit(Subtract(augend, rightNegate.Operand));
            }

            // Convert "-a + b" into "b - a"
            if (augend.NodeType == ExpressionType.Negate && augend is UnaryExpression leftNegate)
            {
                return this.Visit(Subtract(addend, leftNegate.Operand));
            }

            if (IsConstantValue(augend, out var leftConstant))
            {
                if (IsConstantValue(addend, out var rightConstant))
                {
                    // Convert "1 + 1" into "2"
                    // TODO: Support all types.
                    if (leftConstant.Value is double leftValue && rightConstant.Value is double rightValue)
                    {
                        return Expression.Constant(leftValue + rightValue);
                    }
                }
                else
                {
                    // Convert "1 + a" into "a + 1"
                    (augend, addend) = (addend, augend);
                }
            }

            return Add(augend, addend);
        }

        private Expression SimplifySubtract(Expression minuend, Expression subtrahend)
        {
            // Convert "0 - a" into "-a"
            if (IsZero(minuend))
            {
                return Expression.Negate(subtrahend);
            }

            // Convert "a - 0" into "a"
            if (IsZero(subtrahend))
            {
                return minuend;
            }

            // Convert "a - (b + c)" into "a - b - c"
            if (subtrahend.NodeType == ExpressionType.Add && subtrahend is BinaryExpression rightAdd)
            {
                return this.Visit(Subtract(Subtract(minuend, rightAdd.Left), rightAdd.Right));
            }

            // Convert "a - (b - c)" into "a - b + c"
            if (subtrahend.NodeType == ExpressionType.Subtract && subtrahend is BinaryExpression rightSubtract)
            {
                return this.Visit(Add(Subtract(minuend, rightSubtract.Left), rightSubtract.Right));
            }

            // Convert "a - -b" into "a + b"
            if (subtrahend.NodeType == ExpressionType.Negate && subtrahend is UnaryExpression negate)
            {
                return this.Visit(Add(minuend, negate.Operand));
            }

            return Subtract(minuend, subtrahend);
        }

        private Expression SimplifyMultiply(Expression multiplicand, Expression multiplier)
        {
            // Convert "0 * a" into "0"
            if (IsZero(multiplicand))
            {
                return multiplicand;
            }

            // Convert "1 * a" into "a"
            if (IsOne(multiplicand))
            {
                return multiplier;
            }

            // Convert "a * 0" into "0"
            if (IsZero(multiplier))
            {
                return multiplier;
            }

            // Convert "a * 1" into "a"
            if (IsOne(multiplier))
            {
                return multiplicand;
            }

            // Convert "a * (b * c)" into "a * b * c"
            if (multiplier.NodeType == ExpressionType.Add && multiplier is BinaryExpression rightMultiply)
            {
                return this.Visit(Multiply(Multiply(multiplicand, rightMultiply.Left), rightMultiply.Right));
            }

            // Convert "(a / b) * c" into "a * c / b"
            if (multiplicand.NodeType == ExpressionType.Divide && multiplicand is BinaryExpression leftDivide)
            {
                return this.Visit(Divide(Multiply(leftDivide.Left, multiplier), leftDivide.Right));
            }

            // Convert "a * (b / c)" into "a * b / c"
            if (multiplier.NodeType == ExpressionType.Divide && multiplier is BinaryExpression rightDivide)
            {
                return this.Visit(Divide(Multiply(multiplicand, rightDivide.Left), rightDivide.Right));
            }

            // Convert "-a * b" into "-(a * b)"
            if (multiplicand.NodeType == ExpressionType.Negate && multiplicand is UnaryExpression leftNegate)
            {
                return this.Visit(Negate(Multiply(leftNegate.Operand, multiplier)));
            }

            // Convert "a * -b" into "-(a * b)"
            if (multiplier.NodeType == ExpressionType.Negate && multiplier is UnaryExpression rightNegate)
            {
                return this.Visit(Negate(Multiply(multiplicand, rightNegate.Operand)));
            }

            if (IsConstantValue(multiplier, out var rightConstant))
            {
                if (IsConstantValue(multiplicand, out var leftConstant))
                {
                    // Convert "2 * 2" into "4"
                    if (leftConstant.Value is double leftValue && rightConstant.Value is double rightValue) // TODO: Support all types.
                    {
                        return Expression.Constant(leftValue * rightValue);
                    }
                }
                else
                {
                    // Convert "a * 2" into "2 * a"
                    (multiplicand, multiplier) = (multiplier, multiplicand);
                }
            }

            return Multiply(multiplicand, multiplier);
        }

        private Expression SimplifyDivide(Expression dividend, Expression divisor)
        {
            // Maintain "a / 0"
            if (IsZero(divisor))
            {
                return Divide(dividend, divisor);
            }

            // Convert "a / 1" to "a"
            if (IsOne(divisor))
            {
                return dividend;
            }

            // Convert "0 / a" into "0"
            if (IsZero(dividend))
            {
                return dividend;
            }

            return Divide(dividend, divisor);
        }

        private Expression SimplifyPower(Expression @base, Expression exponent)
        {
            // Convert "1 ^ a" to "1"
            if (IsOne(@base))
            {
                return @base;
            }

            // Convert "a ^ 1" to "a"
            if (IsOne(exponent))
            {
                return @base;
            }

            // Convert "a ^ 0" to "1"
            if (IsZero(exponent))
            {
                return Expression.Constant(1);
            }

            if (IsZero(@base))
            {
                // Convert "0 ^ 2" to "0"
                if (IsConstantValue(exponent, out _))
                {
                    return @base;
                }
            }

            if (IsConstantEqual(@base, Math.E))
            {
                return this.Visit(Exp(exponent));
            }

            return Pow(@base, exponent);
        }

        private Expression SimplifyCompare(Expression left, ExpressionType op, Expression right)
        {
            return Compare(left, op, right);
        }
    }
}
