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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CalculatorForm));
            this.inputBox = new System.Windows.Forms.TextBox();
            this.resultPanel = new System.Windows.Forms.Panel();
            this.expressionDisplay = new System.Windows.Forms.PictureBox();
            this.resultDisplay = new System.Windows.Forms.TextBox();
            this.FontDialog = new System.Windows.Forms.FontDialog();
            this.keyPad = new MathParser.Demo.KeyPad();
            this.resultPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.expressionDisplay)).BeginInit();
            this.SuspendLayout();
            // 
            // inputBox
            // 
            resources.ApplyResources(this.inputBox, "inputBox");
            this.inputBox.Name = "inputBox";
            this.inputBox.TextChanged += new System.EventHandler(this.InputBox_TextChanged);
            this.inputBox.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.Control_PreviewKeyDown);
            // 
            // resultPanel
            // 
            resources.ApplyResources(this.resultPanel, "resultPanel");
            this.resultPanel.Controls.Add(this.expressionDisplay);
            this.resultPanel.Name = "resultPanel";
            this.resultPanel.DoubleClick += new System.EventHandler(this.ResultPanel_DoubleClick);
            // 
            // expressionDisplay
            // 
            resources.ApplyResources(this.expressionDisplay, "expressionDisplay");
            this.expressionDisplay.Name = "expressionDisplay";
            this.expressionDisplay.TabStop = false;
            this.expressionDisplay.DoubleClick += new System.EventHandler(this.ResultPanel_DoubleClick);
            // 
            // resultDisplay
            // 
            this.resultDisplay.BackColor = System.Drawing.SystemColors.Window;
            this.resultDisplay.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.resultDisplay, "resultDisplay");
            this.resultDisplay.Name = "resultDisplay";
            this.resultDisplay.ReadOnly = true;
            this.resultDisplay.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.Control_PreviewKeyDown);
            // 
            // keyPad
            // 
            resources.ApplyResources(this.keyPad, "keyPad");
            this.keyPad.Name = "keyPad";
            this.keyPad.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.KeyPad_KeyPress);
            this.keyPad.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.Control_PreviewKeyDown);
            // 
            // CalculatorForm
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.resultPanel);
            this.Controls.Add(this.keyPad);
            this.Controls.Add(this.resultDisplay);
            this.Controls.Add(this.inputBox);
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.Name = "CalculatorForm";
            this.ShowIcon = false;
            this.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.CalculatorForm_KeyPress);
            this.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.Control_PreviewKeyDown);
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
        private System.Windows.Forms.FontDialog FontDialog;
        private KeyPad keyPad;
    }
}

