// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser.VisualNodes
{
    using System;
    using System.Drawing;
    using System.Drawing.Drawing2D;

    internal class FractionVisualNode : VisualNode
    {
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
            var dividendSize = this.Dividend.Measure(graphics, font, out float dividendBaseline);
            var divisorSize = this.Divisor.Measure(graphics, font, out float divisorBaseline);

            RectangleF barBounds;
            using (var path = new GraphicsPath())
            {
                path.AddString(
                    FractionBar,
                    font.FontFamily,
                    (int)font.Style,
                    graphics.DpiY * font.Size / 72,
                    PointF.Empty,
                    StringFormat.GenericDefault);

                using (var matrix = new Matrix())
                {
                    barBounds = path.GetBounds();
                    topLeft.X += barBounds.X;
                    matrix.Scale(Math.Max(dividendSize.Width, divisorSize.Width) / barBounds.Width, 1);
                    path.Transform(matrix);
                    matrix.Reset();
                    barBounds = path.GetBounds();
                    matrix.Translate(topLeft.X - barBounds.X, topLeft.Y + dividendSize.Height - barBounds.Top);
                    path.Transform(matrix);
                    barBounds = path.GetBounds();
                }

                graphics.DrawPath(pen, path);
                graphics.FillPath(brush, path);
            }

            var maxWidth = Math.Max(dividendSize.Width, divisorSize.Width);

            var dividendLocation = topLeft;
            dividendLocation.X += (maxWidth - dividendSize.Width) / 2;
            this.Dividend.Draw(graphics, font, brush, pen, dividendLocation);

            var divisorLocation = topLeft;
            divisorLocation.Y += dividendSize.Height + barBounds.Height;
            divisorLocation.X += (maxWidth - divisorSize.Width) / 2;
            this.Divisor.Draw(graphics, font, brush, pen, divisorLocation);
        }

        public override SizeF Measure(Graphics graphics, Font font, out float baseline)
        {
            var dividendSize = this.Dividend.Measure(graphics, font, out float dividendBaseline);
            var divisorSize = this.Divisor.Measure(graphics, font, out float divisorBaseline);

            var barSize = MeasureString(graphics, FractionBar, font, out float barBaseline);

            RectangleF barBounds;
            using (var path = new GraphicsPath())
            {
                path.AddString(
                    FractionBar,
                    font.FontFamily,
                    (int)font.Style,
                    graphics.DpiY * font.Size / 72,
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
    }
}
