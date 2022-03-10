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