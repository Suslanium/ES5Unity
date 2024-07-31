using System.Collections;
using MasterFile.MasterFileContents.Records;
using UnityEngine;

namespace Engine.Cell.Delegate.Interfaces
{
    public interface ICellPostProcessDelegate
    {
        public IEnumerator PostProcessCell(CELL cell, GameObject cellGameObject);
    }
}