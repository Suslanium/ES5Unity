namespace MasterFile.MasterFileContents.Records.Structures
{
    public class PortalInfo
    {
        public uint OriginReference { get; private set; }
        
        public uint DestinationReference { get; private set; }

        public PortalInfo(uint originReference, uint destinationReference)
        {
            OriginReference = originReference;
            DestinationReference = destinationReference;
        }
    }
}