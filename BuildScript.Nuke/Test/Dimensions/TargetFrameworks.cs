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
using Nuke.Common.Tools.DotNet;

namespace Remotion.BuildScript.Test.Dimensions;

// ReSharper disable InconsistentNaming

[PublicAPI]
public sealed class TargetFrameworks : TestDimension, IConfigureTestSettings
{
  // .NET Framework
  public static readonly TargetFrameworks NET462 = new(nameof(NET462), "net462", true);
  public static readonly TargetFrameworks NET472 = new(nameof(NET472), "net472", true);
  public static readonly TargetFrameworks NET48 = new(nameof(NET48), "net48", true);
  public static readonly TargetFrameworks NET481 = new(nameof(NET481), "net481", true);

  // .NET
  public static readonly TargetFrameworks NET6_0 = new(nameof(NET6_0), "net6.0", false);
  public static readonly TargetFrameworks NET6_0_WINDOWS = new(nameof(NET6_0_WINDOWS), "net6.0-windows", false);
  public static readonly TargetFrameworks NET7_0 = new(nameof(NET7_0), "net7.0", false);
  public static readonly TargetFrameworks NET7_0_WINDOWS = new(nameof(NET7_0_WINDOWS), "net7.0-windows", false);
  public static readonly TargetFrameworks NET8_0 = new(nameof(NET8_0), "net8.0", false);
  public static readonly TargetFrameworks NET8_0_WINDOWS = new(nameof(NET8_0_WINDOWS), "net8.0-windows", false);
  public static readonly TargetFrameworks NET9_0 = new(nameof(NET9_0), "net9.0", false);
  public static readonly TargetFrameworks NET9_0_WINDOWS = new(nameof(NET9_0_WINDOWS), "net9.0-windows", false);

  public string Identifier { get; }

  public bool IsNetFramework { get; }

  public TargetFrameworks (string value, string identifier, bool isNetFramework)
      : base(nameof(TargetFrameworks), value)
  {
    Identifier = identifier;
    IsNetFramework = isNetFramework;
  }

  DotNetTestSettings IConfigureTestSettings.ConfigureTestSettings (DotNetTestSettings settings)
  {
    return settings.SetFramework(Identifier);
  }
}