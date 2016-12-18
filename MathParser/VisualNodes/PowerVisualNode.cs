// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser.VisualNodes
{
    using System.Drawing;

    internal class PowerVisualNode : VisualNode
    {
        private const float FontSizeRatio = 0.6F;

        public PowerVisualNode(VisualNode left, VisualNode right)
        {
            this.Left = left;
            this.Right = right;
        }

        public VisualNode Left { get; }

        public VisualNode Right { get; }

        public override void Draw(Graphics g, Font font, Brush brush, PointF topLeft)
        {
            var superFont = GetSuperFont(font);

            float leftBaseline, rightBaseline;
            var leftSize = this.Left.Measure(g, font, out leftBaseline);
            var rightSize = this.Right.Measure(g, superFont, out rightBaseline);

            var leftOffset = rightSize.Height - leftBaseline / 2;
            var leftLocation = new PointF(topLeft.X, topLeft.Y + (leftOffset > 0 ? leftOffset : 0));
            this.Left.Draw(g, font, brush, leftLocation);

            var rightOffset = leftBaseline / 2 - rightSize.Height;
            var rightLocation = new PointF(topLeft.X + leftSize.Width, topLeft.Y + (rightOffset > 0 ? rightOffset : 0));
            this.Right.Draw(g, superFont, brush, rightLocation);
        }

        public override SizeF Measure(Graphics g, Font font, out float baseline)
        {
            var superFont = GetSuperFont(font);

            float leftBaseline, rightBaseline;
            var leftSize = this.Left.Measure(g, font, out leftBaseline);
            var rightSize = this.Right.Measure(g, superFont, out rightBaseline);

            var leftOffset = rightSize.Height - leftBaseline / 2;

            leftSize.Width += rightSize.Width;
            if (leftOffset > 0)
            {
                leftSize.Height += leftOffset;
                leftBaseline += leftOffset;
            }

            baseline = leftBaseline;
            return leftSize;
        }

        private static Font GetSuperFont(Font font)
        {
            return new Font(font.FontFamily, font.Size * FontSizeRatio, font.Style, font.Unit, font.GdiCharSet, font.GdiVerticalFont);
        }
    }
}
