using System.Collections;
using Engine.Cell.Delegate.Reference;
using Engine.Core;
using MasterFile.MasterFileContents;
using MasterFile.MasterFileContents.Records;
using UnityEngine;

namespace Engine.Cell.Delegate
{
    /// <summary>
    /// Creates and configures individual lights in the cell.
    /// </summary>
    public class LightingObjectDelegate : ICellReferencePreprocessDelegate, ICellReferenceInstantiationDelegate
    {
        private readonly NifManager _nifManager;

        public LightingObjectDelegate(NifManager nifManager)
        {
            _nifManager = nifManager;
        }

        public bool IsPreprocessApplicable(CELL cell, REFR reference, Record referencedRecord)
        {
            return referencedRecord is LIGH light && !string.IsNullOrEmpty(light.NifModelFilename);
        }

        public IEnumerator PreprocessObject(CELL cell, GameObject cellGameObject, REFR reference,
            Record referencedRecord)
        {
            if (referencedRecord is not LIGH light)
                yield break;
            _nifManager.PreloadNifFile(light.NifModelFilename);
        }

        public bool IsInstantiationApplicable(CELL cell, REFR reference, Record referencedRecord)
        {
            return referencedRecord is LIGH;
        }

        public IEnumerator InstantiateObject(CELL cell, GameObject cellGameObject, REFR reference,
            Record referencedRecord)
        {
            if (referencedRecord is not LIGH light)
                yield break;

            var lightInstantiationCoroutine = InstantiateLightAtPositionAndRotation(reference, light,
                reference.Position,
                reference.Rotation, reference.Scale, cellGameObject);
            while (lightInstantiationCoroutine.MoveNext())
            {
                yield return null;
            }
        }

        private IEnumerator InstantiateLightAtPositionAndRotation(REFR lightReference, LIGH lightRecord,
            float[] position,
            float[] rotation,
            float scale, GameObject parent)
        {
            GameObject modelObject = null;
            if (!string.IsNullOrEmpty(lightRecord.NifModelFilename))
            {
                var modelObjectCoroutine =
                    _nifManager.InstantiateNif(lightRecord.NifModelFilename, o => { modelObject = o; });
                while (modelObjectCoroutine.MoveNext())
                {
                    yield return null;
                }
            }

            if (modelObject == null)
                modelObject = new GameObject(lightRecord.EditorID);

            CellUtils.ApplyPositionAndRotation(position, rotation, scale, parent, modelObject);
            InstantiateLightOnGameObject(lightReference, lightRecord, modelObject);
        }

        private static void InstantiateLightOnGameObject(REFR reference, LIGH lightRecord, GameObject gameObject)
        {
            if (gameObject == null) return;
            //Create separate gameObject and rotate it in case of a spot light
            if ((lightRecord.Flags & 0x0400) != 0)
            {
                var spotGameObject = new GameObject(gameObject.name)
                {
                    transform =
                    {
                        parent = gameObject.transform,
                        position = gameObject.transform.position,
                        rotation = Quaternion.LookRotation(Vector3.down)
                    }
                };
                gameObject = spotGameObject;
            }

            var light = gameObject.AddComponent<Light>();
            //For some interesting reason the actual radius shown in CK is Base light radius + XRDS value of REFR
            light.range = 2 * ((lightRecord.Radius + reference.Radius) / Convert.meterInMWUnits);
            light.color = new Color32(lightRecord.ColorRGBA[0], lightRecord.ColorRGBA[1], lightRecord.ColorRGBA[2],
                255);
            //Intensity in Unity != intensity in Skyrim
            light.intensity = lightRecord.Fade + reference.FadeOffset;
            if ((lightRecord.Flags & 0x0400) != 0)
            {
                light.type = LightType.Spot;
            }
            else if ((lightRecord.Flags & 0x0800) == 0 && (lightRecord.Flags & 0x1000) == 0)
            {
                light.shadows = LightShadows.None;
            }
        }
    }
}