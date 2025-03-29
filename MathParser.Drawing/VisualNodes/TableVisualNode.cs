// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser.Drawing.VisualNodes
{
    using System;
    using System.Drawing;
    using System.Linq;

    internal class TableVisualNode : VisualNode
    {
        public TableVisualNode(VisualNode[,] nodes)
        {
            this.Nodes = nodes;
        }

        public VisualNode[,] Nodes { get; }

        public override void Draw(Graphics graphics, Font font, Brush brush, Pen pen, PointF topLeft)
        {
            this.MeasureInternal(graphics, font, out var baseline, out var spacing, out var columnWidths, out var rowBaselines, out var rowHeights, out var baselines);

            var width = this.Nodes.GetLength(0);
            var height = this.Nodes.GetLength(1);

            var rowOffset = SizeF.Empty;
            for (var y = 0; y < height; y++)
            {
                var offset = SizeF.Empty;
                for (var x = 0; x < width; x++)
                {
                    offset.Height = rowBaselines[y] - baselines[y, x];
                    this.Nodes[y, x]?.Draw(graphics, font, brush, pen, topLeft + offset + rowOffset);
                    offset.Width += columnWidths[x] + spacing.Width;
                }

                rowOffset.Height += rowHeights[y] + spacing.Height;
            }
        }

        public override SizeF Measure(Graphics graphics, Font font, out float baseline)
        {
            return this.MeasureInternal(graphics, font, out baseline, out var spacing, out var columnWidths, out var rowHeights, out var rowBaselines, out var baselines);
        }

        private SizeF MeasureInternal(Graphics graphics, Font font, out float baseline, out SizeF spacing, out float[] columnWidths, out float[] rowHeights, out float[] rowBaselines, out float[,] baselines)
        {
            var width = this.Nodes.GetLength(0);
            var height = this.Nodes.GetLength(1);

            columnWidths = new float[width];
            rowHeights = new float[height];
            rowBaselines = new float[height];

            baselines = new float[width, height];
            var sizes = new SizeF[width, height];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var size = sizes[y, x] = this.Nodes[y, x]?.Measure(graphics, font, out baselines[y, x]) ?? SizeF.Empty;
                    columnWidths[x] = Math.Max(columnWidths[x], size.Width);
                    rowHeights[x] = Math.Max(rowBaselines[x], size.Height);
                    rowBaselines[y] = Math.Max(rowBaselines[y], baselines[y, x]);
                }
            }

            spacing = MeasureString(graphics, " ", font, out baseline);
            var totalSize = new SizeF(
                columnWidths.Sum() + (spacing.Width * (width - 1)),
                spacing.Height * (height - 1));

            for (var y = 0; y < height; y++)
            {
                var currentDiff = rowHeights[y] - rowBaselines[y];
                var maxOffset = 0F;
                for (var x = 0; x < width; x++)
                {
                    var offset = sizes[y, x].Height - baselines[y, x] - currentDiff;
                    if (offset > maxOffset)
                    {
                        maxOffset = offset;
                    }
                }

                if (maxOffset > 0)
                {
                    rowHeights[y] += maxOffset;
                }
            }

            totalSize.Height += rowHeights.Sum();
            baseline += totalSize.Height / 2;
            return totalSize;
        }
    }
}
