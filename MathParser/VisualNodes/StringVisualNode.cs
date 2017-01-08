// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser.VisualNodes
{
    using System;
    using System.Drawing;

    internal class StringVisualNode : VisualNode
    {
        public StringVisualNode(object value)
        {
            this.Value = value?.ToString() ?? string.Empty;
        }

        public string Value { get; }

        public override void Draw(Graphics graphics, Font font, Brush brush, PointF topLeft)
        {
            if (graphics == null)
            {
                throw new ArgumentNullException(nameof(graphics));
            }

            graphics.DrawString(this.Value, font, brush, topLeft);
        }

        public override SizeF Measure(Graphics graphics, Font font, out float baseline)
        {
            return MeasureString(graphics, this.Value, font, out baseline);
        }
    }
}
