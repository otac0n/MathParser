// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser.Drawing.Tests
{
    using System;
    using System.Drawing;
    using System.Linq.Expressions;
    using MathParser.Testing;
    using NUnit.Framework;

    [TestFixture]
    public class ExpressionRendererTests
    {

        [TestCaseSource(typeof(TestData), nameof(TestData.ExpressionStrings))]
        public void MeasureAndDrawExpression_ApprovalTest(string input)
        {
            ExpressionRendererTestHelper(input);
        }

        [TestCaseSource(typeof(TestData), nameof(TestData.ComplexExpressions))]
        public void MeasureAndDrawExpression_ApprovalTest(LambdaExpression lambda)
        {
            ExpressionRendererTestHelper(lambda.Body);
        }

        [TestCaseSource(typeof(TestData), nameof(TestData.LambdaExpressions))]
        public void MeasureAndDrawExpression_ApprovalTest(Expression lambda)
        {
            ExpressionRendererTestHelper(lambda);
        }

        private static void ExpressionRendererTestHelper(string math)
        {
            var parser = new Parser();
            var expression = parser.Parse(math);
            ExpressionRendererTestHelper(expression);
        }

        private static void ExpressionRendererTestHelper(Expression expression)
        {
            using var font = new Font("Calibri", 20, FontStyle.Regular);
            var renderer = new ExpressionRenderer()
            {
                Font = font,
                Brush = Brushes.Black,
            };

            SizeF size;
            float baseline;

            using (var bitmap = new Bitmap(1, 1))
            using (var graphics = ExpressionRenderer.CreateDefaultGraphics(bitmap))
            {
                size = renderer.Measure(graphics, expression, out baseline);
            }

            const float Padding = 5;
            using (var bitmap = new Bitmap((int)Math.Ceiling(size.Width + Padding * 2), (int)Math.Ceiling(size.Height + Padding * 2)))
            using (var graphics = ExpressionRenderer.CreateDefaultGraphics(bitmap))
            {
                renderer.DrawExpression(graphics, expression, new PointF(Padding, Padding));

                var w = (int)Math.Round(size.Width);
                var a = (int)Math.Round(baseline);
                var h = (int)Math.Round(size.Height);
                var highlighter = new ImageUtils.Highlighter();
                highlighter.Highlight(graphics, new RectangleF(new PointF(Padding, Padding), new SizeF(w, a)));
                highlighter.Highlight(graphics, new RectangleF(new PointF(Padding, Padding + a + 1), new SizeF(w, h - a - 1)));

                WriteAndAssertResult(bitmap);
            }
        }

        private static void WriteAndAssertResult(Bitmap bitmap)
        {
            TestContext.CurrentContext.ApproveFromFile(bitmap, ".png", (path, b) => b.Save(path), Image.FromFile, (expected, actual) =>
            {
                Assert.That(bitmap.Width, Is.EqualTo(expected.Width));
                Assert.That(bitmap.Height, Is.EqualTo(expected.Height));
                var actualColors = bitmap.GetColors();
                var expectedColors = expected.GetColors();
                Assert.That(actualColors, Is.EqualTo(expectedColors));
            });
        }
    }
}
