// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser.Drawing.Tests
{
    using System;
    using System.Drawing;
    using System.IO;
    using System.Linq.Expressions;
    using System.Reflection;
    using MathParser.Testing;
    using NUnit.Framework;
    using static MathParser.Testing.TestExtensions;

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

        private static void ExpressionRendererTestHelper(string math)
        {
            var parser = new Parser();
            var expression = parser.Parse(math);
            ExpressionRendererTestHelper(expression);
        }

        private static void ExpressionRendererTestHelper(Expression expression)
        {
            using (var font = new Font("Calibri", 20, FontStyle.Regular))
            {
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
        }

        private static void WriteAndAssertResult(Bitmap bitmap)
        {
            var test = TestContext.CurrentContext.Test;
            var testPath = Path.Combine(test.ClassName, SanitizeName(test.Name) + ".png");
            var actualPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "ActualResults", testPath);
            var expectedPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "ExpectedResults", testPath);
            Directory.CreateDirectory(Path.GetDirectoryName(actualPath));
            bitmap.Save(actualPath);

            Assert.That(File.Exists(expectedPath), Is.True, () => $"A file matching '{actualPath}' is expected at '{expectedPath}'.");
            using (var expected = Image.FromFile(expectedPath))
            {
                Assert.That(bitmap.Width, Is.EqualTo(expected.Width));
                Assert.That(bitmap.Height, Is.EqualTo(expected.Height));
                var actualColors = bitmap.GetColors();
                var expectedColors = expected.GetColors();
                Assert.That(actualColors, Is.EqualTo(expectedColors));
            }
        }
    }
}
