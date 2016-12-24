// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser.VisualNodes
{
    using System.Drawing;

    /// <summary>
    /// Represents a portion of an expression that can be drawn.
    /// </summary>
    public abstract class VisualNode
    {
        /// <summary>
        /// Measures a string.
        /// </summary>
        /// <param name="g">The <see cref="Graphics"/> to respect when measuring.</param>
        /// <param name="text">The text to measure.</param>
        /// <param name="font">The <see cref="Font"/> to use when measuring.</param>
        /// <param name="baseline">Set to the baseline of the text with respect to the top of the bounding rectangle.</param>
        /// <returns>The bounding size of the text.</returns>
        public static SizeF MeasureString(Graphics g, string text, Font font, out float baseline)
        {
            var family = font.FontFamily;
            var spacing = family.GetLineSpacing(font.Style);
            var ascent = family.GetCellAscent(font.Style);

            baseline = font.GetHeight(g) * ascent / spacing;
            var size = g.MeasureString(text, font);
            return size;
        }

        /// <summary>
        /// Draws this portion of the expression.
        /// </summary>
        /// <param name="g">The <see cref="Graphics"/> object to draw to.</param>
        /// <param name="font">The <see cref="Font"/> to use to draw.</param>
        /// <param name="brush">The <see cref="Brush"/> to use to draw.</param>
        /// <param name="topLeft">The top left corner of the bounding region that will contain this portion of the expression.</param>
        public abstract void Draw(Graphics g, Font font, Brush brush, PointF topLeft);

        /// <summary>
        /// Measures this portion of the expression.
        /// </summary>
        /// <param name="g">The <see cref="Graphics"/> to respect when measuring.</param>
        /// <param name="font">The <see cref="Font"/> to use when measuring.</param>
        /// <param name="baseline">Set to the baseline with respect to the top of the bounding rectangle.</param>
        /// <returns>The bounding size of this portion of the expression.</returns>
        public abstract SizeF Measure(Graphics g, Font font, out float baseline);
    }
}
