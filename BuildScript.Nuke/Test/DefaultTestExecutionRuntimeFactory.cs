﻿// Copyright (c) rubicon IT GmbH, www.rubicon.eu
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
using DockerExecutionRuntime = Remotion.BuildScript.Test.Runtimes.DockerExecutionRuntime;

namespace Remotion.BuildScript.Test;

public class DefaultTestExecutionRuntimeFactory : ITestExecutionRuntimeFactory
{
  public static readonly DefaultTestExecutionRuntimeFactory Instance = new();

  private DefaultTestExecutionRuntimeFactory ()
  {
  }

  public ITestExecutionRuntime CreateTestExecutionRuntime (TestExecutionContext context)
  {
    var executionRuntime = context.TestMatrixRow.GetDimension<ExecutionRuntimes>();
    if (executionRuntime == ExecutionRuntimes.LocalMachine || executionRuntime == ExecutionRuntimes.EnforcedLocalMachine)
    {
      return new LocalExecutionRuntime();
    }

    if (executionRuntime is DockerExecutionRuntimes dockerExecutionRuntimes)
    {
      var dockerImage = dockerExecutionRuntimes.GetImage(context);
      var dockerIsolationMode = dockerExecutionRuntimes.GetIsolationMode(context);
      return new DockerExecutionRuntime(
          dockerImage,
          dockerIsolationMode);
    }

    throw new NotSupportedException($"The specified execution runtime '{executionRuntime}' is not supported.");
  }
}