using System;
using Engine.Cell;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Engine
{
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private GameObject characterControls;

        [SerializeField] private GameObject loadLocationButton;

        [SerializeField] private TMP_Text loadLocationNameText;

        [SerializeField] private Image fadeObject;

        private float _desiredAlpha;
        private float _currentAlpha;

        private GameEngine _gameEngine;

        private bool _hideLoadButton;
        private Action _fadeCompleteCallback;

        private Color _fadeObjColor = Color.black;

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

        private void FixedUpdate()
        {
            if (_currentAlpha == _desiredAlpha)
            {
                if (_fadeCompleteCallback == null) return;
                _fadeCompleteCallback();
                _fadeCompleteCallback = null;
                return;
            }

            _currentAlpha = Mathf.MoveTowards(_currentAlpha, _desiredAlpha, Time.fixedDeltaTime);
            _fadeObjColor.a = _currentAlpha;
            fadeObject.color = _fadeObjColor;
        }

        public void StartLoadingLocation()
        {
            FadeIn(() =>
            {
                _gameEngine?.LoadCell(_gameEngine.ActiveDoorTeleport.cellFormID, LoadCause.DoorTeleport,
                    _gameEngine.ActiveDoorTeleport.teleportPosition, _gameEngine.ActiveDoorTeleport.teleportRotation);
            });
        }

        public void FadeIn(Action fadeCompleteCallback)
        {
            _hideLoadButton = true;
            characterControls.SetActive(false);
            _desiredAlpha = 1f;
            _fadeCompleteCallback = fadeCompleteCallback;
        }

        public void SetLoadingState()
        {
            characterControls.SetActive(false);
            fadeObject.gameObject.SetActive(false);
        }

        public void SetInGameState()
        {
            _hideLoadButton = false;
            characterControls.SetActive(true);
            fadeObject.gameObject.SetActive(true);
            _desiredAlpha = 0f;
        }

        public void SetPausedState()
        {
            characterControls.SetActive(false);
            fadeObject.gameObject.SetActive(false);
        }

        public void SetGameEngine(GameEngine gameEngine)
        {
            _gameEngine = gameEngine;
        }
    }
}