using System;
using System.Collections.Generic;
using System.Linq;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;

#pragma warning disable CS8618

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

  public override int GetHashCode () => HashCode.Combine(Configuration, ProjectPath, ToolsVersion, IsMultiTargetFramework, TargetFrameworks,
      IsSdkProject, AssemblyPaths);

  public override string ToString () =>
      $"{nameof(Configuration)}: {Configuration}, {nameof(ProjectPath)}: {ProjectPath}, {nameof(ToolsVersion)}: {ToolsVersion}, {nameof(IsMultiTargetFramework)}: {IsMultiTargetFramework}, {nameof(TargetFrameworks)}: {string.Join(",", TargetFrameworks)}, {nameof(IsSdkProject)}: {IsSdkProject}, {nameof(AssemblyPaths)}: {string.Join(",", AssemblyPaths)}";

  public override bool Equals (object? obj)
  {
    if (ReferenceEquals(null, obj))
      return false;
    if (ReferenceEquals(this, obj))
      return true;
    if (obj.GetType() != GetType())
      return false;
    return Equals((ProjectMetadata) obj);
  }

  private bool AreEqualCollections<T> (IReadOnlyCollection<T> firstCollection, IReadOnlyCollection<T> secondCollection) => firstCollection.All(secondCollection.Contains) && secondCollection.All(firstCollection.Contains);

  protected bool Equals (ProjectMetadata other)
  {
    return Configuration == other.Configuration && ProjectPath.ToString().Equals(other.ProjectPath.ToString())
                                                && ToolsVersion == other.ToolsVersion
                                                && IsMultiTargetFramework == other.IsMultiTargetFramework
                                                && AreEqualCollections(TargetFrameworks, other.TargetFrameworks)
                                                && IsSdkProject == other.IsSdkProject
                                                && AreEqualCollections(AssemblyPaths,other.AssemblyPaths);
  }
}
#pragma warning restore CS8618