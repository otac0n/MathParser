namespace MathParser
{
    using System.Diagnostics;

    /// <summary>
    /// A common handle to multiple implementations of a function.
    /// </summary>
    /// <param name="name">The name of the function.</param>
    /// <remarks>Each reference is unique, even if they share a name.</remarks>
    [DebuggerDisplay("{Name,nq}()")]
    public class KnownFunction(string name)
    {
        /// <summary>
        /// Gets the name of the function.
        /// </summary>
        public string Name { get; } = name;
    }
}
