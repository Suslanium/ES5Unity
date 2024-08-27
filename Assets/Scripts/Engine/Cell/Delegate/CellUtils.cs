﻿using NIF.Builder;
using UnityEngine;
using Logger = Engine.Core.Logger;

namespace Engine.Cell.Delegate
{
    public static class CellUtils
    {
        public static void ApplyPositionAndRotation(float[] position, float[] rotation, float scale, GameObject parent,
            GameObject modelObject)
        {
            if (modelObject == null) return;
            if (position is not { Length: 3 } || rotation is not { Length: 3 })
            {
                modelObject.SetActive(false);
                Logger.LogWarning(
                    $"Position and/or rotation array is invalid, deactivating GameObject {modelObject.name}");
                modelObject.transform.parent = parent.transform;
                return;
            }

            if (scale != 0f)
            {
                modelObject.transform.localScale = Vector3.one * scale;
            }

            modelObject.transform.position =
                NifUtils.NifPointToUnityPoint(new Vector3(position[0], position[1], position[2]));
            modelObject.transform.rotation =
                NifUtils.NifEulerAnglesToUnityQuaternion(new Vector3(rotation[0], rotation[1], rotation[2]));
            modelObject.transform.parent = parent.transform;
        }
    }
}