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

        private bool _hideLoadButton;

        private void Update()
        {
            if (!_hideLoadButton && _gameEngine is { ActiveDoorTeleport: not null, GameState: not GameState.Loading })
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
            _hideLoadButton = true;
            _gameEngine?.LoadCell(_gameEngine.ActiveDoorTeleport.cellFormID, LoadCause.DoorTeleport,
                _gameEngine.ActiveDoorTeleport.teleportPosition, _gameEngine.ActiveDoorTeleport.teleportRotation, true);
        }

        public void SetLoadingState()
        {
            characterControls.SetActive(false);
        }

        public void SetInGameState()
        {
            _hideLoadButton = false;
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