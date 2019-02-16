// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser.Demo
{
    using System;
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
            else
            {
                this.enterState = 0;
            }
        }

        private void InputBox_TextChanged(object sender, System.EventArgs e)
        {
            this.display.SetInput(this.inputBox.Text);
            this.resultDisplay.Text = this.display.ResultText;
            this.expressionDisplay.Image = this.display.ExpressionImage;
        }

        private void KeyPad_KeyPress(object sender, KeyPressEventArgs e)
        {
            this.enterState = 0;

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
