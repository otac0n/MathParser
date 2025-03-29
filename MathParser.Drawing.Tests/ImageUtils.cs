// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser.Drawing.Tests
{
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Drawing.Imaging;
    using System.Runtime.InteropServices;
    using System.Threading;

    internal static class ImageUtils
    {
        public static Bitmap Copy(this Image image)
        {
            Bitmap copy = null;
            try
            {
                copy = new Bitmap(image.Width, image.Height);
                using (var g = Graphics.FromImage(copy))
                {
                    g.DrawImage(image, 0, 0, image.Width, image.Height);
                }

                var result = copy;
                copy = null;
                return result;
            }
            finally
            {
                if (copy != null)
                {
                    copy.Dispose();
                }
            }
        }

        public static Color[,] GetColors(this Image image)
        {
            var bitmap = image as Bitmap;
            if (bitmap != null)
            {
                return bitmap.GetColors();
            }

            using var copy = image.Copy();
            return copy.GetColors();
        }

        public static Color[,] GetColors(this Bitmap bitmap)
        {
            var size = bitmap.Size;
            var colors = new Color[size.Width, size.Height];
            var data = bitmap.LockBits(new Rectangle(Point.Empty, size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            for (var y = 0; y < data.Height; y++)
            {
                var row = new int[data.Width];
                Marshal.Copy(data.Scan0 + y * data.Stride, row, 0, data.Width);
                for (var x = 0; x < data.Width; x++)
                {
                    colors[x, y] = Color.FromArgb(row[x]);
                }
            }

            bitmap.UnlockBits(data);
            return colors;
        }

        public class Highlighter
        {
            private static readonly HatchBrush[] HighlightBrushes = new[]
            {
                new HatchBrush(HatchStyle.ForwardDiagonal, Color.FromArgb(127, Color.Salmon), Color.FromArgb(64, Color.White)),
                new HatchBrush(HatchStyle.BackwardDiagonal, Color.FromArgb(127, Color.DodgerBlue), Color.FromArgb(64, Color.White)),
                new HatchBrush(HatchStyle.LargeGrid, Color.FromArgb(127, Color.ForestGreen), Color.FromArgb(64, Color.White)),
            };

            private static readonly Pen[] HighlightPens = new[]
            {
                new Pen(Color.FromArgb(127, Color.Salmon)),
                new Pen(Color.FromArgb(127, Color.DodgerBlue)),
                new Pen(Color.FromArgb(127, Color.ForestGreen)),
            };

            private int highlightIndex = -1;

            public void Highlight(Graphics graphics, RectangleF rectangle)
            {
                var i = Interlocked.Increment(ref this.highlightIndex) % HighlightPens.Length;
                graphics.FillRectangle(HighlightBrushes[i], rectangle);
                graphics.DrawRectangle(HighlightPens[i], rectangle.Left, rectangle.Top, rectangle.Width, rectangle.Height);
            }
        }
    }
}
