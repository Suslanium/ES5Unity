namespace Engine.Core
{
    public static class Logger
    {
        public static void Log(object message)
        {
#if (DEVELOPMENT_BUILD || UNITY_EDITOR)
            UnityEngine.Debug.Log(message);
#endif
        }
        
        public static void LogWarning(string message)
        {
#if (DEVELOPMENT_BUILD || UNITY_EDITOR)
            UnityEngine.Debug.LogWarning(message);
#endif
        }
        
        public static void LogError(string message)
        {
#if (DEVELOPMENT_BUILD || UNITY_EDITOR)
            UnityEngine.Debug.LogError(message);
#endif
        }
    }
}