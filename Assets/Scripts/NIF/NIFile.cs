using System.Collections.Generic;
using NIF.NiObjects;

namespace NIF
{
    public class NIFile
    {
        public Header Header { get; private set; }
        public List<NiObject> NiObjects { get; private set; } = new List<NiObject>();

        public NIFile(Header header)
        {
            Header = header;
        }
    }
}