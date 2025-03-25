// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser.Drawing.VisualNodes
{
    using System;
    using System.Drawing;

    internal class BracketedVisualNode : VisualNode
    {
        public BracketedVisualNode(string leftBracket, VisualNode node, string rightBracket)
        {
            this.LeftBracket = leftBracket;
            this.Node = node;
            this.RightBracket = rightBracket;
        }

        public string LeftBracket { get; }

        public VisualNode Node { get; }

        public string RightBracket { get; }

        public override void Draw(Graphics graphics, Font font, Brush brush, Pen pen, PointF topLeft)
        {
            if (graphics == null)
            {
                throw new ArgumentNullException(nameof(graphics));
            }

            var size = this.Node.Measure(graphics, font, out float baseline);
            var bracketFont = GetBracketFont(graphics, font, this.LeftBracket, this.RightBracket, baseline);

            DrawString(graphics, this.LeftBracket, bracketFont, brush, pen, topLeft);
            topLeft.X += graphics.MeasureString(this.LeftBracket, bracketFont).Width;

            this.Node.Draw(graphics, font, brush, pen, topLeft);
            topLeft.X += size.Width;

            DrawString(graphics, this.RightBracket, bracketFont, brush, pen, topLeft);
        }

        public override SizeF Measure(Graphics graphics, Font font, out float baseline)
        {
            if (graphics == null)
            {
                throw new ArgumentNullException(nameof(graphics));
            }

            var size = this.Node.Measure(graphics, font, out baseline);
            var bracketFont = GetBracketFont(graphics, font, this.LeftBracket, this.RightBracket, baseline);

            var leftBracketSize = graphics.MeasureString(this.LeftBracket, bracketFont);
            var rightBracketSize = graphics.MeasureString(this.RightBracket, bracketFont);

            size.Width += leftBracketSize.Width + rightBracketSize.Width;
            size.Height = Math.Max(size.Height, Math.Max(leftBracketSize.Height, rightBracketSize.Height));

            return size;
        }

        private static Font GetBracketFont(Graphics graphics, Font font, string leftBracket, string rightBracket, float contentBaseline)
        {
            MeasureString(graphics, leftBracket + rightBracket, font, out float normalBaseline);

            if (normalBaseline == contentBaseline)
            {
                return font;
            }

            return new Font(font.FontFamily, font.Size * contentBaseline / normalBaseline, font.Style, font.Unit, font.GdiCharSet, font.GdiVerticalFont);
        }
    }
}
