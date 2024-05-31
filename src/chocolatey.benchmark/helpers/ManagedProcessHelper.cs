﻿using System;
using System.Diagnostics;
using System.Linq;

namespace chocolatey.benchmark.helpers
{
    internal static class ManagedProcessHelper
    {
        private static readonly string[] _filteredParents = new[]
        {
                "explorer",
                "powershell",
                "pwsh",
                "cmd",
                "bash"
        };

        public static ProcessTree GetProcessTree(Process process = null)
        {
            if (process == null)
            {
                process = Process.GetCurrentProcess();
            }

            var tree = new ProcessTree(process.ProcessName);

            Process nextProcess = null;

            while (true)
            {
                var processId = nextProcess?.Id ?? process.Id;
                var processName = FindIndexedProcessName(processId);

                if (string.IsNullOrEmpty(processName))
                {
                    break;
                }

                var foundProcess = FindPidFromIndexedProcessName(processName);
                
                if (foundProcess == null)
                {
                    break;
                }

                nextProcess = foundProcess;
                tree.Processes.AddLast(nextProcess.ProcessName);
            }

            return tree;
        }

        public static string GetParent(Process process = null)
        {
            if (process == null)
            {
                process = Process.GetCurrentProcess();
            }

            Process nextProcess = null;

            while (true)
            {
                var processId = nextProcess?.Id ?? process.Id;

                var processName = FindIndexedProcessName(processId);

                if (string.IsNullOrEmpty(processName))
                {
                    break;
                }

                var foundProcess = FindPidFromIndexedProcessName(processName);

                if (foundProcess == null)
                {
                    break;
                }

                nextProcess = foundProcess;
            }

            return nextProcess?.ProcessName;

            //var processName = FindIndexedProcessName(process.Id);

            //if (string.IsNullOrEmpty(processName))
            //{
            //    return null;
            //}

            //var parentProcess = FindPidFromIndexedProcessName(processName);

            //if (parentProcess == null)
            //{
            //    return null;
            //}

            //var topLevelProcess = GetParent(parentProcess);

            //if (topLevelProcess == null)
            //{
            //    return parentProcess.ProcessName;
            //}

            //return topLevelProcess;
        }

        public static string GetParentFiltered(Process process = null)
        {
            if (process == null)
            {
                process = Process.GetCurrentProcess();
            }

            Process nextProcess = null;
            Process selectedProcess = null;

            while (true)
            {
                var processId = nextProcess?.Id ?? process.Id;

                var processName = FindIndexedProcessName(processId);

                if (string.IsNullOrEmpty(processName))
                {
                    break;
                }

                var foundProcess = FindPidFromIndexedProcessName(processName);

                if (foundProcess == null)
                {
                    break;
                }

                nextProcess = foundProcess;

                if (!IsIgnoredParent(nextProcess.ProcessName))
                {
                    selectedProcess = nextProcess;
                }
            }

            return nextProcess?.ProcessName;

            //var processName = FindIndexedProcessName(process.Id);

            //if (string.IsNullOrEmpty(processName))
            //{
            //    return null;
            //}

            //var parentProcess = FindPidFromIndexedProcessName(processName);

            //if (parentProcess == null)
            //{
            //    return null;
            //}

            //var topLevelProcess = GetParent(parentProcess);

            //if (topLevelProcess == null || IsIgnoredParent(topLevelProcess))
            //{
            //    if (IsIgnoredParent(parentProcess.ProcessName))
            //    {
            //        return null;
            //    }
            //    else
            //    {
            //        return parentProcess.ProcessName;
            //    }
            //}

            //return topLevelProcess;
        }

        private static bool IsIgnoredParent(string processName)
        {
            return _filteredParents.Contains(processName, StringComparer.OrdinalIgnoreCase);
        }

        private static Process FindPidFromIndexedProcessName(string indexedProcessName)
        {
            try
            {
                var parentId = new PerformanceCounter("Process", "Creating Process ID", indexedProcessName);
                return Process.GetProcessById((int)parentId.NextValue());
            }
            catch
            {
                return null;
            }
        }

        private static string FindIndexedProcessName(int pid)
        {
            var processName = Process.GetProcessById(pid).ProcessName;
            var processByName = Process.GetProcessesByName(processName);
            string processIndexedName = null;

            for (var i = 0; i < processByName.Length; i++)
            {
                try
                {
                    processIndexedName = i == 0 ? processName : processName + "#" + i;
                    var processId = new PerformanceCounter("Process", "ID Process", processIndexedName);

                    if ((int)processId.NextValue() == pid)
                    {
                        return processIndexedName;
                    }
                }
                catch
                {
                    // Empty on purpose
                }
            }

            return processIndexedName;
        }
    }
}
