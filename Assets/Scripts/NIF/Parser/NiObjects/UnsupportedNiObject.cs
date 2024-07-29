namespace NIF.Parser.NiObjects
{
    /// <summary>
    /// Dummy object for unsupported types.
    /// </summary>
    public class UnsupportedNiObject: NiObject
    {
        public string Name { get; private set; }

        public UnsupportedNiObject(string name)
        {
            Name = name;
        }
    }
}