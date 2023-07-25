using System;

namespace Core
{
    /// <summary>
    /// A reimplementation of a standard exception type that was introduced in .NET 3.0.
    /// (Taken from https://github.com/ColeDeanShepherd/TESUnity/blob/f4d5e19f68da380da9da745356c7904f3428b9d6/Assets/Scripts/Core/Exceptions.cs)
    /// </summary>
    public class FileFormatException : FormatException
    {
        public FileFormatException() : base() { }
        public FileFormatException(string message) : base(message) { }
        public FileFormatException(string message, Exception innerException) : base(message, innerException) { }
    }
}