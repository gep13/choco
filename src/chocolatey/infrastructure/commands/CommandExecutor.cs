﻿// Copyright © 2017 - 2025 Chocolatey Software, Inc
// Copyright © 2011 - 2017 RealDimensions Software, LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
//
// You may obtain a copy of the License at
//
// 	http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using chocolatey.infrastructure.adapters;
using chocolatey.infrastructure.filesystem;
using chocolatey.infrastructure.logging;
using chocolatey.infrastructure.platforms;
using Process = chocolatey.infrastructure.adapters.Process;

namespace chocolatey.infrastructure.commands
{
    public sealed class CommandExecutor : ICommandExecutor
    {
        public CommandExecutor(IFileSystem fileSystem)
        {
            _fileSystem = new Lazy<IFileSystem>(() => fileSystem);
        }

        private static Lazy<IFileSystem> _fileSystem = new Lazy<IFileSystem>(() => new DotNetFileSystem());

        private static IFileSystem FileSystem
        {
            get { return _fileSystem.Value; }
        }

        private static Func<IProcess> _initializeProcess = () => new Process();

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void InitializeWith(Lazy<IFileSystem> fileSystem, Func<IProcess> processInitializer)
        {
            _fileSystem = fileSystem;
            _initializeProcess = processInitializer;
        }

        public int Execute(string process, string arguments, int waitForExitInSeconds)
        {
            return Execute(process, arguments, waitForExitInSeconds, FileSystem.GetDirectoryName(FileSystem.GetCurrentAssemblyPath()));
        }

        public int Execute(
            string process,
            string arguments,
            int waitForExitInSeconds,
            Action<object, DataReceivedEventArgs> stdOutAction,
            Action<object, DataReceivedEventArgs> stdErrAction,
            bool updateProcessPath = true
            )
        {
            return Execute(process,
                           arguments,
                           waitForExitInSeconds,
                           FileSystem.GetDirectoryName(FileSystem.GetCurrentAssemblyPath()),
                           stdOutAction,
                           stdErrAction,
                           updateProcessPath,
                           allowUseWindow: false
                );
        }

        public int Execute(string process, string arguments, int waitForExitInSeconds, string workingDirectory)
        {
            return Execute(process, arguments, waitForExitInSeconds, workingDirectory, null, null, updateProcessPath: true, allowUseWindow: false);
        }

        public int Execute(string process,
                                  string arguments,
                                  int waitForExitInSeconds,
                                  string workingDirectory,
                                  Action<object, DataReceivedEventArgs> stdOutAction,
                                  Action<object, DataReceivedEventArgs> stdErrAction,
                                  bool updateProcessPath,
                                  bool allowUseWindow
            )
        {
            return Execute(process,
                          arguments,
                          waitForExitInSeconds,
                          workingDirectory,
                          stdOutAction,
                          stdErrAction,
                          updateProcessPath,
                          allowUseWindow,
                          waitForExit: true
               );
        }

        public int Execute(string process,
                                  string arguments,
                                  int waitForExitInSeconds,
                                  string workingDirectory,
                                  Action<object, DataReceivedEventArgs> stdOutAction,
                                  Action<object, DataReceivedEventArgs> stdErrAction,
                                  bool updateProcessPath,
                                  bool allowUseWindow,
                                  bool waitForExit
            )
        {
            return ExecuteStatic(process,
                          arguments,
                          waitForExitInSeconds,
                          workingDirectory,
                          stdOutAction,
                          stdErrAction,
                          updateProcessPath,
                          allowUseWindow,
                          waitForExit
               );
        }

        public static int ExecuteStatic(string process,
                                  string arguments,
                                  int waitForExitInSeconds,
                                  string workingDirectory,
                                  Action<object, DataReceivedEventArgs> stdOutAction,
                                  Action<object, DataReceivedEventArgs> stdErrAction,
                                  bool updateProcessPath,
                                  bool allowUseWindow
           )
        {
            return ExecuteStatic(process,
                          arguments,
                          waitForExitInSeconds,
                          workingDirectory,
                          stdOutAction,
                          stdErrAction,
                          updateProcessPath,
                          allowUseWindow,
                          waitForExit: true
               );
        }

