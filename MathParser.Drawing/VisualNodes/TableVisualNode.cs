// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser.Drawing.VisualNodes
{
    using System;
    using System.Drawing;
    using System.Linq;
    using System.Runtime.Versioning;

    [SupportedOSPlatform("windows")]
    internal class TableVisualNode : VisualNode
    {
        public TableVisualNode(VisualNode[,] nodes)
        {
            this.Nodes = nodes;
        }

        public VisualNode[,] Nodes { get; }

        public override void Draw(Graphics graphics, Font font, Brush brush, Pen pen, PointF topLeft)
        {
            this.MeasureInternal(graphics, font, out var baseline, out var spacing, out var columnWidths, out var rowHeights, out var rowBaselines, out var baselines);

            var rows = this.Nodes.GetLength(0);
            var columns = this.Nodes.GetLength(1);

            var rowOffset = SizeF.Empty;
            for (var row = 0; row < rows; row++)
            {
                var columnOffset = SizeF.Empty;
                for (var col = 0; col < columns; col++)
                {
                    columnOffset.Height = rowBaselines[row] - baselines[row, col];
                    this.Nodes[row, col]?.Draw(graphics, font, brush, pen, topLeft + columnOffset + rowOffset);
                    columnOffset.Width += columnWidths[col] + spacing.Width;
                }

                rowOffset.Height += rowHeights[row] + spacing.Height;
            }
        }

        public override SizeF Measure(Graphics graphics, Font font, out float baseline)
        {
            return this.MeasureInternal(graphics, font, out baseline, out var spacing, out var columnWidths, out var rowHeights, out var rowBaselines, out var baselines);
        }

        private SizeF MeasureInternal(Graphics graphics, Font font, out float baseline, out SizeF spacing, out float[] columnWidths, out float[] rowHeights, out float[] rowBaselines, out float[,] baselines)
        {
            var rows = this.Nodes.GetLength(0);
            var columns = this.Nodes.GetLength(1);

            columnWidths = new float[columns];
            rowHeights = new float[rows];
            rowBaselines = new float[rows];

            baselines = new float[rows, columns];
            var sizes = new SizeF[rows, columns];
            for (var row = 0; row < rows; row++)
            {
                for (var col = 0; col < columns; col++)
                {
                    var size = sizes[row, col] = this.Nodes[row, col]?.Measure(graphics, font, out baselines[row, col]) ?? SizeF.Empty;
                    columnWidths[col] = Math.Max(columnWidths[col], size.Width);
                    rowHeights[row] = Math.Max(rowHeights[row], size.Height);
                    rowBaselines[row] = Math.Max(rowBaselines[row], baselines[row, col]);
                }
            }

            spacing = MeasureString(graphics, " ", font, out baseline);
            var totalSize = new SizeF(
                columnWidths.Sum() + (spacing.Width * (columns - 1)),
                spacing.Height * (rows - 1));

            for (var row = 0; row < rows; row++)
            {
                var currentDiff = rowHeights[row] - rowBaselines[row];
                var maxOffset = 0F;
                for (var col = 0; col < columns; col++)
                {
                    var offset = sizes[row, col].Height - baselines[row, col] - currentDiff;
                    if (offset > maxOffset)
                    {
                        maxOffset = offset;
                    }
                }

                if (maxOffset > 0)
                {
                    rowHeights[row] += maxOffset;
                }
            }

            totalSize.Height += rowHeights.Sum();
            baseline += (totalSize.Height - spacing.Height) / 2;
            return totalSize;
        }
    }
}
