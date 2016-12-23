// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser.Demo
{
    using System;
    using System.Drawing;
    using System.Drawing.Text;
    using System.Linq.Expressions;
    using System.Numerics;
    using System.Reflection;
    using System.Windows.Forms;

    public partial class CalculatorForm : Form
    {
        private readonly Parser parser = new Parser();
        private readonly ExpressionRenderer renderer;
        private int enterState;

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

        private static string ConvertForDisplay(Complex number)
        {
            if (number.Imaginary == 0 || double.IsNaN(number.Imaginary))
            {
                return number.Real.ToString("R");
            }
            else if (number.Real == 0 || double.IsNaN(number.Real))
            {
                return number.Imaginary == 1 ? "i" : number.Imaginary.ToString("R") + "i";
            }
            else
            {
                return number.Real.ToString("R") + "+" + (number.Imaginary == 1 ? "i" : number.Imaginary.ToString("R") + "i");
            }
        }

        private void CalculatorForm_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                if (this.enterState == 0)
                {
                    var expression = this.ParseCurrentInput();
                    if (expression != null)
                    {
                        var input = "(" + this.inputBox.Text + ")";
                        this.inputBox.Text = input;
                        this.inputBox.Select(input.Length, 0);
                        this.enterState = 1;
                        e.Handled = true;
                    }
                }
                else if (this.enterState == 1)
                {
                    var expression = this.ParseCurrentInput();
                    if (expression != null)
                    {
                        this.inputBox.Text = this.Evaluate(expression);
                        this.enterState = 0;
                        e.Handled = true;
                    }
                }

                return;
            }
            else
            {
                this.enterState = 0;
            }

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
                var converted = Expression.Call(
                    typeof(CalculatorForm).GetMethod(nameof(ConvertForDisplay), BindingFlags.NonPublic | BindingFlags.Static),
                    expression.Type == typeof(Complex)
                        ? expression
                        : Expression.Convert(expression, typeof(Complex)));

                return ((Expression<Func<string>>)Expression.Lambda(converted)).Compile()();
            }
            catch
            {
                return "?";
            }
        }

        private void InputBox_TextChanged(object sender, System.EventArgs e)
        {
            var measurementBitmap = new Bitmap(1, 1);
            var expression = this.ParseCurrentInput();
            if (expression != null)
            {
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

                this.resultDisplay.Text = this.Evaluate(expression);
                this.expressionDisplay.Image = bitmap;
            }
            else
            {
                this.expressionDisplay.Image = measurementBitmap;
                this.resultDisplay.Text = "?";
            }
        }

        private Expression ParseCurrentInput()
        {
            try
            {
                return this.parser.Parse(this.inputBox.Text);
            }
            catch (Exception ex) when (ex is FormatException || ex is OverflowException)
            {
                return null;
            }
        }
    }
}
