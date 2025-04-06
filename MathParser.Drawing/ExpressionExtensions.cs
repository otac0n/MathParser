namespace MathParser.Drawing
{
    using System.Linq.Expressions;
    using System.Runtime.Versioning;
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
        /// <param name="scope">The scope in which the transformations are performed.</param>
        /// <returns>The visual representation of the expression.</returns>
        [SupportedOSPlatform("windows")]
        public static VisualNode TransformToVisualTree(this Expression expression, Scope? scope = null)
        {
            var transformer = new VisualNodeTransformer(scope ?? DefaultScope.Instance);
            transformer.Visit(expression);
            return transformer.Result;
        }
    }
}
