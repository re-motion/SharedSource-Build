// Copyright (c) rubicon IT GmbH, www.rubicon.eu
// 
// See the NOTICE file distributed with this work for additional information
// regarding copyright ownership.  rubicon licenses this file to you under
// the Apache License, Version 2.0 (the "License"); you may not use this
// file except in compliance with the License.  You may obtain a copy of the
// License at
// 
//   http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
// WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.  See the
// License for the specific language governing permissions and limitations
// under the License.
// 

using System.IO;
using System.Linq;
using Nuke.Common;

namespace Remotion.BuildScript.Components.Tasks;

public class BaseTask
{
  public static string GetMsBuildToolPath (string msBuildNukePath, string msBuildPath, VisualStudioVersion? visualStudioVersion)
  {
    var toolPath = msBuildNukePath;
    var editions = new[] { "Enterprise", "Professional", "Community", "BuildTools", "Preview" };
    if (!string.IsNullOrEmpty(msBuildPath))
      toolPath = msBuildPath;
    else if (visualStudioVersion != null)
    {
      toolPath = editions
          .Select(
              edition => Path.Combine(
                  EnvironmentInfo.SpecialFolder(SpecialFolders.ProgramFilesX86)!,
                  $@"Microsoft Visual Studio\{visualStudioVersion.VsVersion}\{edition}\MSBuild\{visualStudioVersion.MsBuildVersion}\Bin\msbuild.exe"))
          .First(File.Exists);
    }

    return toolPath;
  }
}
