// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser.Demo
{
    using System;
    using System.Drawing;
    using System.Linq.Expressions;
    using System.Numerics;
    using System.Windows.Forms;

    internal partial class CalculatorForm : Form
    {
        private readonly Display display = new Display();
        private int enterState;

        public CalculatorForm()
        {
            this.InitializeComponent();
            this.InputBox_TextChanged(this, null);
        }

        private void CalculatorForm_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                if (this.display.ExpressionText != null)
                {
                    var input = this.enterState == 0
                        ? "(" + this.display.ExpressionText + ")"
                        : this.display.ResultText;
                    this.inputBox.Text = input;
                    this.inputBox.Select(input.Length, 0);
                    this.enterState = (this.enterState + 1) % 2;
                    e.Handled = true;
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

        private void InputBox_TextChanged(object sender, System.EventArgs e)
        {
            this.display.SetInput(this.inputBox.Text);
            this.resultDisplay.Text = this.display.ResultText;
            this.expressionDisplay.Image = this.display.ExpressionImage;
        }

        private void ResultPanel_DoubleClick(object sender, EventArgs e)
        {
            this.FontDialog.Font = this.display.Font;
            if (this.FontDialog.ShowDialog() == DialogResult.OK)
            {
                this.display.Font = this.FontDialog.Font;
                this.expressionDisplay.Image = this.display.ExpressionImage;
            }
        }

        private class Display
        {
            private static readonly Bitmap EmptyDisplayImage = new Bitmap(1, 1);

            private readonly Parser parser = new Parser();

            private readonly ExpressionRenderer renderer = new ExpressionRenderer
            {
                Font = new Font("Calibri", 20, FontStyle.Regular),
            };

            private Expression expression;

            public Bitmap ExpressionImage { get; private set; }

            public string ExpressionText { get; private set; }

            public Font Font
            {
                get
                {
                    return this.renderer.Font;
                }

                set
                {
                    this.renderer.Font = value;
                    this.RenderExpression();
                }
            }

            public string ResultText { get; private set; }

            public void SetInput(string value)
            {
                this.expression = null;
                try
                {
                    this.expression = this.parser.Parse(value);
                }
                catch (Exception)
                {
                    this.ResultText = "?";
                    this.ExpressionImage = EmptyDisplayImage;
                    this.ExpressionText = null;
                    return;
                }

                try
                {
                    this.RenderExpression();
                }
                catch (Exception)
                {
                    this.ExpressionImage = EmptyDisplayImage;
                }

                try
                {
                    this.ExpressionText = this.expression.TransformToString();
                }
                catch (Exception)
                {
                    this.ExpressionText = null;
                }

                try
                {
                    var converted = Expression.Call(
                        typeof(ExpressionTransformers).GetMethod(nameof(ExpressionTransformers.TransformToString), new[] { typeof(Complex) }),
                        this.expression.Type == typeof(Complex)
                            ? this.expression
                            : Expression.Convert(this.expression, typeof(Complex)));

                    this.ResultText = ((Expression<Func<string>>)Expression.Lambda(converted)).Compile()();
                }
                catch (Exception)
                {
                    this.ResultText = "?";
                }
            }

            private void RenderExpression()
            {
                if (this.expression != null)
                {
                    this.ExpressionImage = this.renderer.RenderExpression(this.expression);
                }
            }
        }
    }
}
