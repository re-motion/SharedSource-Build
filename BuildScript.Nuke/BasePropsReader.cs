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
using System.IO;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Project = Microsoft.Build.Evaluation.Project;

namespace Remotion.BuildScript;

public class BasePropsReader
{
  private const string c_solutionDirectoryProperty = "SolutionDirectory";

  protected readonly AbsolutePath _customizationDirectoryPath;
  private readonly AbsolutePath _solutionDirectoryPath;

  protected BasePropsReader (
      AbsolutePath solutionDirectoryPath,
      AbsolutePath customizationDirectoryPath)
  {
    _solutionDirectoryPath = solutionDirectoryPath;
    _customizationDirectoryPath = customizationDirectoryPath;
  }

  protected Project LoadProjectWithSolutionDirectoryPropertySet (string configFileName)
  {
    var project = ProjectModelTasks.ParseProject(_customizationDirectoryPath / configFileName);
    project.SetGlobalProperty(c_solutionDirectoryProperty, _solutionDirectoryPath.ToStringWithEndingDirectorySeparator());
    project.ReevaluateIfNecessary();
    return project;
  }
}
