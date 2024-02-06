using UnityEngine;

namespace Engine
{
    public class LoadingScreenManager: MonoBehaviour
    {
        [SerializeField] private Camera mainCamera;

        [SerializeField] private Camera loadingScreenCamera;

        public void ShowLoadingScreen()
        {
            mainCamera.gameObject.SetActive(false);
            loadingScreenCamera.gameObject.SetActive(true);
        }

        public void HideLoadingScreen()
        {
            if (mainCamera.gameObject.activeSelf) return;
            mainCamera.gameObject.SetActive(true);
            loadingScreenCamera.gameObject.SetActive(false);
        }
    }
}