namespace MathParser.Drawing
{
    using System.Linq.Expressions;
    using MathParser.Drawing.VisualNodes;

    /// <summary>
    /// Provides convenience methods for <see cref="Drawing"/>.
    /// </summary>
    public static class ExpressionExtensions
    {
        /// <summary>
        /// Converts an expression to its visual representation.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns>The visual representation of the expression.</returns>
        public static VisualNode TransformToVisualTree(this Expression expression)
        {
            var transformer = new VisualNodeTransformer();
            transformer.Visit(expression);
            return transformer.Result;
        }
    }
}
