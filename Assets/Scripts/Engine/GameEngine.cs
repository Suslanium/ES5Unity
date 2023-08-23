using Core;
using MasterFile;

namespace Engine
{
    public class GameEngine
    {
        private const float DesiredWorkTimePerFrame = 1.0f / 200;
        private readonly ResourceManager _resourceManager;
        private readonly ESMasterFile _esMasterFile;
        private readonly TextureManager _textureManager;
        private readonly MaterialManager _materialManager;
        private readonly NifManager _nifManager;
        private readonly CellManager _cellManager;
        private readonly TemporalLoadBalancer _loadBalancer;
        
        public GameEngine(ResourceManager resourceManager, ESMasterFile masterFile)
        {
            _resourceManager = resourceManager;
            _esMasterFile = masterFile;
            _textureManager = new TextureManager(_resourceManager);
            _materialManager = new MaterialManager(_textureManager);
            _nifManager = new NifManager(_materialManager, _resourceManager);
            _loadBalancer = new TemporalLoadBalancer();
            _cellManager = new CellManager(_esMasterFile, _nifManager, _loadBalancer);
        }

        public void LoadInteriorCell(string editorId, bool clearPrevious = false)
        {
            if (clearPrevious)
            {
                _cellManager.DestroyAllCells();
                _nifManager.ClearModelCache();
                _materialManager.ClearCachedMaterialsAndTextures();
            }

            _cellManager.LoadInteriorCell(editorId);
        }
        
        public void LoadInteriorCell(uint formID, bool clearPrevious = false)
        {
            if (clearPrevious)
            {
                _cellManager.DestroyAllCells();
                _nifManager.ClearModelCache();
                _materialManager.ClearCachedMaterialsAndTextures();
            }
            
            _cellManager.LoadInteriorCell(formID);
        }

        public void Update()
        {
            _loadBalancer.RunTasks(DesiredWorkTimePerFrame);
        }

        public void OnStop()
        {
            _cellManager.DestroyAllCells();
            _nifManager.ClearModelCache();
            _materialManager.ClearCachedMaterialsAndTextures();
        }
    }
}