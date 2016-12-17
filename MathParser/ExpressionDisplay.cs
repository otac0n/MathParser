// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser
{
    using System;
    using System.Drawing;
    using System.Linq.Expressions;
    using System.Windows.Forms;

    /// <summary>
    /// Provides a way to display a mathematical expression.
    /// </summary>
    public partial class ExpressionDisplay : Form
    {
        private readonly ExpressionRenderer renderer;

        private Expression expression;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionDisplay"/> class.
        /// </summary>
        public ExpressionDisplay()
        {
            this.InitializeComponent();

            var console = NativeMethods.GetConsoleWindow();
            var rect = default(NativeMethods.Rect);
            if (NativeMethods.GetWindowRect(console, ref rect))
            {
                this.StartPosition = FormStartPosition.Manual;
                this.Location = new Point(rect.Right, rect.Top);
            }

            this.UpdateSuggestedSize(SizeF.Empty);
            this.renderer = new ExpressionRenderer
            {
                Font = this.Font,
                Brush = new SolidBrush(this.ForeColor),
            };
        }

        /// <summary>
        /// Gets or sets the displayed expression.
        /// </summary>
        public Expression Expression
        {
            get
            {
                return this.expression;
            }

            set
            {
                this.expression = value;
                this.AccessibleDescription = value?.ToString();
                this.Invalidate();
                if (value == null)
                {
                    this.UpdateSuggestedSize(SizeF.Empty);
                }
            }
        }

        /// <inheritdoc />
        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            this.renderer.Font = this.Font;
            this.Invalidate();
        }

        /// <inheritdoc />
        protected override void OnForeColorChanged(EventArgs e)
        {
            base.OnForeColorChanged(e);
            this.renderer.Brush = new SolidBrush(this.ForeColor);
            this.Invalidate();
        }

        /// <inheritdoc />
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var expression = this.Expression;
            if (expression != null)
            {
                this.UpdateSuggestedSize(this.renderer.Measure(e.Graphics, expression));
                this.renderer.DrawExpression(e.Graphics, expression, PointF.Empty);
            }
        }

        private void UpdateSuggestedSize(SizeF sizeF)
        {
            this.ClientSize = new Size(Math.Max(10, (int)sizeF.Width), Math.Max(10, (int)sizeF.Height));
        }
    }
}
