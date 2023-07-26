namespace BSA
{
    public static class HashCalculator
    {
        /// <summary>
        /// Rewritten from https://github.com/philjord/BSAManager/blob/0b961861870f9401677e9e69a821b5d8940437e6/BSAManager/src/bsaio/HashCode.java#L14
        /// </summary>
        public static long GetHashCode(string hashName, bool isFolder)
        {
            var hash = 0L;
            string name = null;
            string extension = null;
            if (isFolder)
            {
                name = hashName;
            }
            else
            {
                var separatorIndex = hashName.LastIndexOf('.');
                switch (separatorIndex)
                {
                    case < 0:
                        name = hashName;
                        break;
                    case 0:
                        extension = hashName;
                        break;
                    default:
                        name = hashName[..separatorIndex];
                        extension = hashName[separatorIndex..];
                        break;
                }
            }

            if (name is { Length: > 0 })
            {
                var buffer = GetBytesFast(name);
                var length = buffer.Length;
                hash = (buffer[length - 1] & 255L) + ((long)length << 16) + ((buffer[0] & 255L) << 24);
                if (length > 2)
                {
                    hash += (buffer[length - 2] & 255L) << 8;
                }

                if (length > 3)
                {
                    var subHash = 0L;
                    for (var i = 1; i < length - 2; i++)
                    {
                        subHash = subHash * 0x1003fL + (buffer[i] & 255L) & 0xffffffffL;
                    }

                    hash += subHash << 32;
                }
            }

            if (extension is { Length: > 0 })
            {
                var buffer = GetBytesFast(extension);
                var length = buffer.Length;
                var subHash = 0L;
                for (var i = 0; i < length; i++)
                {
                    subHash = subHash * 0x1003fL + (buffer[i] & 255L) & 0xffffffffL;
                }
                
                hash += subHash << 32;
                switch (extension)
                {
                    case ".nif":
                        hash |= 32768L;
                        break;
                    case ".kf":
                        hash |= 128L;
                        break;
                    case ".dds":
                        hash |= 32896L;
                        break;
                    case ".wav":
                        hash |= 0x80000000L;
                        break;
                }
            }

            return hash;
        }

        private static byte[] GetBytesFast(string str)
        {
            var len = str.Length;
            var buffer = str.ToCharArray();
            var bytes = new byte[len];
            for (var j = 0; j < len; j++)
            {
                bytes[j] = checked((byte)buffer[j]);
            }

            return bytes;
        }
    }
}