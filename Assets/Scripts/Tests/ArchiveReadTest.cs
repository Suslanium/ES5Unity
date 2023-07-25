using System;
using System.IO;
using BSA;
using UnityEngine;

namespace Tests
{
    public class ArchiveReadTest: MonoBehaviour
    {
        [SerializeField] private string filePath;

        private void Start()
        {
            var binaryReader = new BinaryReader(File.Open(filePath, FileMode.Open));
            var bsa = BSAFile.InitBSAFile(binaryReader);
            binaryReader.Close();
        }
    }
}