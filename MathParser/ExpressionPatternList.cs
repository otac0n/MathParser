namespace MathParser
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;

    internal class ExpressionPatternList<T> : Dictionary<LambdaExpression, T>
    {
        public void Add<TIn, TOut>(Expression<Func<TIn, TOut>> expression, T value) =>
            base.Add(expression, value);

        public void Add<T1, T2, TOut>(Expression<Func<T1, T2, TOut>> expression, T value) =>
            base.Add(expression, value);
    }
}
