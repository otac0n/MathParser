// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser.VisualNodes
{
    using System;
    using System.Drawing;
    using System.Drawing.Drawing2D;

    /// <summary>
    /// Represents a portion of an expression that can be drawn.
    /// </summary>
    public abstract class VisualNode
    {
        /// <summary>
        /// The numer of points per inch.
        /// </summary>
        /// <remarks>
        /// See <see href="https://msdn.microsoft.com/en-us/library/windows/desktop/ff684173(v=vs.85).aspx"/> for more info.
        /// </remarks>
        protected const int PointsPerInch = 72;

        /// <summary>
        /// Measures a string.
        /// </summary>
        /// <param name="graphics">The <see cref="Graphics"/> to respect when measuring.</param>
        /// <param name="text">The text to measure.</param>
        /// <param name="font">The <see cref="Font"/> to use when measuring.</param>
        /// <param name="baseline">Set to the baseline of the text with respect to the top of the bounding rectangle.</param>
        /// <returns>The bounding size of the text.</returns>
        public static SizeF MeasureString(Graphics graphics, string text, Font font, out float baseline)
        {
            if (graphics == null)
            {
                throw new ArgumentNullException(nameof(graphics));
            }

            if (font == null)
            {
                throw new ArgumentNullException(nameof(font));
            }

            var family = font.FontFamily;
            var spacing = family.GetLineSpacing(font.Style);
            var ascent = family.GetCellAscent(font.Style);

            baseline = font.GetHeight(graphics) * ascent / spacing;
            var size = graphics.MeasureString(text, font);
            return size;
        }

        /// <summary>
        /// Draws the specified string.
        /// </summary>
        /// <param name="graphics">The <see cref="Graphics"/> to respect when measuring.</param>
        /// <param name="text">The text to measure.</param>
        /// <param name="font">The <see cref="Font"/> to use when measuring.</param>
        /// <param name="brush">The <see cref="Brush"/> to use to draw.</param>
        /// <param name="pen">The <see cref="Pen"/> to use to draw.</param>
        /// <param name="topLeft">The top left corner of the bounding region that will contain this portion of the expression.</param>
        public static void DrawString(Graphics graphics, string text, Font font, Brush brush, Pen pen, PointF topLeft)
        {
            if (graphics == null)
            {
                throw new ArgumentNullException(nameof(graphics));
            }

            if (font == null)
            {
                throw new ArgumentNullException(nameof(font));
            }

            if (brush == null)
            {
                throw new ArgumentNullException(nameof(brush));
            }

            if (pen == null)
            {
                throw new ArgumentNullException(nameof(pen));
            }

            using (var path = new GraphicsPath())
            {
                path.AddString(
                    text,
                    font.FontFamily,
                    (int)font.Style,
                    graphics.DpiY * font.Size / PointsPerInch,
                    topLeft,
                    StringFormat.GenericDefault);
                graphics.DrawPath(pen, path);
                graphics.FillPath(brush, path);
            }
        }

        /// <summary>
        /// Draws this portion of the expression.
        /// </summary>
        /// <param name="graphics">The <see cref="Graphics"/> object to draw to.</param>
        /// <param name="font">The <see cref="Font"/> to use to draw.</param>
        /// <param name="brush">The <see cref="Brush"/> to use to draw.</param>
        /// <param name="pen">The <see cref="Pen"/> to use to draw.</param>
        /// <param name="topLeft">The top left corner of the bounding region that will contain this portion of the expression.</param>
        public abstract void Draw(Graphics graphics, Font font, Brush brush, Pen pen, PointF topLeft);

        /// <summary>
        /// Measures this portion of the expression.
        /// </summary>
        /// <param name="graphics">The <see cref="Graphics"/> to respect when measuring.</param>
        /// <param name="font">The <see cref="Font"/> to use when measuring.</param>
        /// <param name="baseline">Set to the baseline with respect to the top of the bounding rectangle.</param>
        /// <returns>The bounding size of this portion of the expression.</returns>
        public abstract SizeF Measure(Graphics graphics, Font font, out float baseline);
    }
}
