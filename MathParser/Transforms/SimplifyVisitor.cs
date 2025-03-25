namespace MathParser.Transforms
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
                        if (IsConstantValue(simpleRight, out var rightConstant))
                        {
                            if (IsConstantValue(simpleLeft, out var leftConstant))
                            {
                                //if (leftConstant.Value is T leftValue && rightConstant.Value is T rightValue)
                                //{
                                //    return Expression.Constant(leftValue + rightValue);
                                //}
                            }
                            else
                            {
                                (simpleLeft, simpleRight) = (simpleRight, simpleLeft);
                            }
                        }

                        if (TryConvert(simpleLeft, false, (double x) => x == 0))
                        {
                            return simpleRight;
                        }

                        if (TryConvert(simpleRight, false, (double x) => x == 0))
                        {
                            return simpleLeft;
                        }

                        break;
                    }

                case ExpressionType.Multiply:
                    {
                        if (IsConstantValue(simpleRight, out var rightConstant))
                        {
                            if (IsConstantValue(simpleLeft, out var leftConstant))
                            {
                                //if (leftConstant.Value is T leftValue && rightConstant.Value is T rightValue)
                                //{
                                //    return Expression.Constant(leftValue * rightValue);
                                //}
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
                    }

                    {
                        if (simpleLeft.NodeType == ExpressionType.Divide && simpleLeft is BinaryExpression left)
                        {
                            return this.Visit(Expression.Divide(Expression.Multiply(left.Left, simpleRight), left.Right));
                        }

                        if (simpleRight.NodeType == ExpressionType.Divide && simpleRight is BinaryExpression right)
                        {
                            return this.Visit(Expression.Divide(Expression.Multiply(simpleLeft, right.Left), right.Right));
                        }
                    }

                    {
                        if (simpleLeft.NodeType == ExpressionType.Negate && simpleLeft is UnaryExpression leftNegate)
                        {
                            return this.Visit(Expression.Negate(Expression.Multiply(leftNegate.Operand, simpleRight)));
                        }

                        if (simpleRight.NodeType == ExpressionType.Negate && simpleRight is UnaryExpression rightNegate)
                        {
                            return this.Visit(Expression.Negate(Expression.Multiply(simpleLeft, rightNegate.Operand)));
                        }
                    }

                    break;
            }

            return Expression.MakeBinary(node.NodeType, simpleLeft, simpleRight);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var simpleObject = node.Object == null ? null : this.Visit(node.Object);
            var simpleArguments = node.Arguments.Select(this.Visit).ToList();

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

            return Expression.Call(simpleObject, node.Method, simpleArguments);
        }

        /// <inheritdoc/>
        protected override Expression VisitUnary(UnaryExpression node)
        {
            var simpleOperand = this.Visit(node.Operand);
            switch (node.NodeType)
            {
                case ExpressionType.Negate:
                    {
                        if (simpleOperand.NodeType == ExpressionType.Negate && simpleOperand is UnaryExpression inner)
                        {
                            return inner.Operand;
                        }

                        if (simpleOperand.NodeType == ExpressionType.Divide && simpleOperand is BinaryExpression divide)
                        {
                            return this.Visit(Expression.Divide(Expression.Negate(divide.Left), divide.Right));
                        }

                        break;
                    }
            }

            return Expression.MakeUnary(node.NodeType, simpleOperand, node.Type);
        }
    }
}
