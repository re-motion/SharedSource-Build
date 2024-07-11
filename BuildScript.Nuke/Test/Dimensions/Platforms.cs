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
using JetBrains.Annotations;
using Nuke.Common;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Remotion.BuildScript.Util;

namespace Remotion.BuildScript.Test.Dimensions;

// ReSharper disable InconsistentNaming

[PublicAPI]
public sealed class Platforms : TestDimension, IConfigureTestSettings
{
  public static readonly Platforms x86 = new(nameof(x86));
  public static readonly Platforms x64 = new(nameof(x64));

  public Platforms (string value)
      : base(nameof(Platforms), value)
  {
  }

  DotNetTestSettings IConfigureTestSettings.ConfigureTestSettings (DotNetTestSettings settings)
  {
    var dotnetExePath = DotnetUtil.GetDotnetExePath(this);
    Assert.FileExists(dotnetExePath, $"The .NET SDK ({ToString()}) needs to be installed.");

    return settings.SetProcessToolPath(dotnetExePath);
  }
}