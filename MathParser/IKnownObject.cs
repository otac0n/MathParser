// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser
{
    /// <summary>
    /// A common interface for <see cref="KnownFunction"/> and <see cref="KnownConstant"/>.
    /// </summary>
    public interface IKnownObject
    {
        /// <summary>
        /// Gets the name of the object.
        /// </summary>
        public string Name { get; }
    }
}
