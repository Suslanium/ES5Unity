using System.Collections;
using System.Collections.Generic;

namespace Engine.Core
{
    /// <summary>
    /// Class for tracing coroutine call stacks.
    /// Right now it is used to track the coroutines that cause lags/freezes.
    /// </summary>
    public static class Coroutine
    {
        private static Dictionary<IEnumerator, LinkedList<string>> _coroutineCallStack = new();

        /// <summary>
        /// Current task from the TemporalLoadBalancer.cs
        /// </summary>
        public static IEnumerator CurrentTask;

        public static IEnumerator Get(IEnumerator enumerator, string name)
        {
#if (DEVELOPMENT_BUILD || UNITY_EDITOR) && COROUTINE_PERFORMANCE_LOGGING
            if (enumerator == null) yield break;
            var task = CurrentTask;
            if (task != null)
            {
                if (!_coroutineCallStack.ContainsKey(task))
                {
                    _coroutineCallStack.Add(task, new LinkedList<string>());
                }

                _coroutineCallStack[task].AddFirst(name);
                while (enumerator.MoveNext())
                {
                    yield return null;
                }

                _coroutineCallStack[task].RemoveFirst();
            }
            else
            {
                Logger.LogError(
                    "Stack logging: Cannot add coroutine to call stack without a task from the LoadBalancer");
                while (enumerator.MoveNext())
                {
                    yield return null;
                }
            }
#else
            return enumerator;
#endif
        }

        public static IEnumerator<T> Get<T>(IEnumerator<T> enumerator, string name)
        {
#if (DEVELOPMENT_BUILD || UNITY_EDITOR) && COROUTINE_PERFORMANCE_LOGGING
            if (enumerator == null) yield break;
            var task = CurrentTask;
            if (task != null)
            {
                if (!_coroutineCallStack.ContainsKey(task))
                {
                    _coroutineCallStack.Add(task, new LinkedList<string>());
                }

                _coroutineCallStack[task].AddFirst(name);
                while (enumerator.MoveNext())
                {
                    yield return enumerator.Current;
                }

                _coroutineCallStack[task].RemoveFirst();
            }
            else
            {
                Logger.LogError(
                    "Stack logging: Cannot add coroutine to call stack without a task from the LoadBalancer");
                while (enumerator.MoveNext())
                {
                    yield return enumerator.Current;
                }
            }
#else
            return enumerator;
#endif
        }

        public static void RemoveTask(IEnumerator taskCoroutine)
        {
#if (DEVELOPMENT_BUILD || UNITY_EDITOR) && COROUTINE_PERFORMANCE_LOGGING
            _coroutineCallStack.Remove(taskCoroutine);
#endif
        }

        public static string GetCurrentTaskCallStack()
        {
#if (DEVELOPMENT_BUILD || UNITY_EDITOR) && COROUTINE_PERFORMANCE_LOGGING
            if (CurrentTask == null) return "No call stack available";
            return _coroutineCallStack.TryGetValue(CurrentTask, out var stack)
                ? string.Join('\n', stack)
                : "No call stack available";
#endif
            return null;
        }
    }
}