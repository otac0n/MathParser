// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser.VisualNodes
{
    using System;
    using System.Drawing;

    internal class BrackedVisualNode : VisualNode
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
            var bracketFont = GetBracketFont(g, font, baseline);

            g.DrawString(this.LeftBracket, bracketFont, brush, topLeft);
            topLeft.X += g.MeasureString(this.LeftBracket, bracketFont).Width;

            this.Node.Draw(g, font, brush, topLeft);
            topLeft.X += size.Width;

            g.DrawString(this.RightBracket, bracketFont, brush, topLeft);
        }

        public override SizeF Measure(Graphics g, Font font, out float baseline)
        {
            var size = this.Node.Measure(g, font, out baseline);
            var bracketFont = GetBracketFont(g, font, baseline);

            var leftBracketSize = g.MeasureString(this.LeftBracket, bracketFont);
            var rightBracketSize = g.MeasureString(this.RightBracket, bracketFont);

            size.Width += leftBracketSize.Width + rightBracketSize.Width;
            size.Height = Math.Max(size.Height, Math.Max(leftBracketSize.Height, rightBracketSize.Height));

            return size;
        }

        private static Font GetBracketFont(Graphics g, Font font, float contentBaseline)
        {
            float normalBaseline;
            MeasureString(g, " ", font, out normalBaseline);

            if (normalBaseline == contentBaseline)
            {
                return font;
            }

            return new Font(font.FontFamily, font.Size * contentBaseline / normalBaseline, font.Style, font.Unit, font.GdiCharSet, font.GdiVerticalFont);
        }
    }
}
