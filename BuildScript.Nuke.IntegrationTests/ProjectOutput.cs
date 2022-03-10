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
using System.Linq;

namespace BuildScript.Nuke.IntegrationTests;

public class ProjectOutput
{
  public string Name { get; set; }
  public bool IsSdkProject { get; set; }
  public IReadOnlyCollection<string> OutputPaths { get; set; }
  public IReadOnlyCollection<string> OutputPathsExclude { get; set; }
  public IReadOnlyCollection<string> CleanDirectories { get; set; }
  public ProjectOutputConfiguration Configuration { get; set; }

  public ProjectOutput (
      string name,
      string testSolutionPath,
      IReadOnlyCollection<string> outputPaths,
      ProjectOutputConfiguration configuration = ProjectOutputConfiguration.ReleaseDebug,
      bool isSdkProject = true)
  {
    Name = name;
    CleanDirectories = new[]
                       {
                           $"{testSolutionPath}/{name}/bin/",
                           $"{testSolutionPath}/{name}/obj/"
                       };
    OutputPaths = outputPaths.Where(
        path => path.Contains(Enum.GetName(typeof(ProjectOutputConfiguration), configuration)!)
                || configuration == ProjectOutputConfiguration.ReleaseDebug).ToList();
    OutputPathsExclude = outputPaths.Except(OutputPaths).ToList();
    IsSdkProject = isSdkProject;
    Configuration = configuration;
  }
}

public enum ProjectOutputConfiguration
{
  Release,
  Debug,
  ReleaseDebug
}