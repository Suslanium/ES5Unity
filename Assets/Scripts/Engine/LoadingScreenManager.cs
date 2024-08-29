using System.Collections;
using Engine.Core;
using Engine.MasterFile;
using MasterFile.MasterFileContents.Records;
using NIF.Builder;
using UnityEngine;
using Coroutine = Engine.Core.Coroutine;

namespace Engine
{
    public class LoadingScreenManager : MonoBehaviour
    {
        [SerializeField] private Camera mainCamera;

        [SerializeField] private Camera loadingScreenCamera;

        private MasterFileManager _masterFileManager;
        private NifManager _nifManager;
        private TemporalLoadBalancer _temporalLoadBalancer;
        private const string LoadingScreenRecordType = "LSCR";
        private GameObject _currentLoadingScreenModel;
        private int _loadScreenLayer;

        private void Start()
        {
            _loadScreenLayer = LayerMask.NameToLayer("LoadScreen");
        }

        public void ShowLoadingScreen()
        {
            mainCamera.gameObject.SetActive(false);
            loadingScreenCamera.gameObject.SetActive(true);
            _temporalLoadBalancer.AddTask(LoadRandomLoadingScreen());
        }

        public void HideLoadingScreen()
        {
            if (mainCamera.gameObject.activeSelf) return;
            if (_currentLoadingScreenModel != null) Destroy(_currentLoadingScreenModel);
            mainCamera.gameObject.SetActive(true);
            loadingScreenCamera.gameObject.SetActive(false);
        }

        private IEnumerator LoadRandomLoadingScreen()
        {
            var randomScreenTask = _masterFileManager.GetRandomRecordOfTypeTask(LoadingScreenRecordType);
            while (!randomScreenTask.IsCompleted)
                yield return null;

            var loadingScreenInfo = (LSCR)randomScreenTask.Result;

            var staticModelTask = _masterFileManager.GetFromFormIDTask(loadingScreenInfo.StaticNifFormID);
            while (!staticModelTask.IsCompleted)
                yield return null;

            var staticModelInfo = (STAT)staticModelTask.Result;

            var modelObjectCoroutine = Coroutine.Get(
                _nifManager.InstantiateNif(staticModelInfo.NifModelFilename,
                    obj => { _currentLoadingScreenModel = obj; }),
                nameof(_nifManager.InstantiateNif));
            while (modelObjectCoroutine.MoveNext())
                yield return null;

            if (_currentLoadingScreenModel == null) yield break;
            _currentLoadingScreenModel.layer = _loadScreenLayer;
            var children = _currentLoadingScreenModel.GetComponentsInChildren<Transform>(includeInactive: true);
            foreach (var child in children)
                child.gameObject.layer = _loadScreenLayer;

            if (loadingScreenInfo.InitialScale != 0f)
                _currentLoadingScreenModel.transform.localScale = Vector3.one * loadingScreenInfo.InitialScale;

            _currentLoadingScreenModel.transform.position +=
                NifUtils.NifPointToUnityPoint(new Vector3(loadingScreenInfo.InitialTranslation[0],
                    loadingScreenInfo.InitialTranslation[1], loadingScreenInfo.InitialTranslation[2]));
            _currentLoadingScreenModel.transform.rotation *=
                NifUtils.NifEulerAnglesToUnityQuaternion(new Vector3(loadingScreenInfo.InitialRotation[0],
                    loadingScreenInfo.InitialRotation[1], loadingScreenInfo.InitialRotation[2]));
        }

        public void Initialize(MasterFileManager masterFileManager, NifManager nifManager,
            TemporalLoadBalancer loadBalancer)
        {
            _masterFileManager = masterFileManager;
            _nifManager = nifManager;
            _temporalLoadBalancer = loadBalancer;
        }
    }
}