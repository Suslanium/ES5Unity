using NIF.Structures;

namespace NIF
{
    public class NIFile
    {
        public Header Header { get; private set; }

        public NIFile(Header header)
        {
            Header = header;
        }
    }
}