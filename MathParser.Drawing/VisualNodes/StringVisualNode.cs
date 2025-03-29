// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser.Drawing.VisualNodes
{
    using System;
    using System.Drawing;
    using System.Runtime.Versioning;

    [SupportedOSPlatform("windows")]
    internal class StringVisualNode : VisualNode
    {
        public StringVisualNode(object value)
        {
            this.Value = value?.ToString() ?? string.Empty;
        }

        public string Value { get; }

        public override void Draw(Graphics graphics, Font font, Brush brush, Pen pen, PointF topLeft)
        {
            ArgumentNullException.ThrowIfNull(graphics);

            DrawString(graphics, this.Value, font, brush, pen, topLeft);
        }

        public override SizeF Measure(Graphics graphics, Font font, out float baseline)
        {
            return MeasureString(graphics, this.Value, font, out baseline);
        }
    }
}
