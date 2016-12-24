namespace MathParser.Demo
{
    partial class CalculatorForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.inputBox = new System.Windows.Forms.TextBox();
            this.resultPanel = new System.Windows.Forms.Panel();
            this.expressionDisplay = new System.Windows.Forms.PictureBox();
            this.resultDisplay = new System.Windows.Forms.TextBox();
            this.resultPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.expressionDisplay)).BeginInit();
            this.SuspendLayout();
            // 
            // inputBox
            // 
            this.inputBox.Dock = System.Windows.Forms.DockStyle.Top;
            this.inputBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.inputBox.Location = new System.Drawing.Point(12, 50);
            this.inputBox.Margin = new System.Windows.Forms.Padding(0);
            this.inputBox.Name = "inputBox";
            this.inputBox.Size = new System.Drawing.Size(463, 26);
            this.inputBox.TabIndex = 0;
            this.inputBox.TextChanged += new System.EventHandler(this.InputBox_TextChanged);
            // 
            // resultPanel
            // 
            this.resultPanel.AutoScroll = true;
            this.resultPanel.Controls.Add(this.expressionDisplay);
            this.resultPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.resultPanel.Location = new System.Drawing.Point(12, 76);
            this.resultPanel.Name = "resultPanel";
            this.resultPanel.Size = new System.Drawing.Size(463, 75);
            this.resultPanel.TabIndex = 1;
            // 
            // expressionDisplay
            // 
            this.expressionDisplay.Location = new System.Drawing.Point(0, 0);
            this.expressionDisplay.Name = "expressionDisplay";
            this.expressionDisplay.Size = new System.Drawing.Size(377, 50);
            this.expressionDisplay.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.expressionDisplay.TabIndex = 0;
            this.expressionDisplay.TabStop = false;
            // 
            // resultDisplay
            // 
            this.resultDisplay.BackColor = System.Drawing.SystemColors.Window;
            this.resultDisplay.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.resultDisplay.Dock = System.Windows.Forms.DockStyle.Top;
            this.resultDisplay.Font = new System.Drawing.Font("Microsoft Sans Serif", 20F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.resultDisplay.Location = new System.Drawing.Point(12, 12);
            this.resultDisplay.Name = "resultDisplay";
            this.resultDisplay.ReadOnly = true;
            this.resultDisplay.Size = new System.Drawing.Size(463, 38);
            this.resultDisplay.TabIndex = 1;
            this.resultDisplay.Text = "?";
            this.resultDisplay.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.resultDisplay.WordWrap = false;
            // 
            // CalculatorForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(487, 163);
            this.Controls.Add(this.resultPanel);
            this.Controls.Add(this.inputBox);
            this.Controls.Add(this.resultDisplay);
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.Name = "CalculatorForm";
            this.Padding = new System.Windows.Forms.Padding(12);
            this.ShowIcon = false;
            this.Text = "Calculator";
            this.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.CalculatorForm_KeyPress);
            this.resultPanel.ResumeLayout(false);
            this.resultPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.expressionDisplay)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox inputBox;
        private System.Windows.Forms.Panel resultPanel;
        private System.Windows.Forms.PictureBox expressionDisplay;
        private System.Windows.Forms.TextBox resultDisplay;
    }
}

