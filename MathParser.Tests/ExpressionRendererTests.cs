// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser.Tests
{
    using System;
    using System.Drawing;
    using System.Drawing.Text;
    using System.IO;
    using System.Linq.Expressions;
    using System.Numerics;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using NUnit.Framework;

    [TestFixture]
    public class ExpressionRendererTests
    {
        public static Expression<Func<Complex>>[] ExpressionTestCases => new Expression<Func<Complex>>[]
        {
            () => new Complex(1, 3) / 4,
            () => new Complex(1, 0) / 4,
            () => new Complex(0, 3) / 4,
            () => new Complex(-2, 3) / 4,
            () => new Complex(-2, -3) / 4,
            () => new Complex(-2, 0) / 4,
            () => new Complex(0, -2) / 4,
            () => new Complex(1, 3) * 4,
            () => new Complex(1, 0) * 4,
            () => new Complex(0, 3) * 4,
            () => new Complex(-2, 3) * 4,
            () => new Complex(-2, -3) * 4,
            () => new Complex(-2, 0) * 4,
            () => new Complex(0, -2) * 4,
            () => new Complex(1, 3) + 4,
            () => new Complex(1, 0) + 4,
            () => new Complex(0, 3) + 4,
            () => new Complex(-2, 3) + 4,
            () => new Complex(-2, -3) + 4,
            () => new Complex(-2, 0) + 4,
            () => new Complex(0, -2) + 4,
            () => new Complex(1, 3) - 4,
            () => new Complex(1, 0) - 4,
            () => new Complex(0, 3) - 4,
            () => new Complex(-2, 3) - 4,
            () => new Complex(-2, -3) - 4,
            () => new Complex(-2, 0) - 4,
            () => new Complex(0, -2) - 4,
            () => Complex.Pow(new Complex(1, 3), 4),
            () => Complex.Pow(new Complex(1, 0), 4),
            () => Complex.Pow(new Complex(0, 3), 4),
            () => Complex.Pow(new Complex(-2, 3), 4),
            () => Complex.Pow(new Complex(-2, -3), 4),
            () => Complex.Pow(new Complex(-2, 0), 4),
            () => Complex.Pow(new Complex(0, -2), 4),
            () => Complex.Pow(1, Complex.Pow(Complex.ImaginaryOne, 3)),
            () => Complex.Pow(-1, -1 * 2),
            () => Complex.Pow(5, Complex.Divide(1, 2)),
            () => Complex.Pow(5, Complex.ImaginaryOne * 2),
        };

        [TestCase("τ+π")]
        [TestCase("i")]
        [TestCase("e")]
        [TestCase("φ")]
        [TestCase("∞")]
        [TestCase("1.1")]
        [TestCase("10/2")]
        [TestCase("(((((((1+1)^2)^3)^4)^5)^6)^7)^8")]
        [TestCase("3*5")]
        [TestCase("1+1")]
        [TestCase("2^5")]
        [TestCase("2^2^2^2")]
        [TestCase("3-8")]
        [TestCase("(1+2^3^4)^(5*(6+7))")]
        [TestCase("((1+2)^(3+4))^5")]
        [TestCase("(1+2)*(3+4)")]
        [TestCase("1+2÷(2*4)")]
        [TestCase("8-(5+2)")]
        [TestCase("8+(5+2)")]
        [TestCase("(8+5)+2")]
        [TestCase("8-5-2")]
        [TestCase("(-2)^2")]
        [TestCase("-(2^2)")]
        [TestCase("((1+2)+(3+4))")]
        [TestCase("(((1+2)+3)+4)")]
        [TestCase("(1+(2+(3+4)))")]
        [TestCase("((1+(2+3))+4)")]
        [TestCase("(1+((2+3)+4))")]
        [TestCase("((1*2)*(3*4))")]
        [TestCase("(((1*2)*3)*4)")]
        [TestCase("(1*(2*(3*4)))")]
        [TestCase("((1*(2*3))*4)")]
        [TestCase("(1*((2*3)*4))")]
        [TestCase("((1÷2)÷(3÷4))")]
        [TestCase("(((1÷2)÷3)÷4)")]
        [TestCase("(1÷(2÷(3÷4)))")]
        [TestCase("((1÷(2÷3))÷4)")]
        [TestCase("(1÷((2÷3)÷4))")]
        [TestCase("((1-2)-(3-4))")]
        [TestCase("(((1-2)-3)-4)")]
        [TestCase("(1-(2-(3-4)))")]
        [TestCase("((1-(2-3))-4)")]
        [TestCase("(1-((2-3)-4))")]
        [TestCase("((1^2)^(3^4))")]
        [TestCase("(((1^2)^3)^4)")]
        [TestCase("(1^(2^(3^4)))")]
        [TestCase("((1^(2^3))^4)")]
        [TestCase("(1^((2^3)^4))")]
        public void MeasureAndDrawExpression_ApprovalTest(string input)
        {
            ExpressionRendererTestHelper(input);
        }

        [TestCaseSource(typeof(ExpressionRendererTests), nameof(ExpressionTestCases))]
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
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                    size = renderer.Measure(graphics, expression, out baseline);
                }

                const float Padding = 5;
                using (var bitmap = new Bitmap((int)Math.Ceiling(size.Width + Padding * 2), (int)Math.Ceiling(size.Height + Padding * 2)))
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

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

        private static string SanitizeName(string testName)
        {
            return Regex.Replace(testName.Replace('"', '\'').Replace('*', '×').Replace('/', '÷'), "[" + Regex.Escape(new string(Path.GetInvalidFileNameChars())) + "]", "_");
        }

        private static void WriteAndAssertResult(Bitmap bitmap)
        {
            var test = TestContext.CurrentContext.Test;
            var testPath = Path.Combine(test.ClassName, SanitizeName(test.Name) + ".png");
            var actualPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "ActualResults", testPath);
            var expectedPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "ExpectedResults", testPath);
            Directory.CreateDirectory(Path.GetDirectoryName(actualPath));
            bitmap.Save(actualPath);

            Assert.That(File.Exists(expectedPath), Is.True);
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
