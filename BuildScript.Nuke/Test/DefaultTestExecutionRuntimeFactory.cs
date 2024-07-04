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
using Remotion.BuildScript.Test.Dimensions;
using Remotion.BuildScript.Test.Runtimes;

namespace Remotion.BuildScript.Test;

public class DefaultTestExecutionRuntimeFactory : ITestExecutionRuntimeFactory, IRequiresTestParameters
{
  private const string c_imageParameterName = "Image";
  private const string c_isolationModeParameterName = "IsolationMode";

  public static readonly DefaultTestExecutionRuntimeFactory Instance = new();

  private DefaultTestExecutionRuntimeFactory ()
  {
  }

  public void ConfigureTestParameters (TestParameterBuilder builder)
  {
    foreach (var executionRuntime in builder.EnabledTestDimensions.OfType<ExecutionRuntimes>())
    {
      if (!executionRuntime.Value.StartsWith("Docker_"))
        continue;

      builder.AddRequiredParameter(executionRuntime, c_imageParameterName);
      builder.AddOptionalParameter(executionRuntime, c_isolationModeParameterName, "");
    }
  }

  public ITestExecutionRuntime CreateTestExecutionRuntime (TestExecutionContext context)
  {
    var executionRuntime = context.TestMatrixRow.GetDimension<ExecutionRuntimes>();
    if (executionRuntime == ExecutionRuntimes.LocalMachine || executionRuntime == ExecutionRuntimes.EnforcedLocalMachine)
    {
      return new LocalExecutionRuntime();
    }

    if (executionRuntime.Value.StartsWith("Docker_"))
    {
      var dockerImage = context.GetTestParameter(executionRuntime, c_imageParameterName);
      var dockerIsolationMode = context.GetTestParameter(executionRuntime, c_isolationModeParameterName);
      return new DockerExecutionRuntime(
          dockerImage,
          dockerIsolationMode);
    }

    throw new NotSupportedException($"The specified execution runtime '{executionRuntime}' is not supported.");
  }
}