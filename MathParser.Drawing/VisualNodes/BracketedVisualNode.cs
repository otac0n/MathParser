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
            ArgumentNullException.ThrowIfNull(graphics);

            var size = this.Node.Measure(graphics, font, out float baseline);
            this.MeasureInternal(graphics, font, out _, out var bracketFont, out var leftOffset, out var leftWidth, out var rightOffset);

            if (this.LeftBracket != null)
            {
                DrawString(graphics, this.LeftBracket, bracketFont, brush, pen, topLeft + leftOffset);
                topLeft.X += leftWidth;
            }

            this.Node.Draw(graphics, font, brush, pen, topLeft);
            topLeft.X += size.Width;

            if (this.RightBracket != null)
            {
                DrawString(graphics, this.RightBracket, bracketFont, brush, pen, topLeft + rightOffset);
            }
        }

        public override SizeF Measure(Graphics graphics, Font font, out float baseline)
        {
            ArgumentNullException.ThrowIfNull(graphics);

            return this.MeasureInternal(graphics, font, out baseline, out _, out _, out _, out _);
        }

        private SizeF MeasureInternal(Graphics graphics, Font font, out float baseline, out Font bracketFont, out SizeF leftOffset, out float leftWidth, out SizeF rightOffset)
        {
            var size = this.Node.Measure(graphics, font, out baseline);

            var top = baseline;
            var bottom = 0f;
            leftOffset = SizeF.Empty;
            rightOffset = SizeF.Empty;

            var leftSize = SizeF.Empty;
            var leftBounds = RectangleF.Empty;
            if (this.LeftBracket != null)
            {
                leftSize = MeasureString(graphics, this.LeftBracket, font, out _);
                using (var path = new GraphicsPath())
                {
                    path.AddString(
                        this.LeftBracket,
                        font.FontFamily,
                        (int)font.Style,
                        graphics.DpiY * font.Size / PointsPerInch,
                        PointF.Empty,
                        StringFormat.GenericDefault);
                    leftBounds = path.GetBounds();
                    top = Math.Min(leftBounds.Top, top);
                    bottom = Math.Max(leftBounds.Bottom, bottom);
                }
            }

            var rightSize = SizeF.Empty;
            var rightBounds = RectangleF.Empty;
            if (this.RightBracket != null)
            {
                rightSize = MeasureString(graphics, this.RightBracket, font, out _);
                using (var path = new GraphicsPath())
                {
                    path.AddString(
                        this.RightBracket,
                        font.FontFamily,
                        (int)font.Style,
                        graphics.DpiY * font.Size / PointsPerInch,
                        PointF.Empty,
                        StringFormat.GenericDefault);
                    rightBounds = path.GetBounds();
                    top = Math.Min(rightBounds.Top, top);
                    bottom = Math.Max(rightBounds.Bottom, bottom);
                }
            }

            var maxHeight = Math.Max(leftSize.Height, rightSize.Height);
            var targetTop = top;
            var targetBottom = size.Height - (maxHeight - bottom);
            var fontScale = Math.Max((targetTop - targetBottom) / (top - bottom), 1);

            bracketFont = fontScale <= 1 ? font : new Font(font.FontFamily, font.Size * fontScale, font.Style, font.Unit, font.GdiCharSet, font.GdiVerticalFont);
            var verticalOffset = (1 - fontScale) * top;
            leftOffset.Width = (1 - fontScale) * leftBounds.X;
            rightOffset.Width = (1 - fontScale) * rightBounds.X;
            rightOffset.Height = leftOffset.Height = verticalOffset;
            leftWidth = leftSize.Width * fontScale + (1 - fontScale) * (leftSize.Width - leftBounds.Width);
            var rightWidth = rightSize.Width * fontScale + (1 - fontScale) * (rightSize.Width - rightBounds.Width);

            size.Width += leftWidth + rightWidth;

            return size;
        }
    }
}
