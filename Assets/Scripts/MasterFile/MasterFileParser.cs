using System;
using UnityEngine;

namespace MasterFile
{
    public class MasterFileParser : MonoBehaviour
    {
        [SerializeField] private String filePath;

        void Start()
        {
            ESMasterFile masterFile = ESMasterFile.Parse(filePath);
        }
    }
}