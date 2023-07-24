using System;
using System.IO;
using NIF.Converter;
using UnityEngine;

namespace NIF.Tests
{
    public class ParseTestFileScript : MonoBehaviour
    {
        [SerializeField] private string filePath;

        private void Start()
        {
            using BinaryReader fileReader = new BinaryReader(File.Open(filePath, FileMode.Open));
            var nif = NIFile.ReadNIF(filePath, fileReader, 0);
            var builder = new NIFObjectBuilder(nif);
            builder.BuildObject();
            Debug.Log(nif.Header.BlockTypes);
        }
    }
}