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
using Nuke.Common;
using Nuke.Common.Tools.DotNet;
using Remotion.BuildScript.Components;

namespace Remotion.BuildScript.Test;

public class TestExecutionContext
{
  public IBaseBuild Build { get; }

  public ProjectMetadata Project { get; }

  public TestMatrixRow TestMatrixRow { get; }

  public ImmutableDictionary<string, string> TestParameters { get; }

  public DotNetTestSettings DotNetTestSettings { get; }

  public int ExitCode { get; set; } = -1;

  public TestExecutionContext (
      IBaseBuild build,
      ProjectMetadata project,
      ImmutableDictionary<string, string> testParameters,
      TestMatrixRow testMatrixRow,
      DotNetTestSettings dotNetTestSettings)
  {
    Build = build;
    Project = project;
    TestParameters = testParameters;
    TestMatrixRow = testMatrixRow;
    DotNetTestSettings = dotNetTestSettings;
  }

  public string GetTestParameter(TestDimension testDimension, string name)
  {
    return GetTestParameter($"{testDimension}_{name}");
  }

  public string GetTestParameter (string name)
  {
    if (!TestParameters.TryGetValue(name, out var result))
    {
      Assert.Fail($"The test configuration value '{name}' is required but not set. "
                  + $"Use the environment variable 'REMOTION_{name}' to set this test configuration value.");
    }

    return result!;
  }
}