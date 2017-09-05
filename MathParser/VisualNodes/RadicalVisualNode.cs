// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser.VisualNodes
{
    using System.Drawing;
    using System.Drawing.Drawing2D;

    internal class RadicalVisualNode : VisualNode
    {
        private const string Radical = "√";
        private const string Bar = "—";

        public RadicalVisualNode(VisualNode node)
        {
            this.Node = node;
        }

        public VisualNode Node { get; }

        public override void Draw(Graphics graphics, Font font, Brush brush, Pen pen, PointF topLeft)
        {
            var nodeSize = this.Node.Measure(graphics, font, out float nodeBaseline);

            var barSize = MeasureString(graphics, Bar, font, out float barBaseline);

            RectangleF radicalBounds;
            RectangleF barBounds;
            float topMargin, sideMargin, scaleRatio;
            using (var radicalPath = new GraphicsPath())
            using (var barPath = new GraphicsPath())
            {
                barPath.AddString(
                    Bar,
                    font.FontFamily,
                    (int)font.Style,
                    graphics.DpiY * font.Size / PointsPerInch,
                    PointF.Empty,
                    StringFormat.GenericDefault);

                radicalPath.AddString(
                    Radical,
                    font.FontFamily,
                    (int)font.Style,
                    graphics.DpiY * font.Size / PointsPerInch,
                    PointF.Empty,
                    StringFormat.GenericDefault);

                using (var matrix = new Matrix())
                {
                    radicalBounds = radicalPath.GetBounds();
                    barBounds = barPath.GetBounds();
                    topMargin = barBounds.Height;
                    sideMargin = radicalBounds.Left;
                    scaleRatio = (nodeSize.Height + barBounds.Height) / radicalBounds.Height;

                    matrix.Scale(nodeSize.Width / barBounds.Width, 1);
                    barPath.Transform(matrix);
                    matrix.Reset();
                    barBounds = barPath.GetBounds();

                    matrix.Scale(scaleRatio, scaleRatio);
                    radicalPath.Transform(matrix);
                    matrix.Reset();
                    radicalBounds = radicalPath.GetBounds();

                    matrix.Translate(topLeft.X - barBounds.Left + radicalBounds.Width + sideMargin, topLeft.Y - barBounds.Top + topMargin);
                    barPath.Transform(matrix);
                    matrix.Reset();
                    barBounds = barPath.GetBounds();

                    var overlap = topMargin;
                    matrix.Translate(topLeft.X - radicalBounds.Left + overlap + sideMargin, topLeft.Y - radicalBounds.Top + topMargin);
                    radicalPath.Transform(matrix);
                    matrix.Reset();
                    radicalBounds = radicalPath.GetBounds();
                }

                graphics.DrawPath(pen, barPath);
                graphics.DrawPath(pen, radicalPath);
                graphics.FillPath(brush, barPath);
                graphics.FillPath(brush, radicalPath);
            }

            topLeft.X += radicalBounds.Width + sideMargin;
            topLeft.Y += 2 * barBounds.Height;
            this.Node.Draw(graphics, font, brush, pen, topLeft);
        }

        public override SizeF Measure(Graphics graphics, Font font, out float baseline)
        {
            var nodeSize = this.Node.Measure(graphics, font, out float nodeBaseline);

            var barSize = MeasureString(graphics, Bar, font, out float barBaseline);
            var radicalSize = MeasureString(graphics, Bar, font, out float radicalBaseline);

            RectangleF radicalBounds;
            using (var path = new GraphicsPath())
            {
                path.AddString(
                    Radical,
                    font.FontFamily,
                    (int)font.Style,
                    graphics.DpiY * font.Size / PointsPerInch,
                    PointF.Empty,
                    StringFormat.GenericDefault);
                radicalBounds = path.GetBounds();
            }

            RectangleF barBounds;
            using (var path = new GraphicsPath())
            {
                path.AddString(
                    Bar,
                    font.FontFamily,
                    (int)font.Style,
                    graphics.DpiY * font.Size / PointsPerInch,
                    PointF.Empty,
                    StringFormat.GenericDefault);
                barBounds = path.GetBounds();
            }

            var topMargin = barBounds.Height;
            var sideMargin = radicalBounds.Left;
            var scaleRatio = (nodeSize.Height + barBounds.Height) / radicalBounds.Height;

            baseline = nodeBaseline + barBounds.Height + topMargin;
            var size = nodeSize;
            size.Width += radicalBounds.Width * scaleRatio + 2 * sideMargin;
            size.Height += barBounds.Height + topMargin;
            return size;
        }
    }
}
