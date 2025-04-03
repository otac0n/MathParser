namespace MathParser
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Numerics;

    internal class ExpressionPatternList<T> : Dictionary<LambdaExpression, T>
    {
        public void Add<TIn, TOut>(Expression<Func<TIn, TOut>> expression, T value)
            where TIn : INumberBase<TIn>
            where TOut : INumberBase<TOut> =>
                base.Add(expression, value);

        public void Add<T1, T2, TOut>(Expression<Func<T1, T2, TOut>> expression, T value)
            where T1 : INumberBase<T1>
            where T2 : INumberBase<T2>
            where TOut : INumberBase<TOut> =>
                base.Add(expression, value);
    }
}
