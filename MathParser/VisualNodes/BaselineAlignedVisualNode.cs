// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser.VisualNodes
{
    using System.Drawing;
    using System.Linq;

    internal class BaselineAlignedVisualNode : VisualNode
    {
        public BaselineAlignedVisualNode(params VisualNode[] nodes)
        {
            this.Nodes = nodes;
        }

        public VisualNode[] Nodes { get; }

        public override void Draw(Graphics g, Font font, Brush brush, PointF topLeft)
        {
            float[] baselines;
            SizeF[] sizes;
            float baseline;
            this.MeasureInternal(g, font, out baseline, out sizes, out baselines);

            var offset = SizeF.Empty;
            var nodes = this.Nodes.Length;
            for (int i = 0; i < nodes; i++)
            {
                offset.Height = baseline - baselines[i];
                this.Nodes[i].Draw(g, font, brush, topLeft + offset);
                offset.Width += sizes[i].Width;
            }
        }

        public override SizeF Measure(Graphics g, Font font, out float baseline)
        {
            float[] baselines;
            SizeF[] sizes;
            return this.MeasureInternal(g, font, out baseline, out sizes, out baselines);
        }

        private SizeF MeasureInternal(Graphics g, Font font, out float baseline, out SizeF[] sizes, out float[] baselines)
        {
            var nodes = this.Nodes.Length;

            baselines = new float[nodes];
            sizes = new SizeF[nodes];
            for (int i = 0; i < nodes; i++)
            {
                sizes[i] = this.Nodes[i].Measure(g, font, out baselines[i]);
            }

            var currentBaseline = baselines.Max();
            var currentSize = new SizeF(
                sizes.Sum(s => s.Width),
                sizes.Max(s => s.Height));

            var currentDiff = currentSize.Height - currentBaseline;
            var maxOffset = 0F;
            for (int i = 0; i < nodes; i++)
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
