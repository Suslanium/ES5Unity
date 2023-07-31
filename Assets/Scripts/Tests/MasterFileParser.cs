using System.Diagnostics;
using System.IO;
using MasterFile;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Tests
{
    public class MasterFileParser : MonoBehaviour
    {
        [SerializeField] private string filePath;

        private void Start()
        {
            BinaryReader fileReader = new BinaryReader(File.Open(filePath, FileMode.Open));
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            var masterFile = new ESMasterFile(fileReader);
            stopWatch.Stop();
            var elapsedMs = stopWatch.ElapsedMilliseconds;
            fileReader.Close();
            Debug.Log(elapsedMs);
        }
    }
}