// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser.VisualNodes
{
    using System.Drawing;

    internal abstract class VisualNode
    {
        public static SizeF MeasureString(Graphics g, string text, Font font, out float baseline)
        {
            var family = font.FontFamily;
            var spacing = family.GetLineSpacing(font.Style);
            var ascent = family.GetCellAscent(font.Style);

            baseline = font.GetHeight(g) * ascent / spacing;
            var size = g.MeasureString(text, font);
            return size;
        }

        public abstract void Draw(Graphics g, Font font, Brush brush, PointF topLeft);

        public abstract SizeF Measure(Graphics g, Font font, out float baseline);
    }
}
