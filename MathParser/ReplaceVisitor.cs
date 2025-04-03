namespace MathParser
{
    using System.Collections.Generic;
    using System.Linq.Expressions;

    /// <summary>
    /// A visitor that replaces nodes.
    /// </summary>
    /// <param name="replacements">The mapping of nodes to replace.</param>
    public class ReplaceVisitor(IDictionary<Expression, Expression> replacements) : ExpressionVisitor
    {
        /// <inheritdoc/>
        public override Expression? Visit(Expression? node) =>
            node != null && replacements.TryGetValue(node, out var replacement) ? replacement : base.Visit(node);
    }
}
