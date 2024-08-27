namespace Engine.Core
{
    public static class Logger
    {
        #if (DEVELOPMENT_BUILD || UNITY_EDITOR)
        public static void Log(object message)
        {
            UnityEngine.Debug.Log(message);
        }
        
        public static void LogWarning(string message)
        {
            UnityEngine.Debug.LogWarning(message);
        }
        
        public static void LogError(string message)
        {
            UnityEngine.Debug.LogError(message);
        }
        #endif
    }
}