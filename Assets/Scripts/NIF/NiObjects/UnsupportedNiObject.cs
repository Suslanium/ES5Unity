namespace NIF.NiObjects
{
    public class UnsupportedNiObject: NiObject
    {
        public string Name { get; private set; }

        public UnsupportedNiObject(string name)
        {
            Name = name;
        }
    }
}