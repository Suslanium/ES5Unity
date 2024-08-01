using Engine.Core;
using NIF.Parser.NiObjects;
using UnityEngine;

namespace NIF.Builder
{
    /// <summary>
    /// Taken from https://github.com/ColeDeanShepherd/TESUnity/blob/f4d5e19f68da380da9da745356c7904f3428b9d6/Assets/Scripts/TES/NIF/NIFObjectBuilder.cs
    /// </summary>
    public static class NifUtils
    {
        public static Vector3 NifVectorToUnityVector(Vector3 nifVector)
        {
            Utils.Swap(ref nifVector.y, ref nifVector.z);

            return nifVector;
        }
        public static Vector3 NifPointToUnityPoint(Vector3 nifPoint)
        {
            return NifVectorToUnityVector(nifPoint) / Convert.meterInMWUnits;
        }
        public static Matrix4x4 NifRotationMatrixToUnityRotationMatrix(Matrix4x4 nifRotationMatrix)
        {
            var matrix = new Matrix4x4
            {
                m00 = nifRotationMatrix.m00,
                m01 = nifRotationMatrix.m02,
                m02 = nifRotationMatrix.m01,
                m03 = 0,
                m10 = nifRotationMatrix.m20,
                m11 = nifRotationMatrix.m22,
                m12 = nifRotationMatrix.m21,
                m13 = 0,
                m20 = nifRotationMatrix.m10,
                m21 = nifRotationMatrix.m12,
                m22 = nifRotationMatrix.m11,
                m23 = 0,
                m30 = 0,
                m31 = 0,
                m32 = 0,
                m33 = 1
            };

            return matrix;
        }
        public static Quaternion NifRotationMatrixToUnityQuaternion(Matrix4x4 nifRotationMatrix)
        {
            return Convert.RotationMatrixToQuaternion(NifRotationMatrixToUnityRotationMatrix(nifRotationMatrix));
        }
        public static Quaternion NifEulerAnglesToUnityQuaternion(Vector3 nifEulerAngles)
        {
            var eulerAngles = NifVectorToUnityVector(nifEulerAngles);

            var xRot = Quaternion.AngleAxis(Mathf.Rad2Deg * eulerAngles.x, Vector3.right);
            var yRot = Quaternion.AngleAxis(Mathf.Rad2Deg * eulerAngles.y, Vector3.up);
            var zRot = Quaternion.AngleAxis(Mathf.Rad2Deg * eulerAngles.z, Vector3.forward);

            return xRot * zRot * yRot;
        }
        
        public static Quaternion HavokQuaternionToUnityQuaternion(Quaternion havokQuaternion)
        {
            Quaternion unityQuat = new Quaternion(havokQuaternion.x, havokQuaternion.z, havokQuaternion.y, -havokQuaternion.w);
            return unityQuat;
        }

        public static void ApplyNiAvObjectTransform(NiAvObject anNiAvObject, GameObject obj)
        {
            obj.transform.position = NifPointToUnityPoint(anNiAvObject.Translation.ToUnityVector());
            obj.transform.rotation = NifRotationMatrixToUnityQuaternion(anNiAvObject.Rotation.ToMatrix4X4());
            obj.transform.localScale = anNiAvObject.Scale * Vector3.one;
        }
    }
}