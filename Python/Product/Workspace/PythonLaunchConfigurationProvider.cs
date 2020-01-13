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

using Microsoft.VisualStudio.Workspace;
using Microsoft.VisualStudio.Workspace.Debug;

namespace Microsoft.PythonTools.Workspace {
    [ExportLaunchConfigurationProvider(
        ProviderType,
        new[] { PythonConstants.FileExtension, PythonConstants.WindowsFileExtension },
        PythonLaunchDebugTargetProvider.LaunchTypeName,
        PythonLaunchDebugTargetProvider.JsonSchema
    )]
    class PythonLaunchConfigurationProvider : ILaunchConfigurationProvider {
        public const string ProviderType = "CCA8088B-06BC-4AE7-8521-FC66628ABE13";

        public bool IsDebugLaunchActionSupported(DebugLaunchActionContext debugLaunchActionContext) {
            return true;
        }

        public void CustomizeLaunchConfiguration(DebugLaunchActionContext debugLaunchActionContext, IPropertySettings launchSettings) {
            launchSettings[PythonLaunchDebugTargetProvider.InterpreterKey] = "(default)";
            launchSettings[PythonLaunchDebugTargetProvider.InterpreterArgumentsKey] = string.Empty;
            launchSettings[PythonLaunchDebugTargetProvider.ScriptArgumentsKey] = string.Empty;
            launchSettings[PythonLaunchDebugTargetProvider.EnvKey] = PropertySettings.EmptySettings;
            launchSettings[PythonLaunchDebugTargetProvider.NativeDebuggingKey] = false;
            launchSettings[PythonLaunchDebugTargetProvider.WebBrowserUrlKey] = string.Empty;
        }
    }
}
