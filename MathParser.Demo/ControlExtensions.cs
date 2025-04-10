namespace MathParser.Demo
{
    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.Windows.Forms;

    internal static class ControlExtensions
    {
        /// <summary>
        /// Attach a <see cref="Control"/> to a button.
        /// </summary>
        /// <param name="button">The button that will activate the context menu.</param>
        /// <param name="makeContext">The function that constructs a context control.</param>
        /// <returns>A disposable that can be used to remove the context menu.</returns>
        public static IComponent AttachDropDownContext(this Button button, Func<Control> makeContext)
        {
            void Activate(Control sender)
            {
                var popup = new PopupForm();
                popup.Controls.Add(makeContext());
                popup.PopUnder(sender);
            }

            void Click(object sender, EventArgs e) => Activate((Control)sender);

            void PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
            {
                if (e.KeyCode == Keys.Down && e.Modifiers == Keys.None)
                {
                    e.IsInputKey = true;
                }
            }

            void KeyDown(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Down && e.Modifiers == Keys.None)
                {
                    e.Handled = true;
                    Activate((Control)sender);
                }
            }

            button.Click += Click;
            button.PreviewKeyDown += PreviewKeyDown;
            button.KeyDown += KeyDown;

            void Dispose()
            {
                button.Click -= Click;
                button.PreviewKeyDown -= PreviewKeyDown;
                button.KeyDown -= KeyDown;
            }

            return new ActionDisposableComponent(Dispose)
            {
                Site = button.Site,
            };
        }

        /// <summary>
        /// A utility to construct a control or other disposable, disposing of the control if an exception is thrown before completion.
        /// </summary>
        /// <typeparam name="TControl">The type of control being constructed.</typeparam>
        /// <param name="update">The actions atomically performed after the construction of the control.</param>
        /// <returns>The constructed control.</returns>
        public static TControl Construct<TControl>(Action<TControl> update)
            where TControl : class, IDisposable, new() =>
                Construct(() => new TControl(), update);

        /// <summary>
        /// A utility to construct a control or other disposable, disposing of the control if an exception is thrown before completion.
        /// </summary>
        /// <typeparam name="TControl">The type of control being constructed.</typeparam>
        /// <param name="constructor">The parameterless function to construct the control.</param>
        /// <param name="update">The actions atomically performed after the construction of the control.</param>
        /// <returns>The constructed control.</returns>
        public static TControl Construct<TControl>(Func<TControl> constructor, Action<TControl> update)
            where TControl : class, IDisposable
        {
            TControl control = default;
            try
            {
                control = constructor();
                update(control);
                var result = control;
                control = null;
                return result;
            }
            finally
            {
                control?.Dispose();
            }
        }

        /// <summary>
        /// Show a form under a control.
        /// </summary>
        /// <param name="contextForm">The form to show in context.</param>
        /// <param name="control">The control under which the form will be shown.</param>
        public static void PopUnder(this Form contextForm, Control control)
        {
            var offset = new Point(0, control.Height);
            var screenPos = control.PointToScreen(offset);

            contextForm.Location = screenPos;
            contextForm.Show(control.FindForm());
        }

        private class ActionDisposable(Action dispose) : IDisposable
        {
            public event EventHandler Disposed;

            public void Dispose()
            {
                dispose();
                this.Disposed?.Invoke(this, EventArgs.Empty);
            }
        }

        private class ActionDisposableComponent(Action action) : ActionDisposable(action), IComponent
        {
            public ISite Site { get; set; }
        }


        public class PopupForm : Form
        {
            public PopupForm()
            {
                this.FormBorderStyle = FormBorderStyle.None;
                this.ShowInTaskbar = false;
                this.StartPosition = FormStartPosition.Manual;
                this.TopMost = true;
                this.AutoSize = true;
                this.AutoSizeMode = AutoSizeMode.GrowAndShrink;

                this.Deactivate += (s, e) => this.Close();
            }

            protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
            {
                if (keyData == Keys.Escape)
                {
                    this.Close();
                    return true;
                }

                return base.ProcessCmdKey(ref msg, keyData);
            }
        }
    }
}
