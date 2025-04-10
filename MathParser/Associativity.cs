namespace MathParser
{
    /// <summary>
    /// Indicates the associativity of an operator.
    /// </summary>
    public enum Associativity
    {
        /// <summary>
        /// The operator is not associative.
        /// </summary>
        None = 0,

        /// <summary>
        /// The operator is left-associative.
        /// </summary>
        Left,

        /// <summary>
        /// The operator is right-associative.
        /// </summary>
        Right,
    }
}
