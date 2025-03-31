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
                case ExpressionType.Add:
                    {
                        if (IsConstantValue(simpleLeft, out var leftConstant))
                        {
                            if (IsConstantValue(simpleRight, out var rightConstant))
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
                                (simpleLeft, simpleRight) = (simpleRight, simpleLeft);
                            }
                        }

                        if (IsZero(simpleLeft))
                        {
                            return simpleRight;
                        }

                        if (IsZero(simpleRight))
                        {
                            return simpleLeft;
                        }

                        // Convert "a + (b + c)" into "a + b + c"
                        if (simpleRight.NodeType == ExpressionType.Add && simpleRight is BinaryExpression rightAdd)
                        {
                            return this.Visit(Add(Add(simpleLeft, rightAdd.Left), rightAdd.Right));
                        }

                        // Convert "a + (b - c)" into "a + b - c"
                        if (simpleRight.NodeType == ExpressionType.Subtract && simpleRight is BinaryExpression rightSubtract)
                        {
                            return this.Visit(Subtract(Add(simpleLeft, rightSubtract.Left), rightSubtract.Right));
                        }

                        // Convert "a + -b" into "a - b"
                        if (simpleRight.NodeType == ExpressionType.Negate && simpleRight is UnaryExpression rightNegate)
                        {
                            return this.Visit(Subtract(simpleLeft, rightNegate.Operand));
                        }

                        // Convert "-a + b" into "b - a"
                        if (simpleLeft.NodeType == ExpressionType.Negate && simpleLeft is UnaryExpression leftNegate)
                        {
                            return this.Visit(Subtract(simpleRight, leftNegate.Operand));
                        }

                        return node.Update(ConvertIfLower(simpleLeft, to: simpleRight), null, ConvertIfLower(simpleRight, to: simpleLeft));
                    }

                case ExpressionType.Subtract:
                    {
                        if (IsZero(simpleLeft))
                        {
                            return Expression.Negate(simpleRight);
                        }

                        if (IsZero(simpleRight))
                        {
                            return simpleLeft;
                        }

                        // Convert "a - (b + c)" into "a - b - c"
                        if (simpleRight.NodeType == ExpressionType.Add && simpleRight is BinaryExpression rightAdd)
                        {
                            return this.Visit(Subtract(Subtract(simpleLeft, rightAdd.Left), rightAdd.Right));
                        }

                        // Convert "a - (b - c)" into "a - b + c"
                        if (simpleRight.NodeType == ExpressionType.Subtract && simpleRight is BinaryExpression rightSubtract)
                        {
                            return this.Visit(Add(Subtract(simpleLeft, rightSubtract.Left), rightSubtract.Right));
                        }

                        // Convert "a - -b" into "a + b"
                        if (simpleRight.NodeType == ExpressionType.Negate && simpleRight is UnaryExpression negate)
                        {
                            return this.Visit(Add(simpleLeft, negate.Operand));
                        }

                        return node.Update(ConvertIfLower(simpleLeft, to: simpleRight), null, ConvertIfLower(simpleRight, to: simpleLeft));
                    }

                case ExpressionType.Multiply:
                    {
                        if (IsConstantValue(simpleRight, out var rightConstant))
                        {
                            if (IsConstantValue(simpleLeft, out var leftConstant))
                            {
                                // Convert "2 * 2" into "4"
                                // TODO: Support all types.
                                if (leftConstant.Value is double leftValue && rightConstant.Value is double rightValue)
                                {
                                    return Expression.Constant(leftValue * rightValue);
                                }
                            }
                            else
                            {
                                (simpleLeft, simpleRight) = (simpleRight, simpleLeft);
                            }
                        }

                        if (IsZero(simpleLeft))
                        {
                            return simpleLeft;
                        }
                        else if (IsOne(simpleLeft))
                        {
                            return simpleRight;
                        }

                        if (IsZero(simpleRight))
                        {
                            return simpleRight;
                        }
                        else if (IsOne(simpleRight))
                        {
                            return simpleLeft;
                        }

                        // Convert "(a / b) * c" into "a * c / b"
                        if (simpleLeft.NodeType == ExpressionType.Divide && simpleLeft is BinaryExpression leftDivide)
                        {
                            return this.Visit(Divide(Multiply(leftDivide.Left, simpleRight), leftDivide.Right));
                        }

                        // Convert "a * (b / c)" into "a * b / c"
                        if (simpleRight.NodeType == ExpressionType.Divide && simpleRight is BinaryExpression rightDivide)
                        {
                            return this.Visit(Divide(Multiply(simpleLeft, rightDivide.Left), rightDivide.Right));
                        }

                        // Convert "-a * b" into "-(a * b)"
                        if (simpleLeft.NodeType == ExpressionType.Negate && simpleLeft is UnaryExpression leftNegate)
                        {
                            return this.Visit(Negate(Multiply(leftNegate.Operand, simpleRight)));
                        }

                        // Convert "a * -b" into "-(a * b)"
                        if (simpleRight.NodeType == ExpressionType.Negate && simpleRight is UnaryExpression rightNegate)
                        {
                            return this.Visit(Negate(Multiply(simpleLeft, rightNegate.Operand)));
                        }

                        return node.Update(ConvertIfLower(simpleLeft, to: simpleRight), null, ConvertIfLower(simpleRight, to: simpleLeft));
                    }

                case ExpressionType.Divide:
                    {
                        if (IsZero(simpleRight))
                        {
                            return Divide(simpleLeft, simpleRight);
                        }
                        else if (IsOne(simpleRight))
                        {
                            return simpleLeft;
                        }

                        if (IsZero(simpleLeft))
                        {
                            return simpleLeft;
                        }

                        return node.Update(ConvertIfLower(simpleLeft, to: simpleRight), null, ConvertIfLower(simpleRight, to: simpleLeft));
                    }
            }

            return node.Update(simpleLeft, node.Conversion, simpleRight);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var simpleObject = node.Object == null ? null : this.Visit(node.Object);
            var simpleArguments = node.Arguments.Select(a => this.Visit(a)).ToList();

            if (node.Object is null && (node.Method.DeclaringType == typeof(Math) || node.Method.DeclaringType == typeof(Complex)))
            {
                switch (node.Method.Name)
                {
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

                case ExpressionType.Negate:
                    {
                        if (simpleOperand.NodeType == ExpressionType.Negate && simpleOperand is UnaryExpression inner)
                        {
                            return inner.Operand;
                        }

                        if (simpleOperand.NodeType == ExpressionType.Add && simpleOperand is BinaryExpression add)
                        {
                            return this.Visit(Subtract(Negate(add.Left), add.Right));
                        }

                        if (simpleOperand.NodeType == ExpressionType.Subtract && simpleOperand is BinaryExpression subtract)
                        {
                            return this.Visit(Subtract(subtract.Right, subtract.Left));
                        }

                        if (simpleOperand.NodeType == ExpressionType.Divide && simpleOperand is BinaryExpression divide)
                        {
                            return this.Visit(Divide(Negate(divide.Left), divide.Right));
                        }

                        break;
                    }
            }

            return node.Update(simpleOperand);
        }
    }
}
