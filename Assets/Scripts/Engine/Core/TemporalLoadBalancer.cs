using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Engine.UI;

namespace Engine.Core
{
    /// <summary>
    /// Distributes work (the execution of coroutines) over several frames to avoid freezes by soft-limiting execution time.
    /// (Taken from https://github.com/ColeDeanShepherd/TESUnity/blob/f4d5e19f68da380da9da745356c7904f3428b9d6/Assets/Scripts/Core/TemporalLoadBalancer.cs#L8)
    /// </summary>
    public class TemporalLoadBalancer
    {
        /// <summary>
        /// Adds a task coroutine and returns it.
        /// </summary>
        public IEnumerator AddTask(IEnumerator taskCoroutine)
        {
            _tasks.Add(taskCoroutine);

            return taskCoroutine;
        }

        /// <summary>
        /// Adds a task coroutine to be run next and returns it.
        /// </summary>
        public IEnumerator AddTaskPriority(IEnumerator taskCoroutine)
        {
            _tasks.Insert(0, taskCoroutine);

            return taskCoroutine;
        }

        public void Prioritize(IEnumerator taskCoroutine)
        {
            _tasks.Remove(taskCoroutine);
            _tasks.Insert(0, taskCoroutine);
        }

        public void CancelTask(IEnumerator taskCoroutine)
        {
#if (DEVELOPMENT_BUILD || UNITY_EDITOR) && COROUTINE_PERFORMANCE_LOGGING
            Coroutine.RemoveTask(taskCoroutine);
#endif
            _tasks.Remove(taskCoroutine);
        }

        public void RunTasks()
        {
            Debug.Assert(DesiredWorkTimePerFrame >= 0);

            if (_tasks.Count == 0)
            {
#if (DEVELOPMENT_BUILD || UNITY_EDITOR) && COROUTINE_PERFORMANCE_LOGGING
                Coroutine.CurrentTask = null;
#endif
                return;
            }

            _stopwatch.Reset();
            _stopwatch.Start();

            // Run the tasks.
            do
            {
#if (DEVELOPMENT_BUILD || UNITY_EDITOR) && COROUTINE_PERFORMANCE_LOGGING
                Coroutine.CurrentTask = _tasks[0];
#endif
                // Try to execute an iteration of a task. Remove the task if it's execution has completed.
                if (!_tasks[0].MoveNext())
                {
#if (DEVELOPMENT_BUILD || UNITY_EDITOR) && COROUTINE_PERFORMANCE_LOGGING
                    Coroutine.RemoveTask(_tasks[0]);
#endif
                    _tasks.RemoveAt(0);
                }
            } while (_tasks.Count > 0 && _stopwatch.Elapsed.TotalSeconds < DesiredWorkTimePerFrame);

#if (DEVELOPMENT_BUILD || UNITY_EDITOR) && COROUTINE_PERFORMANCE_LOGGING
            if (_stopwatch.Elapsed.TotalSeconds >= desiredWorkTime * 2)
            {
                Logger.Log(
                    $"Too much work per frame, took {_stopwatch.Elapsed.TotalMilliseconds} ms. Coroutine call stack:\n" +
                    $"{Coroutine.GetCurrentTaskCallStack()}");
            }
#endif

            _stopwatch.Stop();
        }

        public void WaitForTask(IEnumerator taskCoroutine)
        {
            Debug.Assert(_tasks.Contains(taskCoroutine));

            while (taskCoroutine.MoveNext())
            {
            }

            _tasks.Remove(taskCoroutine);
        }

        public void WaitForAllTasks()
        {
            foreach (var task in _tasks)
            {
                while (task.MoveNext())
                {
                }
            }

            _tasks.Clear();
        }

        private readonly List<IEnumerator> _tasks = new();
        private readonly Stopwatch _stopwatch = new();
        public float DesiredWorkTimePerFrame = Settings.LoadingDesiredWorkTimePerFrame;
    }
}