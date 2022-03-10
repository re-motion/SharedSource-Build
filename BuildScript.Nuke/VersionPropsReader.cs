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
using System.Linq;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Project = Microsoft.Build.Evaluation.Project;


public class VersionPropsReader : BasePropsReader
{
  private const string c_versionFileName = "Version.props";
  private const string c_versionProperty = "Version";

  private readonly Project _xmlProperties;

  public static SemanticVersion Read (AbsolutePath solutionDirectoryPath, AbsolutePath customizationDirectoryPath)
  {
    var versionPropsReader = new VersionPropsReader(solutionDirectoryPath, customizationDirectoryPath);
    return versionPropsReader.ReadVersion();
  }

  private VersionPropsReader (AbsolutePath solutionDirectoryPath, AbsolutePath customizationDirectoryPath)
      : base(solutionDirectoryPath, customizationDirectoryPath)
  {
    _xmlProperties = ProjectModelTasks.ParseProject(_customizationDirectoryPath / c_versionFileName);
  }

  private SemanticVersion ReadVersion ()
  {
    var version = _xmlProperties.Properties.Single(prop => prop.Name == c_versionProperty);
    return new SemanticVersion(version.EvaluatedValue);
  }
}