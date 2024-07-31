using System.Collections;

namespace Engine.Cell.Delegate.Interfaces
{
    public interface ICellDestroyDelegate
    {
        public IEnumerator OnDestroy();
    }
}