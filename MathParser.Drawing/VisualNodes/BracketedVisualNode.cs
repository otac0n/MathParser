// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser.Drawing.VisualNodes
{
    using System;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Runtime.Versioning;

    [SupportedOSPlatform("windows")]
    internal class BracketedVisualNode : VisualNode
    {
        private readonly string bothBrackets;

        public BracketedVisualNode(string leftBracket, VisualNode node, string rightBracket)
        {
            this.LeftBracket = leftBracket;
            this.Node = node;
            this.RightBracket = rightBracket;

            this.bothBrackets = leftBracket + rightBracket;
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
            this.MeasureInternal(graphics, font, out _, out var bracketFont, out var bracketOffset);
            var bracketShift = new SizeF(0, bracketOffset);

            if (this.LeftBracket != null)
            {
                DrawString(graphics, this.LeftBracket, bracketFont, brush, pen, topLeft + bracketShift);
                topLeft.X += graphics.MeasureString(this.LeftBracket, bracketFont).Width;
            }

            this.Node.Draw(graphics, font, brush, pen, topLeft);
            topLeft.X += size.Width;

            if (this.RightBracket != null)
            {
                DrawString(graphics, this.RightBracket, bracketFont, brush, pen, topLeft + bracketShift);
            }
        }

        public override SizeF Measure(Graphics graphics, Font font, out float baseline)
        {
            if (graphics == null)
            {
                throw new ArgumentNullException(nameof(graphics));
            }

            return this.MeasureInternal(graphics, font, out baseline, out var bracketFont, out var bracketOffset);
        }

        private SizeF MeasureInternal(Graphics graphics, Font font, out float baseline, out Font bracketFont, out float bracketOffset)
        {
            var size = this.Node.Measure(graphics, font, out baseline);

            RectangleF bothBounds;
            using (var path = new GraphicsPath())
            {
                path.AddString(
                    this.bothBrackets,
                    font.FontFamily,
                    (int)font.Style,
                    graphics.DpiY * font.Size / PointsPerInch,
                    PointF.Empty,
                    StringFormat.GenericDefault);
                bothBounds = path.GetBounds();
            }

            var bothSize = MeasureString(graphics, this.bothBrackets, font, out _);
            var targetBottom = size.Height - (bothSize.Height - bothBounds.Bottom);
            var fontScale = Math.Max((bothBounds.Top - targetBottom) / (bothBounds.Top - bothBounds.Bottom), 1);

            bracketFont = fontScale <= 1 ? font : new Font(font.FontFamily, font.Size * fontScale, font.Style, font.Unit, font.GdiCharSet, font.GdiVerticalFont);
            bracketOffset = -(fontScale * bothBounds.Top) + bothBounds.Top;

            var leftBracketSize = this.LeftBracket == null ? SizeF.Empty : graphics.MeasureString(this.LeftBracket, bracketFont);
            var rightBracketSize = this.RightBracket == null ? SizeF.Empty : graphics.MeasureString(this.RightBracket, bracketFont);

            size.Width += leftBracketSize.Width + rightBracketSize.Width;
            ////size.Height = Math.Max(size.Height, Math.Max(leftBracketSize.Height, rightBracketSize.Height) + bracketOffset);

            return size;
        }
    }
}
