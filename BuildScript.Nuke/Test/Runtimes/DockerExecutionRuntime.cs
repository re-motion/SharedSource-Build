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

public class DockerExecutionRuntime : ITestExecutionRuntime
{
  private readonly string _image;
  private readonly string _isolationMode;

  public DockerExecutionRuntime (string image, string isolationMode)
  {
    _image = image;
    _isolationMode = isolationMode;
  }

  public void ExecuteTests (TestExecutionContext context)
  {
    var solutionFolder = context.Build.Solution.Directory;

    var dockerRunSettings = new DockerRunSettings()
        .EnableProcessLogOutput()
        .SetImage(_image)
        .When(!string.IsNullOrEmpty(_isolationMode), s => s
            .SetIsolation(_isolationMode)
        )
        .EnableRm()
        .AddVolume($"{solutionFolder}:{solutionFolder}")
        .AddVolume($"{context.Build.LogFolder}:{context.Build.LogFolder}")
        .When(context.TestMatrixRow.GetDimension<TargetFrameworks>().IsNetFramework, s =>
        {
          var platform = context.TestMatrixRow.GetDimension<Platforms>();
          var dotnetPath = DotnetUtil.GetDotnetPath(platform);
          Assert.DirectoryExists(dotnetPath, $"The .NET SDK ({platform}) needs to be installed.");

          return s.AddVolume($"{dotnetPath}:{dotnetPath}");
        })
        .SetEntrypoint(context.DotNetTestSettings.ProcessToolPath)
        .SetArgs(context.DotNetTestSettings.GetProcessArguments().AsArguments().RenderAsArray())
        .SetProcessArgumentConfigurator(arguments => arguments.InsertAt(1, "--quiet"));

    var process = ProcessTasks.StartProcess(dockerRunSettings);
    process.WaitForExit();

    context.ExitCode = process.ExitCode;
  }
}