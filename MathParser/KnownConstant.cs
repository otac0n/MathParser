namespace MathParser
{
    using System.Diagnostics;

    /// <summary>
    /// A common handle to multiple representations of a constant.
    /// </summary>
    /// <param name="name">The name of the constant.</param>
    /// <remarks>Each reference is unique, even if they share a name.</remarks>
    [DebuggerDisplay("{Name,nq}")]
    public class KnownConstant(string name) : IKnownObject
    {
        /// <summary>
        /// Gets the name of the constant.
        /// </summary>
        public string Name => name;

        /// <inheritdoc/>
        public override string ToString() => name;
    }
}
