using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Engine.UI
{
    public class LoadingAnimation : MonoBehaviour
    {
        [SerializeField] private Image loadingImage;
        
        [SerializeField] private List<Sprite> loadingSprites;
        
        private int _currentSpriteIndex = 0;
        
        private int _spriteAmount;

        private void Start()
        {
            _spriteAmount = loadingSprites.Count;
        }

        private void Update()
        {
            _currentSpriteIndex++;
            if (_currentSpriteIndex >= _spriteAmount)
            {
                _currentSpriteIndex = 0;
            }
            loadingImage.sprite = loadingSprites[_currentSpriteIndex];
        }
    }
}