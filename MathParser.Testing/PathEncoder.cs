namespace MathParser.Testing
{
    using System;

    /// <summary>
    /// Provides utilities for working with long file names.
    /// </summary>
    public static class PathEncoder
    {
        private const string DeviceNamespacePrefix = @"\\.\";
        private const string FileNamespacePrefix = @"\\?\";
        private const string UncPrefix = FileNamespacePrefix + @"UNC\";

        /// <summary>
        /// Extends a path (if necessary) with the <c>\\.\</c> Win32 file namespace prefix.
        /// </summary>
        /// <param name="path">The path to extend.</param>
        /// <returns>The extended path.</returns>
        public static string? ExtendPath(string? path)
        {
            if (path == null ||
                path.Length < 260 ||
                path.StartsWith(FileNamespacePrefix, StringComparison.Ordinal) ||
                path.StartsWith(DeviceNamespacePrefix, StringComparison.Ordinal))
            {
                return path;
            }

            if (path.StartsWith(@"\\", StringComparison.Ordinal))
            {
                return string.Concat(UncPrefix, path.AsSpan(2));
            }
            else
            {
                return FileNamespacePrefix + path;
            }
        }
    }
}
