using TMPro;
using UnityEngine;

namespace Engine
{
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private GameObject characterControls;

        [SerializeField] private GameObject loadLocationButton;

        [SerializeField] private TMP_Text loadLocationNameText;

        private GameEngine _gameEngine;

        private void Update()
        {
            if (_gameEngine is { ActiveDoorTeleport: not null })
            {
                loadLocationNameText.text = _gameEngine.ActiveDoorTeleport.destinationCellName;
                loadLocationButton.SetActive(true);
            }
            else
            {
                loadLocationButton.SetActive(false);
            }
        }

        public void LoadLocation()
        {
            _gameEngine?.LoadCell(_gameEngine.ActiveDoorTeleport.cellFormID, LoadCause.DoorTeleport,
                _gameEngine.ActiveDoorTeleport.teleportPosition, _gameEngine.ActiveDoorTeleport.teleportRotation, true);
            if (_gameEngine != null) _gameEngine.ActiveDoorTeleport = null;
        }

        public void SetLoadingState()
        {
            characterControls.SetActive(false);
        }

        public void SetInGameState()
        {
            characterControls.SetActive(true);
        }

        public void SetPausedState()
        {
            characterControls.SetActive(false);
        }

        public void SetGameEngine(GameEngine gameEngine)
        {
            _gameEngine = gameEngine;
        }
    }
}