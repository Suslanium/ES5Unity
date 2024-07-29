namespace NIF.Parser.NiObjects
{
    /// <summary>
    /// Bethesda class to combine NiObject and hkReferencedObject so that Havok classes can be read/written with NiStream.
    /// </summary>
    public abstract class BhkSerializable : BhkRefObject
    {}
}