namespace MathParser.Demo
{
    using System;
    using System.Windows.Forms;

    public partial class KeyPad : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KeyPad"/> class.
        /// </summary>
        public KeyPad()
        {
            this.InitializeComponent();
        }

        private void Key_Click(object sender, EventArgs e)
        {
            var button = (Button)sender;
            this.OnKeyPress(new KeyPressEventArgs(button.Text[0]));
        }

        private void Key_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar != ' ' && e.KeyChar != (char)Keys.Enter)
            {
                this.OnKeyPress(e);
            }
        }

        private void Key_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            this.OnPreviewKeyDown(e);
        }
    }
}
