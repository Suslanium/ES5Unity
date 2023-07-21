using System;
using System.IO;
using UnityEngine;

namespace NIF.Tests
{
    public class ParseTestFileScript : MonoBehaviour
    {
        [SerializeField] private string filePath;

        private void Start()
        {
            using BinaryReader fileReader = new BinaryReader(File.Open(filePath, FileMode.Open));
            var nif = NIFReader.ReadNIF(filePath, fileReader, 0);
            Debug.Log(nif.Header.BlockTypes);
        }
    }
}