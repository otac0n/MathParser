// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Drawing;
    using System.Linq;
    using System.Linq.Expressions;

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

        private class BaselineAlignedVisualNode : VisualNode
        {
            public BaselineAlignedVisualNode(params VisualNode[] nodes)
            {
                this.Nodes = nodes;
            }

            public VisualNode[] Nodes { get; }

            public override void Draw(Graphics g, Font font, Brush brush, PointF topLeft)
            {
                float[] baselines;
                SizeF[] sizes;
                float baseline;
                var size = this.MeasureInternal(g, font, out baseline, out sizes, out baselines);

                var offset = SizeF.Empty;
                var nodes = this.Nodes.Length;
                for (int i = 0; i < nodes; i++)
                {
                    offset.Height = baseline - baselines[i];
                    this.Nodes[i].Draw(g, font, brush, topLeft + offset);
                    offset.Width += sizes[i].Width;
                }
            }

            public override SizeF Measure(Graphics g, Font font, out float baseline)
            {
                float[] baselines;
                SizeF[] sizes;
                return this.MeasureInternal(g, font, out baseline, out sizes, out baselines);
            }

            private SizeF MeasureInternal(Graphics g, Font font, out float baseline, out SizeF[] sizes, out float[] baselines)
            {
                var nodes = this.Nodes.Length;

                baselines = new float[nodes];
                sizes = new SizeF[nodes];
                for (int i = 0; i < nodes; i++)
                {
                    sizes[i] = this.Nodes[i].Measure(g, font, out baselines[i]);
                }

                var currentBaseline = baselines.Max();
                var currentSize = new SizeF(
                    sizes.Sum(s => s.Width),
                    sizes.Max(s => s.Height));

                var currentDiff = currentSize.Height - currentBaseline;
                var maxOffset = 0F;
                for (int i = 0; i < nodes; i++)
                {
                    var offset = sizes[i].Height - baselines[i] - currentDiff;
                    if (offset > maxOffset)
                    {
                        maxOffset = offset;
                    }
                }

                if (maxOffset > 0)
                {
                    currentSize.Height += maxOffset;
                }

                baseline = currentBaseline;
                return currentSize;
            }
        }

        private class BrackedVisualNode : VisualNode
        {
            public BrackedVisualNode(string leftBracket, VisualNode node, string rightBracket)
            {
                this.LeftBracket = leftBracket;
                this.Node = node;
                this.RightBracket = rightBracket;
            }

            public string LeftBracket { get; }

            public VisualNode Node { get; }

            public string RightBracket { get; }

            public override void Draw(Graphics g, Font font, Brush brush, PointF topLeft)
            {
                float baseline;
                var size = this.Node.Measure(g, font, out baseline);

                var bracketFont = GetBracketFont(g, font, size);

                var leftBracketSize = g.MeasureString(this.LeftBracket, bracketFont);
                var rightBracketSize = g.MeasureString(this.RightBracket, bracketFont);

                g.DrawString(this.LeftBracket, bracketFont, brush, topLeft);
                topLeft.X += leftBracketSize.Width;

                this.Node.Draw(g, font, brush, topLeft);
                topLeft.X += size.Width;

                g.DrawString(this.RightBracket, bracketFont, brush, topLeft);
            }

            public override SizeF Measure(Graphics g, Font font, out float baseline)
            {
                var size = this.Node.Measure(g, font, out baseline);

                var bracketFont = GetBracketFont(g, font, size);

                var leftBracketSize = g.MeasureString(this.LeftBracket, bracketFont);
                var rightBracketSize = g.MeasureString(this.RightBracket, bracketFont);

                size.Width += leftBracketSize.Width + rightBracketSize.Width;

                return size;
            }

            private static Font GetBracketFont(Graphics g, Font font, SizeF contentSize)
            {
                var normalSize = g.MeasureString(" ", font);

                if (normalSize.Height == contentSize.Height)
                {
                    return font;
                }

                return new Font(font.FontFamily, font.Size * contentSize.Height / normalSize.Height, font.Style, font.Unit, font.GdiCharSet, font.GdiVerticalFont);
            }
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

                if (node.NodeType == ExpressionType.Power)
                {
                    if (NeedsBrackets(node.NodeType, node.Left))
                    {
                        left = new BrackedVisualNode("(", left, ")");
                    }

                    this.root = new PowerVisualNode(left, right);
                }
                else
                {
                    if (NeedsBrackets(node.NodeType, node.Left))
                    {
                        left = new BrackedVisualNode("(", left, ")");
                    }

                    if (NeedsBrackets(node.NodeType, node.Right))
                    {
                        right = new BrackedVisualNode("(", right, ")");
                    }

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
                this.root = new StringVisualNode(node.Value);
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

            private static bool NeedsBrackets(ExpressionType outer, Expression inner)
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
        }

        private class PowerVisualNode : VisualNode
        {
            private const float FontSizeRatio = 0.6F;

            public PowerVisualNode(VisualNode left, VisualNode right)
            {
                this.Left = left;
                this.Right = right;
            }

            public VisualNode Left { get; }

            public VisualNode Right { get; }

            public override void Draw(Graphics g, Font font, Brush brush, PointF topLeft)
            {
                var superFont = GetSuperFont(font);

                float leftBaseline, rightBaseline;
                var leftSize = this.Left.Measure(g, font, out leftBaseline);
                var rightSize = this.Right.Measure(g, superFont, out rightBaseline);

                var leftOffset = rightSize.Height - leftBaseline / 2;
                var leftLocation = new PointF(topLeft.X, topLeft.Y + (leftOffset > 0 ? leftOffset : 0));
                this.Left.Draw(g, font, brush, leftLocation);

                var rightOffset = leftBaseline / 2 - rightSize.Height;
                var rightLocation = new PointF(topLeft.X + leftSize.Width, topLeft.Y + (rightOffset > 0 ? rightOffset : 0));
                this.Right.Draw(g, superFont, brush, rightLocation);
            }

            public override SizeF Measure(Graphics g, Font font, out float baseline)
            {
                var superFont = GetSuperFont(font);

                float leftBaseline, rightBaseline;
                var leftSize = this.Left.Measure(g, font, out leftBaseline);
                var rightSize = this.Right.Measure(g, superFont, out rightBaseline);

                var leftOffset = rightSize.Height - leftBaseline / 2;

                leftSize.Width += rightSize.Width;
                if (leftOffset > 0)
                {
                    leftSize.Height += leftOffset;
                    leftBaseline += leftOffset;
                }

                baseline = leftBaseline;
                return leftSize;
            }

            private static Font GetSuperFont(Font font)
            {
                return new Font(font.FontFamily, font.Size * FontSizeRatio, font.Style, font.Unit, font.GdiCharSet, font.GdiVerticalFont);
            }
        }

        private class StringVisualNode : VisualNode
        {
            public StringVisualNode(object value)
            {
                this.Value = value?.ToString() ?? string.Empty;
            }

            public string Value { get; }

            public override void Draw(Graphics g, Font font, Brush brush, PointF topLeft)
            {
                g.DrawString(this.Value, font, brush, topLeft);
            }

            public override SizeF Measure(Graphics g, Font font, out float baseline)
            {
                var family = font.FontFamily;
                var spacing = family.GetLineSpacing(font.Style);
                var ascent = family.GetCellAscent(font.Style);

                baseline = font.GetHeight(g) * ascent / spacing;
                var size = g.MeasureString(this.Value, font);
                return size;
            }
        }

        private abstract class VisualNode
        {
            public abstract void Draw(Graphics g, Font font, Brush brush, PointF topLeft);

            public abstract SizeF Measure(Graphics g, Font font, out float baseline);
        }
    }
}
