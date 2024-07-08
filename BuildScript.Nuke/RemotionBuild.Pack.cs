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
using System.Collections.Immutable;
using JetBrains.Annotations;
using Nuke.Common;
using Remotion.BuildScript.Components;
using Remotion.BuildScript.Pack;

namespace Remotion.BuildScript;

public partial class RemotionBuild
{
  protected static readonly string WithDebugSymbolsOutputFolderName = "NuGetWithDebugSymbols";

  public ImmutableArray<IPackProfile> Profiles { get; set; } = ImmutableArray<IPackProfile>.Empty;

  [PublicAPI]
  public virtual Target PreparePack => _ => _
      .DependentFor<IPack>()
      .Executes(() =>
      {
        Profiles = Profiles.Add(new DefaultPackProfile(WithDebugSymbolsOutputFolderName));
      });
}