        public static int ExecuteStatic(string process,
                                  string arguments,
                                  int waitForExitInSeconds,
                                  string workingDirectory,
                                  Action<object, DataReceivedEventArgs> stdOutAction,
                                  Action<object, DataReceivedEventArgs> stdErrAction,
                                  bool updateProcessPath,
                                  bool allowUseWindow,
                                  bool waitForExit
            )
        {
            var exitCode = -1;
            if (updateProcessPath)
            {
                process = FileSystem.GetFullPath(process);
            }

            if (Platform.GetPlatform() != PlatformType.Windows)
            {
                arguments = process + " " + arguments;
                process = "mono";
            }

            if (string.IsNullOrWhiteSpace(workingDirectory))
            {
                workingDirectory = FileSystem.GetDirectoryName(FileSystem.GetCurrentAssemblyPath());
            }

            "chocolatey".Log().Debug(() => "Calling command ['\"{0}\" {1}']".FormatWith(process.EscapeCurlyBraces(), arguments.EscapeCurlyBraces()));

            var psi = new ProcessStartInfo(process.UnquoteSafe(), arguments)
            {
                UseShellExecute = false,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = !allowUseWindow,
                WindowStyle = ProcessWindowStyle.Minimized,
            };

            using (var p = _initializeProcess())
            {
                p.StartInfo = psi;
                if (stdOutAction == null)
                {
                    p.OutputDataReceived += LogOutput;
                }
                else
                {
                    p.OutputDataReceived += (s, e) => stdOutAction(s, e);
                }
                if (stdErrAction == null)
                {
                    p.ErrorDataReceived += LogError;
                }
                else
                {
                    p.ErrorDataReceived += (s, e) => stdErrAction(s, e);
                }

                p.EnableRaisingEvents = true;
                p.Start();
                p.BeginErrorReadLine();
                p.BeginOutputReadLine();

                if (waitForExit || waitForExitInSeconds > 0)
                {
                    if (waitForExitInSeconds > 0)
                    {
                        var exited = p.WaitForExit((int)TimeSpan.FromSeconds(waitForExitInSeconds).TotalMilliseconds);
                        if (exited)
                        {
                            exitCode = p.ExitCode;
                        }
                        else
                        {
                            "chocolatey".Log().Warn(ChocolateyLoggers.Important, () => @"Chocolatey timed out waiting for the command to finish. The timeout
 specified (or the default value) was '{0}' seconds. Perhaps try a
 higher `--execution-timeout`? See `choco --help` for details.".FormatWith(waitForExitInSeconds));
                        }
                    }
                    else
                    {
                        p.WaitForExit();
                        exitCode = p.ExitCode;
                    }
                }
                else
                {
                    "chocolatey".Log().Debug(ChocolateyLoggers.LogFileOnly, () => @"Started process called but not waiting on exit.");
                }
            }

            "chocolatey".Log().Debug(() => "Command ['\"{0}\" {1}'] exited with '{2}'".FormatWith(process.EscapeCurlyBraces(), arguments.EscapeCurlyBraces(), exitCode));
            return exitCode;
        }

        private static void LogOutput(object sender, DataReceivedEventArgs e)
        {
            if (e != null)
            {
                "chocolatey".Log().Info(e.Data.EscapeCurlyBraces());
            }
        }

        private static void LogError(object sender, DataReceivedEventArgs e)
        {
            if (e != null)
            {
                "chocolatey".Log().Error(e.Data.EscapeCurlyBraces());
            }
        }


#pragma warning disable IDE0022, IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void initialize_with(Lazy<IFileSystem> file_system, Func<IProcess> process_initializer)
            => InitializeWith(file_system, process_initializer);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public int execute(string process, string arguments, int waitForExitInSeconds)
            => Execute(process, arguments, waitForExitInSeconds);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public int execute(
            string process,
            string arguments,
            int waitForExitInSeconds,
            Action<object, DataReceivedEventArgs> stdOutAction,
            Action<object, DataReceivedEventArgs> stdErrAction,
            bool updateProcessPath = true)
            => Execute(process, arguments, waitForExitInSeconds, stdOutAction, stdErrAction, updateProcessPath);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public int execute(string process, string arguments, int waitForExitInSeconds, string workingDirectory)
            => Execute(process, arguments, waitForExitInSeconds, workingDirectory);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public int execute(string process,
                                  string arguments,
                                  int waitForExitInSeconds,
                                  string workingDirectory,
                                  Action<object, DataReceivedEventArgs> stdOutAction,
                                  Action<object, DataReceivedEventArgs> stdErrAction,
                                  bool updateProcessPath,
                                  bool allowUseWindow)
            => Execute(process, arguments, waitForExitInSeconds, workingDirectory, stdOutAction, stdErrAction, updateProcessPath, allowUseWindow);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public int execute(string process,
                                  string arguments,
                                  int waitForExitInSeconds,
                                  string workingDirectory,
                                  Action<object, DataReceivedEventArgs> stdOutAction,
                                  Action<object, DataReceivedEventArgs> stdErrAction,
                                  bool updateProcessPath,
                                  bool allowUseWindow,
                                  bool waitForExit)
            => Execute(process, arguments, waitForExitInSeconds, workingDirectory, stdOutAction, stdErrAction, updateProcessPath, allowUseWindow, waitForExit);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static int execute_static(string process,
                                  string arguments,
                                  int waitForExitInSeconds,
                                  string workingDirectory,
                                  Action<object, DataReceivedEventArgs> stdOutAction,
                                  Action<object, DataReceivedEventArgs> stdErrAction,
                                  bool updateProcessPath,
                                  bool allowUseWindow)
            => ExecuteStatic(process, arguments, waitForExitInSeconds, workingDirectory, stdOutAction, stdErrAction, updateProcessPath, allowUseWindow);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static int execute_static(string process,
                                  string arguments,
                                  int waitForExitInSeconds,
                                  string workingDirectory,
                                  Action<object, DataReceivedEventArgs> stdOutAction,
                                  Action<object, DataReceivedEventArgs> stdErrAction,
                                  bool updateProcessPath,
                                  bool allowUseWindow,
                                  bool waitForExit)
            => ExecuteStatic(process, arguments, waitForExitInSeconds, workingDirectory, stdOutAction, stdErrAction, updateProcessPath, allowUseWindow, waitForExit);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        private static void log_output(object sender, DataReceivedEventArgs e)
            => LogOutput(sender, e);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        private static void log_error(object sender, DataReceivedEventArgs e)
            => LogError(sender, e);
#pragma warning restore IDE0022, IDE1006
    }
}
