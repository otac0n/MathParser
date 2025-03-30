namespace MathParser
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Numerics;
    using System.Reflection;

    internal class MethodList<T> : Dictionary<MethodInfo, T>
    {
        public void Add(Delegate @delegate, T value)
        {
            this.Add(@delegate.Method, value);
        }

        public void Add<TIn, TOut>(Expression<Func<TIn, TOut>> expression, T value)
            where TIn : INumberBase<TIn>
            where TOut : INumberBase<TOut> =>
                this.AddInternal(expression, value);

        public void Add<T1, T2, TOut>(Expression<Func<T1, T2, TOut>> expression, T value)
            where T1 : INumberBase<T1>
            where T2 : INumberBase<T2>
            where TOut : INumberBase<TOut> =>
                this.AddInternal(expression, value);

        private void AddInternal(LambdaExpression expression, T value)
        {
            var methodCall = (MethodCallExpression)expression.Body;
            this.Add(methodCall.Method, value);
        }
    }
}
