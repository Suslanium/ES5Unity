namespace MasterFile.MasterFileContents.Records.Structures
{
    /// <summary>
    /// 32-byte struct for Door teleports
    /// </summary>
    public class DoorTeleport
    {
        /// <summary>
        /// Destination DOOR FormId
        /// </summary>
        public uint DestinationDoorReference { get; private set; }
        
        /// <summary>
        /// x/y/z position
        /// Z is up
        /// </summary>
        public float[] Position { get; private set; }
        
        /// <summary>
        /// x/y/z rotation
        /// Z is up
        /// </summary>
        public float[] Rotation { get; private set; }
        
        /// <summary>
        /// 0x01 - No alarm
        /// </summary>
        public uint Flag { get; private set; }

        public DoorTeleport(uint destinationDoorReference, float[] position, float[] rotation, uint flag)
        {
            DestinationDoorReference = destinationDoorReference;
            Position = position;
            Rotation = rotation;
            Flag = flag;
        }
    }
}