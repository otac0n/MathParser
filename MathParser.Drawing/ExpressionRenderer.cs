// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser.Drawing
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Drawing.Text;
    using System.Linq.Expressions;
    using System.Runtime.Versioning;
    using MathParser;

    /// <summary>
    /// Renders <see cref="Expression">Expressions</see> a an images.
    /// </summary>
    /// <param name="scope">The scope in which the transformations are performed.</param>
    [SupportedOSPlatform("windows")]
    public class ExpressionRenderer(Scope? scope = null)
    {

        /// <summary>
        /// Gets or sets the brush that will be used when rendering expressions.
        /// </summary>
        public Brush Brush { get; set; } = SystemBrushes.WindowText;

        /// <summary>
        /// Gets or sets the font that will be used when measuring and rendering expressions.
        /// </summary>
        public Font Font { get; set; } = SystemFonts.DefaultFont;

        /// <summary>
        /// Gets or sets the pen that will be used when rendering expressions.
        /// </summary>
        public Pen Pen { get; set; } = new Pen(SystemColors.Window, 2) { Alignment = PenAlignment.Center };

        /// <summary>
        /// Gets the scope in which expressions are interpreted.
        /// </summary>
        public Scope Scope { get; } = scope ?? DefaultScope.Instance;

        /// <summary>
        /// Creates a new <see cref="Graphics"/> object with the recommended settings for rendering text.
        /// </summary>
        /// <param name="image">The target image.</param>
        /// <returns>A new graphics object that can render into the specified bitmap.</returns>
        public static Graphics CreateDefaultGraphics(Image image)
        {
            Graphics graphics = null;
            try
            {
                graphics = Graphics.FromImage(image);
                graphics.InterpolationMode = InterpolationMode.High;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.TextRenderingHint = TextRenderingHint.AntiAlias;

                var result = graphics;
                graphics = null;
                return result;
            }
            finally
            {
                if (graphics != null)
                {
                    graphics.Dispose();
                }
            }
        }

        /// <summary>
        /// Draws the specified expression at the specified location.
        /// </summary>
        /// <param name="graphics">The target <see cref="Graphics"/> object.</param>
        /// <param name="expression">The expression to draw.</param>
        /// <param name="point">The <see cref="PointF"/> specifies the upper-left corner of the drawn expression.</param>
        public void DrawExpression(Graphics graphics, Expression expression, PointF point)
        {
            var visualTree = expression.TransformToVisualTree(this.Scope);
            visualTree.Draw(graphics, this.Font, this.Brush, this.Pen, point);
        }

        /// <summary>
        /// Measures the size of the specified expression.
        /// </summary>
        /// <param name="graphics">The target <see cref="Graphics"/> object.</param>
        /// <param name="expression">The expression to measure.</param>
        /// <returns>The size of the bounding region of the measured expression.</returns>
        public SizeF Measure(Graphics graphics, Expression expression)
        {
            var visualTree = expression.TransformToVisualTree(this.Scope);
            return visualTree.Measure(graphics, this.Font, out _);
        }

        /// <summary>
        /// Measures the size of the specified expression.
        /// </summary>
        /// <param name="expression">The expression to measure.</param>
        /// <returns>The size of the bounding region of the measured expression.</returns>
        public Size Measure(Expression expression)
        {
            using var bitmap = new Bitmap(1, 1);
            using var graphics = CreateDefaultGraphics(bitmap);
            var size = this.Measure(graphics, expression);
            return new Size((int)Math.Ceiling(size.Width), (int)Math.Ceiling(size.Height));
        }

        /// <summary>
        ///  Measures the size of the specified expression.
        /// </summary>
        /// <param name="graphics">The target <see cref="Graphics"/> object.</param>
        /// <param name="expression">The expression to measure.</param>
        /// <param name="baseline">Will be set to the distance, in pixels, from the top of the bounding region to the baseline.</param>
        /// <returns>The size of the bounding region of the measured expression.</returns>
        [SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "2#", Justification = "This is an optional overload. It is left as an out parameter for performance.")]
        public SizeF Measure(Graphics graphics, Expression expression, out float baseline)
        {
            var visualTree = expression.TransformToVisualTree(this.Scope);
            return visualTree.Measure(graphics, this.Font, out baseline);
        }

        /// <summary>
        /// Renders the specified expression to a bitmap.
        /// </summary>
        /// <param name="expression">The expression to render.</param>
        /// <returns>A bitmap containing the expression.</returns>
        public Bitmap RenderExpression(Expression expression)
        {
            var size = this.Measure(expression);
            Bitmap bitmap = null;
            try
            {
                bitmap = new Bitmap(size.Width, size.Height);
                using (var graphics = CreateDefaultGraphics(bitmap))
                {
                    this.DrawExpression(graphics, expression, PointF.Empty);
                }

                var result = bitmap;
                bitmap = null;
                return result;
            }
            finally
            {
                if (bitmap != null)
                {
                    bitmap.Dispose();
                }
            }
        }
    }
}
