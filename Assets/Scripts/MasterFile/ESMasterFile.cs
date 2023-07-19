using System.Collections.Generic;
using System.IO;
using MasterFile.MasterFileContents;
using MasterFile.MasterFileContents.Records;
using UnityEngine;

namespace MasterFile
{
    /// <summary>
    /// Mod files (Plugin files) are collections of records, which are further divided into fields. Records themselves are organized into groups.
    /// <para>At the highest grouping level, a plugin file is generally:</para>
    /// <para>- A single TES4 record (plugin information).</para>
    /// <para>- A collection of top groups. </para>
    /// </summary>
    public class ESMasterFile
    {
        public TES4 PluginInfo { get; private set; }
        public List<Group> Groups { get; private set; } = new();

        private ESMasterFile(){}
        
        public static ESMasterFile Parse(string filePath)
        {
            ESMasterFile masterFile = new ESMasterFile();
            using BinaryReader fileReader = new BinaryReader(File.Open(filePath, FileMode.Open));
            masterFile.PluginInfo = MasterFileEntry.Parse(fileReader, 0) as TES4;
            while (fileReader.BaseStream.Position < fileReader.BaseStream.Length)
            {
                masterFile.Groups.Add(MasterFileEntry.Parse(fileReader, fileReader.BaseStream.Position) as Group);
            }
            
            return masterFile;
        }
    }
}