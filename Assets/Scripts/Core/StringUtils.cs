namespace Core
{
    /// <summary>
    /// Taken from https://github.com/ColeDeanShepherd/TESUnity/blob/f4d5e19f68da380da9da745356c7904f3428b9d6/Assets/Scripts/Core/StringUtils.cs
    /// </summary>
    public static class StringUtils
    {
        /// <summary>
        /// Quickly checks if an ASCII encoded string is equal to a C# string.
        /// </summary>
        public static bool Equals(byte[] asciiBytes, string str)
        {
            if(asciiBytes.Length != str.Length)
            {
                return false;
            }

            for(int i = 0; i < asciiBytes.Length; i++)
            {
                if(asciiBytes[i] != str[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}