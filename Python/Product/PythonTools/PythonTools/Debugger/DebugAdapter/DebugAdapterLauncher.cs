﻿// Python Tools for Visual Studio
// Copyright(c) Microsoft Corporation
// All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the License); you may not use
// this file except in compliance with the License. You may obtain a copy of the
// License at http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED ON AN  *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS
// OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY
// IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE,
// MERCHANTABILITY OR NON-INFRINGEMENT.
//
// See the Apache Version 2.0 License for specific language governing
// permissions and limitations under the License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Web;
using System.Windows.Forms;
using Microsoft.PythonTools.Infrastructure;
using Microsoft.VisualStudio.Debugger.DebugAdapterHost.Interfaces;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json.Linq;

namespace Microsoft.PythonTools.Debugger {
    [ComVisible(true)]
    [Guid(DebugAdapterLauncherCLSIDNoBraces)]
    public sealed class DebugAdapterLauncher : IAdapterLauncher {
        public const string DebugAdapterLauncherCLSIDNoBraces = "C2990BF1-A87B-4459-9478-322482C535D6";
        public const string DebugAdapterLauncherCLSID = "{" + DebugAdapterLauncherCLSIDNoBraces + "}";
        public const string VSCodeDebugEngineId = "{86432F39-ADFD-4C56-AA8F-AF8FCDC66039}";
        public static Guid VSCodeDebugEngine = new Guid(VSCodeDebugEngineId);

        private DebugInfo _debugInfo;

        public DebugAdapterLauncher() { }

        public void Initialize(IDebugAdapterHostContext context) { }

        public ITargetHostProcess LaunchAdapter(IAdapterLaunchInfo launchInfo, ITargetHostInterop targetInterop) {
            if (launchInfo.LaunchType == LaunchType.Attach) {
                var debugAttachInfo = (DebugAttachInfo)_debugInfo;

                return DebugAdapterRemoteProcess.Attach(debugAttachInfo);
            }

            var debugLaunchInfo = (DebugLaunchInfo)_debugInfo;
            var ptvsdAdapterDirectory = Path.GetDirectoryName(PythonToolsInstallPath.GetFile("Packages\\ptvsd\\adapter\\__init__.py"));

            var targetProcess = new DebugAdapterProcess(targetInterop, ptvsdAdapterDirectory);
            return targetProcess.StartProcess(debugLaunchInfo.InterpreterPathAndArguments.FirstOrDefault(), debugLaunchInfo.WebPageUrl);
        }

        public void UpdateLaunchOptions(IAdapterLaunchInfo adapterLaunchInfo) {
            if (adapterLaunchInfo.LaunchType == LaunchType.Launch) {
                _debugInfo = GetLaunchDebugInfo(adapterLaunchInfo.LaunchJson);
            } else {
                _debugInfo = GetTcpAttachDebugInfo(adapterLaunchInfo);
            }

            AddDebugOptions(adapterLaunchInfo, _debugInfo);

            adapterLaunchInfo.LaunchJson = _debugInfo.GetJsonString();
        }

        #region Launch
        private static DebugLaunchInfo GetLaunchDebugInfo(string adapterLaunchJson) {
            var adapterLaunchInfoJson = JObject.Parse(adapterLaunchJson);
            adapterLaunchInfoJson = adapterLaunchInfoJson.Value<JObject>("ConfigurationProperties") ?? adapterLaunchInfoJson;//Based on the VS version, the JSON could be nested in ConfigurationProperties

            var debugLaunchInfo = new DebugLaunchInfo() {
                CurrentWorkingDirectory = adapterLaunchInfoJson.Value<string>("cwd"),
                Console = "externalTerminal",
            };

            SetInterpreterPathAndArguments(debugLaunchInfo, adapterLaunchInfoJson);
            SetScriptPathAndArguments(debugLaunchInfo, adapterLaunchInfoJson);
            SetEnvVariables(debugLaunchInfo, adapterLaunchInfoJson);
            SetLaunchDebugOptions(debugLaunchInfo, adapterLaunchInfoJson);

            return debugLaunchInfo;
        }

