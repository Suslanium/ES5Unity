using System.IO;
using NIF;
using NIF.Converter;
using UnityEngine;

namespace Tests
{
    public class ParseTestFileScript : MonoBehaviour
    {
        [SerializeField] private string filePath;

        private void Start()
        {
            using BinaryReader fileReader = new BinaryReader(File.Open(filePath, FileMode.Open));
            var nif = NiFile.ReadNif(filePath, fileReader, 0);
            var builder = new NifObjectBuilder(nif);
            builder.BuildObject();
            Debug.Log(nif.Header.BlockTypes);
        }
    }
}