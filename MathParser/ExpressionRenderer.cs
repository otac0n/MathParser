// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Drawing;
    using System.Linq.Expressions;
    using MathParser.VisualNodes;

    /// <summary>
    /// Renders <see cref="Expression">Expressions</see> a an images.
    /// </summary>
    public class ExpressionRenderer
    {
        /// <summary>
        /// Gets or sets the brush that will be used when rendering expressions.
        /// </summary>
        public Brush Brush { get; set; } = SystemBrushes.WindowText;

        /// <summary>
        /// Gets or sets the font that will be used when measuring and rendering expressions.
        /// </summary>
        public Font Font { get; set; } = SystemFonts.DefaultFont;

        /// <summary>
        /// Draws the specified expression at the specified location.
        /// </summary>
        /// <param name="graphics">The target <see cref="Graphics"/> object.</param>
        /// <param name="expression">The expression to draw.</param>
        /// <param name="point">The <see cref="PointF"/> specifies the upper-left corner of the drawn expression.</param>
        public void DrawExpression(Graphics graphics, Expression expression, PointF point)
        {
            var visualTree = ExpressionTransformer.TransformToVisualTree(expression);
            visualTree.Draw(graphics, this.Font, this.Brush, point);
        }

        /// <summary>
        /// Measures the size of the specified expression.
        /// </summary>
        /// <param name="graphics">The target <see cref="Graphics"/> object.</param>
        /// <param name="expression">The expression to measure.</param>
        /// <returns>The size of the bounding region of the measured expression.</returns>
        public SizeF Measure(Graphics graphics, Expression expression)
        {
            var visualTree = ExpressionTransformer.TransformToVisualTree(expression);
            float baseline;
            return visualTree.Measure(graphics, this.Font, out baseline);
        }

        /// <summary>
        ///  Measures the size of the specified expression.
        /// </summary>
        /// <param name="graphics">The target <see cref="Graphics"/> object.</param>
        /// <param name="expression">The expression to measure.</param>
        /// <param name="baseline">Will be set to the distance, in pixels, from the top of the bounding region to the baseline.</param>
        /// <returns>The size of the bounding region of the measured expression.</returns>
        [SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "2#", Justification = "This is an optional overload. It is left as an out parameter for performance.")]
        public SizeF Measure(Graphics graphics, Expression expression, out float baseline)
        {
            var visualTree = ExpressionTransformer.TransformToVisualTree(expression);
            return visualTree.Measure(graphics, this.Font, out baseline);
        }

        private class ExpressionTransformer : ExpressionVisitor
        {
            private VisualNode root;

            private ExpressionTransformer()
            {
            }

            public static VisualNode TransformToVisualTree(Expression expression)
            {
                var transformer = new ExpressionTransformer();
                transformer.Visit(expression);
                return transformer.root;
            }

            protected override Expression VisitBinary(BinaryExpression node)
            {
                this.Visit(node.Left);
                var left = this.root;
                this.Visit(node.Right);
                var right = this.root;

                if (NeedsLeftBrackets(node.NodeType, node.Left))
                {
                    left = new BracketedVisualNode("(", left, ")");
                }

                if (NeedsRightBrackets(node.NodeType, node.Right))
                {
                    right = new BracketedVisualNode("(", right, ")");
                }

                if (node.NodeType == ExpressionType.Power)
                {
                    this.root = new PowerVisualNode(left, right);
                }
                else
                {
                    string op;
                    switch (node.NodeType)
                    {
                        case ExpressionType.Add:
                        case ExpressionType.AddChecked:
                            op = "+";
                            break;

                        case ExpressionType.Subtract:
                        case ExpressionType.SubtractChecked:
                            op = "-";
                            break;

                        case ExpressionType.Multiply:
                        case ExpressionType.MultiplyChecked:
                            op = "×";
                            break;

                        case ExpressionType.Divide:
                            op = "÷";
                            break;

                        default:
                            throw new NotImplementedException();
                    }

                    this.root = new BaselineAlignedVisualNode(left, new StringVisualNode(op), right);
                }

                return node;
            }

            protected override Expression VisitConstant(ConstantExpression node)
            {
                string text = null;

                if (node.Value is double)
                {
                    var value = (double)node.Value;
                    if (value == Math.PI * 2)
                    {
                        text = "τ";
                    }
                    else if (value == Math.PI)
                    {
                        text = "π";
                    }
                }

                this.root = new StringVisualNode(text ?? node.Value?.ToString() ?? "null");
                return node;
            }

            private static bool IsAdditiveExpressionType(ExpressionType outer)
            {
                return outer == ExpressionType.Add ||
                       outer == ExpressionType.AddChecked ||
                       outer == ExpressionType.Subtract ||
                       outer == ExpressionType.SubtractChecked;
            }

            private static bool IsMultiplicativeExpressionType(ExpressionType outer)
            {
                return outer == ExpressionType.Multiply ||
                       outer == ExpressionType.Divide;
            }

            private static bool IsPowerExpressionType(ExpressionType outer)
            {
                return outer == ExpressionType.Power;
            }

            private static bool NeedsLeftBrackets(ExpressionType outer, Expression inner)
            {
                if (IsAdditiveExpressionType(outer))
                {
                    return false;
                }

                if (IsMultiplicativeExpressionType(outer))
                {
                    var binaryExpression = inner as BinaryExpression;
                    return binaryExpression != null && IsAdditiveExpressionType(inner.NodeType);
                }

                if (IsPowerExpressionType(outer))
                {
                    var binaryExpression = inner as BinaryExpression;
                    return binaryExpression != null && (IsAdditiveExpressionType(inner.NodeType) || IsMultiplicativeExpressionType(inner.NodeType) || IsPowerExpressionType(inner.NodeType));
                }

                return false;
            }

            private static bool NeedsRightBrackets(ExpressionType outer, Expression inner)
            {
                if (IsAdditiveExpressionType(outer))
                {
                    return false;
                }

                if (IsMultiplicativeExpressionType(outer))
                {
                    var binaryExpression = inner as BinaryExpression;
                    return binaryExpression != null && (IsAdditiveExpressionType(inner.NodeType) || IsMultiplicativeExpressionType(inner.NodeType));
                }

                return false;
            }
        }
    }
}