        private static void SetInterpreterPathAndArguments(DebugLaunchInfo debugLaunchInfo, JObject adapterLaunchInfoJson) {
            debugLaunchInfo.InterpreterPathAndArguments = new List<string>() {
                adapterLaunchInfoJson.Value<string>("exe").Replace("\"", "")
            };

            string interpreterArgs = adapterLaunchInfoJson.Value<string>("interpreterArgs");
            try {
                debugLaunchInfo.InterpreterPathAndArguments.AddRange(GetParsedCommandLineArguments(interpreterArgs));
            } catch (Exception) {
                MessageBox.Show(Strings.UnableToParseInterpreterArgs.FormatUI(interpreterArgs), Strings.ProductTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                debugLaunchInfo.ScriptPathAndArguments = new List<string> {
                    adapterLaunchInfoJson.Value<string>("exe")
                };
            }
        }

        private static void SetScriptPathAndArguments(DebugLaunchInfo debugLaunchInfo, JObject adapterLaunchInfoJson) {
            debugLaunchInfo.ScriptPathAndArguments = new List<string> {
                adapterLaunchInfoJson.Value<string>("scriptName")
            };

            string scriptArgs = adapterLaunchInfoJson.Value<string>("scriptArgs");
            try {
                debugLaunchInfo.ScriptPathAndArguments.AddRange(GetParsedCommandLineArguments(scriptArgs));
            } catch (Exception) {
                MessageBox.Show(Strings.UnableToParseScriptArgs.FormatUI(scriptArgs), Strings.ProductTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                debugLaunchInfo.ScriptPathAndArguments = new List<string> {
                    adapterLaunchInfoJson.Value<string>("scriptName")
                };
            }
        }

        private static void SetEnvVariables(DebugLaunchInfo debugLaunchInfo, JObject adapterLaunchInfoJson) {
            var env = new Dictionary<string, string>();
            foreach (var envVariable in adapterLaunchInfoJson.Value<JArray>("env")) {
                env[envVariable.Value<string>("name")] = envVariable.Value<string>("value");
            }

            debugLaunchInfo.Env = env.Count == 0 ? null : env;
        }

        private static void SetLaunchDebugOptions(DebugLaunchInfo debugLaunchInfo, JObject adapterLaunchInfoJson) {
            string[] options = SplitDebugOptions(adapterLaunchInfoJson.Value<string>("options"));

            string djangoOption = options.FirstOrDefault(x => x.StartsWith("DJANGO_DEBUG"));
            if (djangoOption != null) {
                string[] parsedOption = djangoOption.Split('=');
                if (parsedOption.Length == 2) {
                    debugLaunchInfo.DebugDjango = parsedOption[1].Trim().Equals("true");
                }
            }

            string webPageUrlOption = options.FirstOrDefault(x => x.StartsWith("WEB_BROWSER_URL"));
            if (webPageUrlOption != null) {
                string[] parsedOption = webPageUrlOption.Split('=');
                if (parsedOption.Length == 2) {
                    debugLaunchInfo.WebPageUrl = HttpUtility.UrlDecode(parsedOption[1]);
                }
            }
        }

        private static string[] SplitDebugOptions(string options) {
            var res = new List<string>();
            int lastStart = 0;
            for (int i = 0; i < options.Length; i++) {
                if (options[i] == ';') {
                    if (i < options.Length - 1 && options[i + 1] != ';') {
                        // valid option boundary
                        res.Add(options.Substring(lastStart, i - lastStart));
                        lastStart = i + 1;
                    } else {
                        i++;
                    }
                }
            }
            if (options.Length - lastStart > 0) {
                res.Add(options.Substring(lastStart, options.Length - lastStart));
            }
            return res.ToArray();
        }

        #endregion

        #region Attach
        private static DebugAttachInfo GetTcpAttachDebugInfo(IAdapterLaunchInfo adapterLaunchInfo) {
            var debugAttachInfo = new DebugAttachInfo();

            adapterLaunchInfo.DebugPort.GetPortName(out var adapterHostPortInfo);
            debugAttachInfo.RemoteUri = new Uri(adapterHostPortInfo);

            var uriInfo = new Uri(adapterHostPortInfo);
            debugAttachInfo.Host = uriInfo.Host;
            debugAttachInfo.Port = uriInfo.Port;

            return debugAttachInfo;
        }

        #endregion

        private static void AddDebugOptions(IAdapterLaunchInfo adapterLaunchInfo, DebugInfo launchJson) {
            var debugService = (IPythonDebugOptionsService)Package.GetGlobalService(typeof(IPythonDebugOptionsService));

            // Stop on entry should always be true for VS Debug Adapter Host.
            // If stop on entry is disabled then VS will automatically issue
            // continue when it sees "stopped" event with "reason=entry".
            launchJson.StopOnEntry = true;

            launchJson.PromptBeforeRunningWithBuildError = debugService.PromptBeforeRunningWithBuildError;
            launchJson.RedirectOutput = debugService.TeeStandardOutput;
            launchJson.WaitOnAbnormalExit = debugService.WaitOnAbnormalExit;
            launchJson.WaitOnNormalExit = debugService.WaitOnNormalExit;
            launchJson.BreakOnSystemExitZero = debugService.BreakOnSystemExitZero;
            launchJson.DebugStdLib = debugService.DebugStdLib;
            launchJson.ShowReturnValue = debugService.ShowFunctionReturnValue;
        }

        [DllImport("shell32.dll", SetLastError = true)]
        private static extern IntPtr CommandLineToArgvW([MarshalAs(UnmanagedType.LPWStr)] string lpCmdLine, out int pNumArgs);

        private static IEnumerable<string> GetParsedCommandLineArguments(string command) {
            if (string.IsNullOrEmpty(command)) {
                yield break;
            }

            IntPtr argPointer = CommandLineToArgvW(command, out var argumentCount);
            if (argPointer == IntPtr.Zero) {
                throw new System.ComponentModel.Win32Exception();
            }

            try {
                for (int i = 0; i < argumentCount; i++) {
                    yield return Marshal.PtrToStringUni(Marshal.ReadIntPtr(argPointer, i * IntPtr.Size));
                }

            } finally {
                Marshal.FreeHGlobal(argPointer);
            }
        }
    }
}