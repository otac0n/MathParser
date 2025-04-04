// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser.Demo
{
    using System;
    using System.Linq.Expressions;
    using System.Numerics;
    using System.Windows.Forms;
    using OxyPlot;
    using OxyPlot.Series;

    internal partial class CalculatorForm : Form
    {
        private readonly Parser parser = new Parser(DefaultScope.Instance);
        private readonly Display display = new Display(DefaultScope.Instance);
        private Expression expression;

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
                    var input = this.display.ResultText;
                    this.inputBox.Text = input;
                    this.inputBox.Select(input.Length, 0);
                    e.Handled = true;
                }

                return;
            }
            else if (e.KeyChar == (char)Keys.Escape)
            {
                this.inputBox.Text = string.Empty;
                e.Handled = true;
            }
        }

        private void Control_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.IsInputKey = true;
            }
        }

        private void InputBox_TextChanged(object sender, System.EventArgs e)
        {
            this.expression = null;
            try
            {
                this.expression = this.parser.Parse(this.inputBox.Text);
                this.display.SetInput(this.expression);
            }
            catch (Exception)
            {
                this.display.SetInput(null);
            }

            this.resultDisplay.Text = this.display.ResultText;
            this.expressionDisplay.Image = this.display.ExpressionImage;

            this.plotView.Model = null;
            if (this.expression != null)
            {
                if (this.expression is LambdaExpression lambda && lambda.Parameters.Count == 1)
                {
                    Func<double, double> plot = null;

                    try
                    {
                        var compiled = lambda.Compile();

                        if (lambda.Body.Type == typeof(Complex))
                        {
                            if (lambda.Parameters[0].Type == typeof(Complex))
                            {
                                var concrete = (Func<Complex, Complex>)compiled;
                                plot = x => concrete(x).Real;
                            }
                            else if (lambda.Parameters[0].Type == typeof(double))
                            {
                                var concrete = (Func<double, Complex>)compiled;
                                plot = x => concrete(x).Real;
                            }
                        }
                        else if (lambda.Body.Type == typeof(double))
                        {
                            if (lambda.Parameters[0].Type == typeof(Complex))
                            {
                                var concrete = (Func<Complex, double>)compiled;
                                plot = x => concrete(x);
                            }
                            else if (lambda.Parameters[0].Type == typeof(double))
                            {
                                plot = (Func<double, double>)compiled;
                            }
                        }
                    }
                    catch (Exception)
                    {
                    }

                    if (plot != null)
                    {
                        var model = new PlotModel();
                        model.Series.Add(new FunctionSeries(plot, -10, 10, 0.01, this.display.ExpressionText));
                        this.plotView.Model = model;
                    }
                }
            }
        }

        private void KeyPad_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Back)
            {
                if (this.inputBox.SelectionLength != 0)
                {
                    this.inputBox.SelectedText = string.Empty;
                }
                else
                {
                    var start = this.inputBox.SelectionStart;
                    if (start > 0)
                    {
                        var newStart = start - 1;
                        var text = this.inputBox.Text;
                        this.inputBox.Text = text.Substring(0, newStart) + text.Substring(start);
                        this.inputBox.SelectionStart = newStart;
                    }
                }
            }
            else if (!char.IsControl(e.KeyChar))
            {
                var value = e.KeyChar.ToString();
                if (this.inputBox.SelectionStart == this.inputBox.TextLength)
                {
                    this.inputBox.AppendText(value);
                }
                else
                {
                    this.inputBox.SelectedText = value;
                }
            }
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
    }
}
