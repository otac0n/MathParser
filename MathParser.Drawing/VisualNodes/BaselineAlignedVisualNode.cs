// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser.Drawing.VisualNodes
{
    using System.Drawing;
    using System.Linq;
    using System.Runtime.Versioning;

    [SupportedOSPlatform("windows")]
    internal class BaselineAlignedVisualNode(params VisualNode[] nodes) : VisualNode
    {
        public VisualNode[] Nodes { get; } = nodes;

        public override void Draw(Graphics graphics, Font font, Brush brush, Pen pen, PointF topLeft)
        {
            this.MeasureInternal(graphics, font, out var baseline, out var sizes, out var baselines);

            var offset = SizeF.Empty;
            var nodes = this.Nodes.Length;
            for (var i = 0; i < nodes; i++)
            {
                offset.Height = baseline - baselines[i];
                this.Nodes[i].Draw(graphics, font, brush, pen, topLeft + offset);
                offset.Width += sizes[i].Width;
            }
        }

        public override SizeF Measure(Graphics graphics, Font font, out float baseline)
        {
            return this.MeasureInternal(graphics, font, out baseline, out _, out _);
        }

        private SizeF MeasureInternal(Graphics graphics, Font font, out float baseline, out SizeF[] sizes, out float[] baselines)
        {
            var nodes = this.Nodes.Length;

            baselines = new float[nodes];
            sizes = new SizeF[nodes];
            for (var i = 0; i < nodes; i++)
            {
                sizes[i] = this.Nodes[i].Measure(graphics, font, out baselines[i]);
            }

            var currentBaseline = baselines.Max();
            var currentSize = new SizeF(
                sizes.Sum(s => s.Width),
                sizes.Max(s => s.Height));

            var currentDiff = currentSize.Height - currentBaseline;
            var maxOffset = 0F;
            for (var i = 0; i < nodes; i++)
            {
                var offset = sizes[i].Height - baselines[i] - currentDiff;
                if (offset > maxOffset)
                {
                    maxOffset = offset;
                }
            }

            if (maxOffset > 0)
            {
                currentSize.Height += maxOffset;
            }

            baseline = currentBaseline;
            return currentSize;
        }
    }
}
