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

using System;
using Nuke.Common.IO;
using Remotion.BuildScript.Test.Dimensions;

namespace Remotion.BuildScript.Util;

public static class DotnetUtil
{
  public static AbsolutePath GetDotnetPath (Platforms platform)
  {
    var specialFolder = platform == Platforms.x86
        ? Environment.SpecialFolder.ProgramFilesX86
        : Environment.SpecialFolder.ProgramFiles;

    return AbsolutePath.Create(Environment.GetFolderPath(specialFolder)) / "dotnet";
  }

  public static AbsolutePath GetDotnetExePath (Platforms platform)
  {
    return GetDotnetPath(platform) / "dotnet.exe";
  }
}