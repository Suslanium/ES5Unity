using System;
using MasterFile;
using UnityEngine;

namespace Tests
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