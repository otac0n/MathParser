// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser.VisualNodes
{
    using System.Drawing;

    internal class StringVisualNode : VisualNode
    {
        public StringVisualNode(object value)
        {
            this.Value = value?.ToString() ?? string.Empty;
        }

        public string Value { get; }

        public override void Draw(Graphics g, Font font, Brush brush, PointF topLeft)
        {
            g.DrawString(this.Value, font, brush, topLeft);
        }

        public override SizeF Measure(Graphics g, Font font, out float baseline)
        {
            return MeasureString(g, this.Value, font, out baseline);
        }
    }
}
