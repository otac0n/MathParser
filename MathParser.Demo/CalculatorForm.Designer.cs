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
            var resources = new System.ComponentModel.ComponentResourceManager(typeof(CalculatorForm));
            this.inputBox = new System.Windows.Forms.TextBox();
            this.resultPanel = new System.Windows.Forms.Panel();
            this.expressionDisplay = new System.Windows.Forms.PictureBox();
            this.resultDisplay = new System.Windows.Forms.TextBox();
            this.FontDialog = new System.Windows.Forms.FontDialog();
            this.plotView = new OxyPlot.WindowsForms.PlotView();
            this.keyPadButton = new System.Windows.Forms.Button();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.resultPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)this.expressionDisplay).BeginInit();
            ((System.ComponentModel.ISupportInitialize)this.splitContainer1).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // inputBox
            // 
            resources.ApplyResources(this.inputBox, "inputBox");
            this.inputBox.Name = "inputBox";
            this.inputBox.TextChanged += this.InputBox_TextChanged;
            this.inputBox.PreviewKeyDown += this.Control_PreviewKeyDown;
            // 
            // resultPanel
            // 
            resources.ApplyResources(this.resultPanel, "resultPanel");
            this.resultPanel.Controls.Add(this.expressionDisplay);
            this.resultPanel.Name = "resultPanel";
            this.resultPanel.DoubleClick += this.ResultPanel_DoubleClick;
            // 
            // expressionDisplay
            // 
            resources.ApplyResources(this.expressionDisplay, "expressionDisplay");
            this.expressionDisplay.Name = "expressionDisplay";
            this.expressionDisplay.TabStop = false;
            this.expressionDisplay.DoubleClick += this.ResultPanel_DoubleClick;
            // 
            // resultDisplay
            // 
            this.resultDisplay.BackColor = System.Drawing.SystemColors.Window;
            this.resultDisplay.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.resultDisplay, "resultDisplay");
            this.resultDisplay.Name = "resultDisplay";
            this.resultDisplay.ReadOnly = true;
            this.resultDisplay.PreviewKeyDown += this.Control_PreviewKeyDown;
            // 
            // plotView
            // 
            resources.ApplyResources(this.plotView, "plotView");
            this.plotView.Name = "plotView";
            this.plotView.PanCursor = System.Windows.Forms.Cursors.Hand;
            this.plotView.ZoomHorizontalCursor = System.Windows.Forms.Cursors.SizeWE;
            this.plotView.ZoomRectangleCursor = System.Windows.Forms.Cursors.SizeNWSE;
            this.plotView.ZoomVerticalCursor = System.Windows.Forms.Cursors.SizeNS;
            // 
            // keyPadButton
            // 
            resources.ApplyResources(this.keyPadButton, "keyPadButton");
            this.keyPadButton.Name = "keyPadButton";
            this.keyPadButton.UseVisualStyleBackColor = true;
            // 
            // splitContainer1
            // 
            resources.ApplyResources(this.splitContainer1, "splitContainer1");
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.resultPanel);
            this.splitContainer1.Panel1.Controls.Add(this.resultDisplay);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.plotView);
            // 
            // CalculatorForm
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.keyPadButton);
            this.Controls.Add(this.inputBox);
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.Name = "CalculatorForm";
            this.ShowIcon = false;
            this.KeyPress += this.CalculatorForm_KeyPress;
            this.PreviewKeyDown += this.Control_PreviewKeyDown;
            this.resultPanel.ResumeLayout(false);
            this.resultPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)this.expressionDisplay).EndInit();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)this.splitContainer1).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.TextBox inputBox;
        private System.Windows.Forms.Panel resultPanel;
        private System.Windows.Forms.PictureBox expressionDisplay;
        private System.Windows.Forms.TextBox resultDisplay;
        private System.Windows.Forms.FontDialog FontDialog;
        private OxyPlot.WindowsForms.PlotView plotView;
        private System.Windows.Forms.Button keyPadButton;
        private System.Windows.Forms.SplitContainer splitContainer1;
    }
}

