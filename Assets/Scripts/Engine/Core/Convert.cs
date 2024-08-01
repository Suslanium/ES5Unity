using UnityEngine;

namespace Engine.Core
{
    /// <summary>
    /// Taken from https://github.com/ColeDeanShepherd/TESUnity/blob/f4d5e19f68da380da9da745356c7904f3428b9d6/Assets/Scripts/TES/Convert.cs#L1C20-L1C20
    /// </summary>
    public static class Convert
    {
        public const int yardInMWUnits = 64;
        public const float meterInYards = 1.09361f;
        public const float meterInMWUnits = meterInYards * yardInMWUnits;

        public const int ExteriorCellSideLengthInSamples = 33;
        public const int ExteriorCellSideLengthInMWUnits = 4096;
        public const float ExteriorCellSideLengthInMeters = (float)ExteriorCellSideLengthInMWUnits / meterInMWUnits;

        public static Quaternion RotationMatrixToQuaternion(Matrix4x4 matrix)
        {
            return Quaternion.LookRotation(matrix.GetColumn(2), matrix.GetColumn(1));
        }
    }
}