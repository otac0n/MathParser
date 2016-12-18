// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser.Demo
{
    using System;
    using System.Drawing;
    using System.Drawing.Text;
    using System.Linq.Expressions;
    using System.Windows.Forms;

    public partial class CalculatorForm : Form
    {
        private readonly Parser parser = new Parser();
        private readonly ExpressionRenderer renderer;

        public CalculatorForm()
        {
            this.InitializeComponent();
            this.renderer = new ExpressionRenderer
            {
                Font = new Font("Calibri", 20, FontStyle.Regular),
                Brush = new SolidBrush(this.ForeColor),
            };
            this.InputBox_TextChanged(this, null);
        }

        private void CalculatorForm_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Escape)
            {
                this.inputBox.Text = string.Empty;
                e.Handled = true;
            }
        }

        private string Evaluate(Expression expression)
        {
            try
            {
                return ((Expression<Func<double>>)Expression.Lambda(expression)).Compile()().ToString();
            }
            catch
            {
                return "?";
            }
        }

        private void InputBox_TextChanged(object sender, System.EventArgs e)
        {
            var measurementBitmap = new Bitmap(1, 1);
            var input = this.inputBox.Text;
            try
            {
                var expression = this.parser.Parse(input);

                SizeF size;
                using (var graphics = Graphics.FromImage(measurementBitmap))
                {
                    graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                    size = this.renderer.Measure(graphics, expression);
                }

                const float Padding = 5;
                var bitmap = new Bitmap((int)Math.Ceiling(size.Width + Padding * 2), (int)Math.Ceiling(size.Height + Padding * 2));
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                    this.renderer.DrawExpression(graphics, expression, new PointF(Padding, Padding));
                }

                this.resultDisplay.Text = "= " + this.Evaluate(expression);
                this.expressionDisplay.Image = bitmap;
            }
            catch (FormatException)
            {
                this.expressionDisplay.Image = measurementBitmap;
                this.resultDisplay.Text = "= ?";
                return;
            }
        }
    }
}
