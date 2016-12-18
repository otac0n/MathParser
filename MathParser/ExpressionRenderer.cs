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

            private enum Associativity
            {
                None = 0,
                Left,
                Right,
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

            protected override Expression VisitUnary(UnaryExpression node)
            {
                base.VisitUnary(node);
                var inner = this.root;

                if (NeedsRightBrackets(node.NodeType, node.Operand))
                {
                    inner = new BracketedVisualNode("(", inner, ")");
                }

                this.root = new BaselineAlignedVisualNode(new StringVisualNode("-"), inner);

                return node;
            }

            private static Associativity GetAssociativity(int precedence)
            {
                switch (precedence)
                {
                    case 0:
                    case 1:
                        return Associativity.Left;

                    case 2:
                        return Associativity.Right;

                    default:
                        return Associativity.None;
                }
            }

            private static int GetPrecedence(ExpressionType type)
            {
                switch (type)
                {
                    case ExpressionType.Add:
                    case ExpressionType.AddChecked:
                    case ExpressionType.Subtract:
                    case ExpressionType.SubtractChecked:
                    case ExpressionType.Negate:
                    case ExpressionType.NegateChecked:
                        return 0;

                    case ExpressionType.Multiply:
                    case ExpressionType.MultiplyChecked:
                    case ExpressionType.Divide:
                    case ExpressionType.Modulo:
                        return 1;

                    case ExpressionType.Power:
                        return 2;

                    default:
                        return int.MaxValue;
                }
            }

            private static bool IsFullyAssociative(ExpressionType left, ExpressionType right)
            {
                if ((left == ExpressionType.Add || left == ExpressionType.AddChecked) &&
                    (right == ExpressionType.Add || right == ExpressionType.AddChecked))
                {
                    return true;
                }

                if ((left == ExpressionType.Multiply || left == ExpressionType.MultiplyChecked) &&
                    (right == ExpressionType.Multiply || right == ExpressionType.MultiplyChecked))
                {
                    return true;
                }

                return false;
            }

            private static bool NeedsLeftBrackets(ExpressionType outer, Expression inner)
            {
                var outerPrecedence = GetPrecedence(outer);
                var innerPrecedence = GetPrecedence(inner.NodeType);
                var innerAssociativity = GetAssociativity(innerPrecedence);
                var fullyAssociative = IsFullyAssociative(inner.NodeType, outer);

                if (outer == ExpressionType.Power && inner.NodeType == ExpressionType.Constant)
                {
                    var value = ((ConstantExpression)inner).Value;
                    if ((dynamic)value < 0)
                    {
                        return true;
                    }
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

            private static bool NeedsRightBrackets(ExpressionType outer, Expression inner)
            {
                var outerPrecedence = GetPrecedence(outer);
                var innerPrecedence = GetPrecedence(inner.NodeType);
                var innerAssociativity = GetAssociativity(innerPrecedence);
                var fullyAssociative = IsFullyAssociative(outer, inner.NodeType);

                if (outerPrecedence < innerPrecedence || fullyAssociative || outer == ExpressionType.Power)
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
        }
    }
}
