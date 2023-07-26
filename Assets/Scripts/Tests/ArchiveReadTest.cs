using System.IO;
using BSA;
using NIF;
using NIF.Converter;
using UnityEngine;

namespace Tests
{
    public class ArchiveReadTest: MonoBehaviour
    {
        [SerializeField] private string meshesArchivePath;
        [SerializeField] private string meshPath;

        private void Start()
        {
            var binaryReader = new BinaryReader(File.Open(meshesArchivePath, FileMode.Open));
            var bsa = new BsaFile(binaryReader);
            var fileStream = bsa.GetFile(meshPath);
            var nifReader = new BinaryReader(fileStream);
            var nif = NiFile.ReadNif(meshPath, nifReader, 0);
            var builder = new NifObjectBuilder(nif);
            builder.BuildObject();
            binaryReader.Close();
            nifReader.Close();
        }
    }
}