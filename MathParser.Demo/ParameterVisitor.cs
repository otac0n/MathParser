// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser.Demo
{
    using System.Collections.Immutable;
    using System.Linq.Expressions;

    internal class ParameterVisitor : ExpressionVisitor
    {
        private ImmutableHashSet<ParameterExpression> parameters = [];

        protected override Expression VisitParameter(ParameterExpression node)
        {
            this.parameters = this.parameters.Add(node);
            return node;
        }

        public ImmutableHashSet<ParameterExpression> Search(Expression expression)
        {
            this.parameters = [];
            this.Visit(expression);
            var result = this.parameters;
            this.parameters = [];
            return result;
        }
    }
}
