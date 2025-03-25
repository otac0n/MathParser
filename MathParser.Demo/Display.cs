// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser.Demo
{
    using System;
    using System.Drawing;
    using System.Linq.Expressions;
    using System.Numerics;
    using MathParser.Drawing;

    internal class Display
    {
        private static readonly Bitmap EmptyDisplayImage = new Bitmap(1, 1);

        private readonly ExpressionRenderer renderer = new ExpressionRenderer
        {
            Font = new Font("Calibri", 20, FontStyle.Regular),
        };

        private Expression expression;

        public Bitmap ExpressionImage { get; private set; }

        public string ExpressionText { get; private set; }

        public Font Font
        {
            get
            {
                return this.renderer.Font;
            }

            set
            {
                this.renderer.Font = value;
                this.RenderExpression();
            }
        }

        public string ResultText { get; private set; }

        public void SetInput(Expression expression)
        {
            this.expression = expression;
            if (this.expression == null)
            {
                this.ResultText = "?";
                this.ExpressionImage = EmptyDisplayImage;
                this.ExpressionText = null;
                return;
            }

            try
            {
                this.RenderExpression();
            }
            catch (Exception)
            {
                this.ExpressionImage = EmptyDisplayImage;
            }

            try
            {
                this.ExpressionText = this.expression.TransformToString();
            }
            catch (Exception)
            {
                this.ExpressionText = null;
            }

            try
            {
                var converted = Expression.Call(
                    typeof(MathParser.ExpressionExtensions).GetMethod(nameof(MathParser.ExpressionExtensions.TransformToString), new[] { typeof(Complex) }),
                    this.expression.Type == typeof(Complex)
                        ? this.expression
                        : Expression.Convert(this.expression, typeof(Complex)));

                this.ResultText = ((Expression<Func<string>>)Expression.Lambda(converted)).Compile()();
            }
            catch (Exception)
            {
                this.ResultText = "?";
            }
        }

        private void RenderExpression()
        {
            if (this.expression != null)
            {
                this.ExpressionImage = this.renderer.RenderExpression(this.expression);
            }
        }
    }
}
