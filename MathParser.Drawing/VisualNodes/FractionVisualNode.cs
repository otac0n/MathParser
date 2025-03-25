// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser.Drawing.VisualNodes
{
    using System;
    using System.Drawing;
    using System.Drawing.Drawing2D;

    internal class FractionVisualNode : VisualNode
    {
        private const float FontSizeRatio = 0.9F;
        private const string FractionBar = "—";

        public FractionVisualNode(VisualNode dividend, VisualNode divisor)
        {
            this.Dividend = dividend;
            this.Divisor = divisor;
        }

        public VisualNode Dividend { get; private set; }

        public VisualNode Divisor { get; private set; }

        public override void Draw(Graphics graphics, Font font, Brush brush, Pen pen, PointF topLeft)
        {
            var componentFont = GetComponentFont(font);
            var dividendSize = this.Dividend.Measure(graphics, componentFont, out float dividendBaseline);
            var divisorSize = this.Divisor.Measure(graphics, componentFont, out float divisorBaseline);

            RectangleF barBounds;
            using (var path = new GraphicsPath())
            {
                path.AddString(
                    FractionBar,
                    font.FontFamily,
                    (int)font.Style,
                    graphics.DpiY * font.Size / PointsPerInch,
                    PointF.Empty,
                    StringFormat.GenericDefault);

                using (var matrix = new Matrix())
                {
                    barBounds = path.GetBounds();
                    topLeft.X += barBounds.Left;
                    matrix.Scale(Math.Max(dividendSize.Width, divisorSize.Width) / barBounds.Width, 1);
                    path.Transform(matrix);
                    matrix.Reset();
                    barBounds = path.GetBounds();
                    matrix.Translate(topLeft.X - barBounds.Left, topLeft.Y + dividendSize.Height - barBounds.Top);
                    path.Transform(matrix);
                    barBounds = path.GetBounds();
                }

                graphics.DrawPath(pen, path);
                graphics.FillPath(brush, path);
            }

            var maxWidth = Math.Max(dividendSize.Width, divisorSize.Width);

            var dividendLocation = topLeft;
            dividendLocation.X += (maxWidth - dividendSize.Width) / 2;
            this.Dividend.Draw(graphics, componentFont, brush, pen, dividendLocation);

            var divisorLocation = topLeft;
            divisorLocation.Y += dividendSize.Height + barBounds.Height;
            divisorLocation.X += (maxWidth - divisorSize.Width) / 2;
            this.Divisor.Draw(graphics, componentFont, brush, pen, divisorLocation);
        }

        public override SizeF Measure(Graphics graphics, Font font, out float baseline)
        {
            var componentFont = GetComponentFont(font);
            var dividendSize = this.Dividend.Measure(graphics, componentFont, out float dividendBaseline);
            var divisorSize = this.Divisor.Measure(graphics, componentFont, out float divisorBaseline);

            var barSize = MeasureString(graphics, FractionBar, font, out float barBaseline);

            RectangleF barBounds;
            using (var path = new GraphicsPath())
            {
                path.AddString(
                    FractionBar,
                    font.FontFamily,
                    (int)font.Style,
                    graphics.DpiY * font.Size / PointsPerInch,
                    PointF.Empty,
                    StringFormat.GenericDefault);
                barBounds = path.GetBounds();
            }

            var size = new SizeF(
                Math.Max(dividendSize.Width, divisorSize.Width) + barSize.Width - barBounds.Width,
                dividendSize.Height + barBounds.Height + divisorSize.Height);
            baseline = dividendSize.Height + barBaseline - barBounds.Top;
            return size;
        }

        private static Font GetComponentFont(Font font)
        {
            return new Font(font.FontFamily, font.Size * FontSizeRatio, font.Style, font.Unit, font.GdiCharSet, font.GdiVerticalFont);
        }
    }
}
