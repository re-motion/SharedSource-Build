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
using Nuke.Common;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.Docker;
using Remotion.BuildScript.Test.Dimensions;
using Remotion.BuildScript.Util;

namespace Remotion.BuildScript.Test.Runtimes;

public class DockerExecutionRuntime : ITestExecutionRuntime, IRequiresTestParameters
{
  private const string c_imageParameterName = "Image";
  private const string c_isolationModeParameterName = "IsolationMode";

  private readonly ExecutionRuntimes _executionRuntime;

  public DockerExecutionRuntime (ExecutionRuntimes executionRuntime)
  {
    _executionRuntime = executionRuntime;
  }

  public void ConfigureTestParameters (TestParameterBuilder parameter)
  {
    parameter.AddRequiredParameter(_executionRuntime, c_imageParameterName);
    parameter.AddOptionalParameter(_executionRuntime, c_isolationModeParameterName, "");
  }

  public int ExecuteTests (TestExecutionContext context)
  {
    var dockerImage = context.TestSettings.GetTestParameter(_executionRuntime, c_imageParameterName);
    var dockerIsolationMode = context.TestSettings.GetTestParameter(_executionRuntime, c_isolationModeParameterName);

    var solutionFolder = context.Build.Solution.Directory;

    var dockerRunSettings = new DockerRunSettings()
        .EnableProcessLogOutput()
        .SetImage(dockerImage)
        .When(!string.IsNullOrEmpty(dockerIsolationMode), s => s
            .SetIsolation(dockerIsolationMode)
        )
        .EnableRm()
        .AddVolume($"{solutionFolder}:{solutionFolder}")
        .AddVolume($"{context.Build.LogFolder}:{context.Build.LogFolder}")
        .When(context.TestConfiguration.GetDimensionOrDefault<TargetFrameworks>()!.IsNetFramework, s =>
        {
          var platform = context.TestConfiguration.GetDimensionOrDefault<Platforms>()!;
          var dotnetPath = DotnetUtil.GetDotnetPath(platform);
          Assert.DirectoryExists(dotnetPath, $"The .NET SDK ({platform}) needs to be installed.");

          return s.AddVolume($"{dotnetPath}:{dotnetPath}");
        })
        .SetEntrypoint(context.DotNetTestSettings.ProcessToolPath)
        .SetArgs(context.DotNetTestSettings.GetProcessArguments().RenderForExecution().Split(' ')); // todo this is incorrect we need correct arg supplying

    var process = ProcessTasks.StartProcess(dockerRunSettings);
    process.WaitForExit();

    return process.ExitCode;
  }
}