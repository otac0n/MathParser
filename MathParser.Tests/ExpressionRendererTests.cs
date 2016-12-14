// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser.Tests
{
    using System;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Drawing.Text;
    using System.IO;
    using System.Reflection;
    using NUnit.Framework;

    [TestFixture]
    public class ExpressionRendererTests
    {
        [Test]
        public void MeasureAndDrawExpression_WhenGivenAConstantExpression_ReturnExpectedValues()
        {
            using (var scope = new TestScope(50, 40))
            {
                var expression = scope.Parser.Parse("1.1");

                float baseline;
                var size = scope.Renderer.Measure(scope.Graphics, expression, out baseline);

                scope.Renderer.DrawExpression(scope.Graphics, expression, PointF.Empty);
                scope.HighlightBaseline(size, baseline);
                scope.WriteAndAssertResult();
            }
        }

        [Test]
        public void MeasureAndDrawExpression_WhenGivenADivisionExpression_ReturnExpectedValues()
        {
            using (var scope = new TestScope(100, 40))
            {
                var expression = scope.Parser.Parse("10/2");

                float baseline;
                var size = scope.Renderer.Measure(scope.Graphics, expression, out baseline);

                scope.Renderer.DrawExpression(scope.Graphics, expression, PointF.Empty);
                scope.HighlightBaseline(size, baseline);
                scope.WriteAndAssertResult();
            }
        }

        [Test]
        public void MeasureAndDrawExpression_WhenGivenAExpressionNeedingBrackets_ReturnExpectedValues()
        {
            using (var scope = new TestScope(250, 60))
            {
                var expression = scope.Parser.Parse("(1+2^3^4)^(5*(6+7))");

                float baseline;
                var size = scope.Renderer.Measure(scope.Graphics, expression, out baseline);

                scope.Renderer.DrawExpression(scope.Graphics, expression, PointF.Empty);
                scope.HighlightBaseline(size, baseline);
                scope.WriteAndAssertResult();
            }
        }

        [Test]
        public void MeasureAndDrawExpression_WhenGivenAExpressionNeedingBrackets2_ReturnExpectedValues()
        {
            using (var scope = new TestScope(220, 50))
            {
                var expression = scope.Parser.Parse("((1+2)^(3+4))^5");

                float baseline;
                var size = scope.Renderer.Measure(scope.Graphics, expression, out baseline);

                scope.Renderer.DrawExpression(scope.Graphics, expression, PointF.Empty);
                scope.HighlightBaseline(size, baseline);
                scope.WriteAndAssertResult();
            }
        }

        [Test]
        public void MeasureAndDrawExpression_WhenGivenAExpressionNeedingBrackets3_ReturnExpectedValues()
        {
            using (var scope = new TestScope(220, 40))
            {
                var expression = scope.Parser.Parse("1+2/(2*4)");

                float baseline;
                var size = scope.Renderer.Measure(scope.Graphics, expression, out baseline);

                scope.Renderer.DrawExpression(scope.Graphics, expression, PointF.Empty);
                scope.HighlightBaseline(size, baseline);
                scope.WriteAndAssertResult();
            }
        }

        [Test]
        public void MeasureAndDrawExpression_WhenGivenAKnownConstant_ReturnExpectedValues()
        {
            using (var scope = new TestScope(80, 40))
            {
                var expression = scope.Parser.Parse("τ+π");

                float baseline;
                var size = scope.Renderer.Measure(scope.Graphics, expression, out baseline);

                scope.Renderer.DrawExpression(scope.Graphics, expression, PointF.Empty);
                scope.HighlightBaseline(size, baseline);
                scope.WriteAndAssertResult();
            }
        }

        [Test]
        public void MeasureAndDrawExpression_WhenGivenAMultiplicationExpression_ReturnExpectedValues()
        {
            using (var scope = new TestScope(80, 40))
            {
                var expression = scope.Parser.Parse("3*5");

                float baseline;
                var size = scope.Renderer.Measure(scope.Graphics, expression, out baseline);

                scope.Renderer.DrawExpression(scope.Graphics, expression, PointF.Empty);
                scope.HighlightBaseline(size, baseline);
                scope.WriteAndAssertResult();
            }
        }

        [Test]
        public void MeasureAndDrawExpression_WhenGivenAnAdditionExpression_ReturnExpectedValues()
        {
            using (var scope = new TestScope(80, 40))
            {
                var expression = scope.Parser.Parse("1+1");

                float baseline;
                var size = scope.Renderer.Measure(scope.Graphics, expression, out baseline);

                scope.Renderer.DrawExpression(scope.Graphics, expression, PointF.Empty);
                scope.HighlightBaseline(size, baseline);
                scope.WriteAndAssertResult();
            }
        }

        [Test]
        public void MeasureAndDrawExpression_WhenGivenAPowerExpression_ReturnExpectedValues()
        {
            using (var scope = new TestScope(50, 50))
            {
                var expression = scope.Parser.Parse("2^5");

                float baseline;
                var size = scope.Renderer.Measure(scope.Graphics, expression, out baseline);

                scope.Renderer.DrawExpression(scope.Graphics, expression, PointF.Empty);
                scope.HighlightBaseline(size, baseline);
                scope.WriteAndAssertResult();
            }
        }

        [Test]
        public void MeasureAndDrawExpression_WhenGivenAPowerTower_ReturnExpectedValues()
        {
            using (var scope = new TestScope(60, 60))
            {
                var expression = scope.Parser.Parse("2^2^2^2");

                float baseline;
                var size = scope.Renderer.Measure(scope.Graphics, expression, out baseline);

                scope.Renderer.DrawExpression(scope.Graphics, expression, PointF.Empty);
                scope.HighlightBaseline(size, baseline);
                scope.WriteAndAssertResult();
            }
        }

        [Test]
        public void MeasureAndDrawExpression_WhenGivenASubtractionExpression_ReturnExpectedValues()
        {
            using (var scope = new TestScope(80, 40))
            {
                var expression = scope.Parser.Parse("3-8");

                float baseline;
                var size = scope.Renderer.Measure(scope.Graphics, expression, out baseline);

                scope.Renderer.DrawExpression(scope.Graphics, expression, PointF.Empty);
                scope.HighlightBaseline(size, baseline);
                scope.WriteAndAssertResult();
            }
        }

        private class TestScope : IDisposable
        {
            private Bitmap bitmap;
            private Brush[] highlightBrushes;
            private int highlightIndex;
            private Pen[] highlightPens;

            public TestScope()
                : this(10, 10)
            {
            }

            public TestScope(int width, int height)
            {
                this.bitmap = new Bitmap(width, height);
                this.Graphics = Graphics.FromImage(this.bitmap);
                this.Graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                this.Renderer = new ExpressionRenderer()
                {
                    Font = new Font("Calibri", 20, FontStyle.Regular),
                    Brush = Brushes.Black,
                };
                this.Parser = new Parser();

                var fillColor = Color.FromArgb(127, Color.White);

                this.highlightIndex = 0;
                this.highlightPens = new[]
                {
                    new Pen(Color.FromArgb(127, Color.Salmon)),
                    new Pen(Color.FromArgb(127, Color.DodgerBlue)),
                    new Pen(Color.FromArgb(127, Color.ForestGreen)),
                };
                this.highlightBrushes = new[]
                {
                    new HatchBrush(HatchStyle.ForwardDiagonal, Color.FromArgb(127, Color.Salmon), fillColor),
                    new HatchBrush(HatchStyle.BackwardDiagonal, Color.FromArgb(127, Color.DodgerBlue), fillColor),
                    new HatchBrush(HatchStyle.LargeGrid, Color.FromArgb(127, Color.ForestGreen), fillColor),
                };
            }

            public Graphics Graphics { get; }

            public Parser Parser { get; }

            public ExpressionRenderer Renderer { get; }

            public void Dispose()
            {
                this.Graphics.Dispose();
                this.bitmap.Dispose();
                Array.ForEach(this.highlightPens, p => p.Dispose());
                Array.ForEach(this.highlightBrushes, p => p.Dispose());
            }

            public void Highlight(Rectangle rectangle)
            {
                var i = this.highlightIndex;
                this.Graphics.FillRectangle(this.highlightBrushes[i], rectangle);
                this.Graphics.DrawRectangle(this.highlightPens[i], rectangle);
                this.highlightIndex = (i + 1) % this.highlightPens.Length;
            }

            public void HighlightBaseline(SizeF size, float baseline)
            {
                var w = (int)Math.Round(size.Width);
                var a = (int)Math.Round(baseline);
                var h = (int)Math.Round(size.Height);
                this.Highlight(new Rectangle(new Point(0, 0), new Size(w, a)));
                this.Highlight(new Rectangle(new Point(0, a + 1), new Size(w, h - a - 1)));
            }

            public void WriteAndAssertResult()
            {
                var test = TestContext.CurrentContext.Test;
                var testPath = Path.Combine(test.ClassName, test.MethodName + ".png");
                var actualPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "ActualResults", testPath);
                var expectedPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "ExpectedResults", testPath);
                Directory.CreateDirectory(Path.GetDirectoryName(actualPath));
                this.bitmap.Save(actualPath);

                Assert.That(File.Exists(expectedPath), Is.True);
                using (var expected = Image.FromFile(expectedPath))
                {
                    Assert.That(this.bitmap.Width, Is.EqualTo(expected.Width));
                    Assert.That(this.bitmap.Height, Is.EqualTo(expected.Height));
                    var actualColors = this.bitmap.GetColors();
                    var expectedColors = expected.GetColors();
                    Assert.That(actualColors, Is.EqualTo(expectedColors));
                }
            }
        }
    }
}
