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
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;

#pragma warning disable CS8618

namespace Remotion.BuildScript;

public class ProjectMetadata
{
  public Project Project { get; init; }
  public string Configuration { get; init; }
  public AbsolutePath ProjectPath { get; init; }
  public string ToolsVersion { get; init; }
  public bool IsMultiTargetFramework { get; init; }
  public IReadOnlyCollection<string> TargetFrameworks { get; init; }
  public bool IsSdkProject { get; init; }
  public IReadOnlyCollection<string> AssemblyPaths { get; init; }
  public bool IsDocumentationFile { get; init; }
  public bool ExcludeFromDocumentation { get; init; }

  public override int GetHashCode () => HashCode.Combine(Configuration, ProjectPath, ToolsVersion, IsMultiTargetFramework, TargetFrameworks,
      IsSdkProject, AssemblyPaths);

  public override string ToString () =>
      $"{nameof(Configuration)}: {Configuration}, {nameof(ProjectPath)}: {ProjectPath}, "
      + $"{nameof(ToolsVersion)}: {ToolsVersion}, {nameof(IsMultiTargetFramework)}: {IsMultiTargetFramework}, "
      + $"{nameof(TargetFrameworks)}: {string.Join(",", TargetFrameworks)}, {nameof(IsSdkProject)}: {IsSdkProject}, {nameof(AssemblyPaths)}: {string.Join(",", AssemblyPaths)}";

  public override bool Equals (object? obj)
  {
    if (ReferenceEquals(null, obj))
      return false;
    if (ReferenceEquals(this, obj))
      return true;
    if (obj.GetType() != GetType())
      return false;
    return Equals((ProjectMetadata)obj);
  }

  private bool AreEqualCollections<T> (IReadOnlyCollection<T> firstCollection, IReadOnlyCollection<T> secondCollection) =>
      firstCollection.All(secondCollection.Contains) && secondCollection.All(firstCollection.Contains);

  protected bool Equals (ProjectMetadata other) =>
      Configuration == other.Configuration && ProjectPath.ToString().Equals(other.ProjectPath.ToString())
                                           && ToolsVersion == other.ToolsVersion
                                           && IsMultiTargetFramework == other.IsMultiTargetFramework
                                           && AreEqualCollections(TargetFrameworks, other.TargetFrameworks)
                                           && IsSdkProject == other.IsSdkProject
                                           && AreEqualCollections(AssemblyPaths, other.AssemblyPaths);
}
#pragma warning restore CS8618
