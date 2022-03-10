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

using System;
using System.Collections.Generic;

namespace BuildScript.Nuke.IntegrationTests;

public record TestOutputData(
    int Total,
    int Executed,
    int Passed,
    int Failed
);

public class TestProjectOutput : ProjectOutput
{
  public TestOutputData ExpectedTestOutputData { get; set; } = new(0, 0, 0, 0);

  public TestProjectOutput (
      string name,
      string testSolutionPath,
      IReadOnlyCollection<string> outputPaths,
      ProjectOutputConfiguration configuration = ProjectOutputConfiguration.ReleaseDebug,
      bool isSdkProject = true)
      : base(name, testSolutionPath, outputPaths, configuration, isSdkProject)
  {
  }
}