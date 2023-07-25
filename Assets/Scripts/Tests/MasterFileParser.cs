using MasterFile;
using UnityEngine;

namespace Tests
{
    public class MasterFileParser : MonoBehaviour
    {
        [SerializeField] private string filePath;

        private void Start()
        {
            var masterFile = ESMasterFile.Parse(filePath);
        }
    }
}