using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Evaluation;
using Nuke.Common.IO;

public record BuildConfigurationProps(
    IReadOnlyCollection<string> SupportedTargetRuntimes,
    IReadOnlyCollection<string> SupportedBrowsers,
    IReadOnlyCollection<string> SupportedPlatforms,
    IReadOnlyCollection<string> SupportedDatabaseSystems,
    IReadOnlyCollection<string> SupportedExecutionRuntimes
);

public class BuildConfigurationPropsReader : BasePropsReader
{
  private const string c_buildConfigFileName = "BuildConfiguration.props";
  private const string c_supportedTargetRuntimesProperty = "SupportedTargetRuntimes";
  private const string c_supportedExecutionRuntimesProperty = "SupportedExecutionRuntimes";
  private const string c_supportedBrowsersProperty = "SupportedBrowsers";
  private const string c_supportedDatabaseSystemsProperty = "SupportedDatabaseSystems";
  private const string c_supportedPlatformsProperty = "SupportedPlatforms";

  private readonly Project _xmlProperties;

  public static BuildConfigurationProps Read (AbsolutePath solutionDirectoryPath, AbsolutePath customizationDirectoryPath)
  {
    var buildConfigurationReader = new BuildConfigurationPropsReader(solutionDirectoryPath, customizationDirectoryPath);
    return new BuildConfigurationProps(
        buildConfigurationReader.ReadSupportedTargetRuntimes(),
        buildConfigurationReader.ReadSupportedBrowsers(),
        buildConfigurationReader.ReadSupportedPlatforms(),
        buildConfigurationReader.ReadSupportedDatabaseSystems(),
        buildConfigurationReader.ReadSupportedExecutionRuntimes()
    );
  }

  private BuildConfigurationPropsReader (AbsolutePath solutionDirectoryPath, AbsolutePath customizationDirectoryPath)
      : base(solutionDirectoryPath, customizationDirectoryPath)
  {
    _xmlProperties = LoadProjectWithSolutionDirectoryPropertySet(c_buildConfigFileName);
  }

  private IReadOnlyCollection<string> ReadSupportedTargetRuntimes ()
  {
    var supportedTargetRuntimes = _xmlProperties.Items.Where(prop => prop.ItemType == c_supportedTargetRuntimesProperty);
    return supportedTargetRuntimes.SelectMany(x => x.EvaluatedInclude.Split(";")).ToList();
  }

  private IReadOnlyCollection<string> ReadSupportedDatabaseSystems ()
  {
    var supportedDatabaseSystems = _xmlProperties.Items.Where(prop => prop.ItemType == c_supportedDatabaseSystemsProperty);
    return supportedDatabaseSystems.SelectMany(x => x.EvaluatedInclude.Split(";")).ToList();
  }

  private IReadOnlyCollection<string> ReadSupportedBrowsers ()
  {
    var supportedBrowsers = _xmlProperties.Items.Where(prop => prop.ItemType == c_supportedBrowsersProperty);
    return supportedBrowsers.SelectMany(x => x.EvaluatedInclude.Split(";")).ToList();
  }

  private IReadOnlyCollection<string> ReadSupportedPlatforms ()
  {
    var supportedPlatforms = _xmlProperties.Items.Where(prop => prop.ItemType == c_supportedPlatformsProperty)
        .SelectMany(x => x.EvaluatedInclude.Split(";")).ToList();
    return !supportedPlatforms.Any() ? new List<string> { "x64", "x86" } : supportedPlatforms;
  }

  private IReadOnlyCollection<string> ReadSupportedExecutionRuntimes ()
  {
    var supportedExecutionRuntimes = _xmlProperties.Items.Where(prop => prop.ItemType == c_supportedExecutionRuntimesProperty);
    return supportedExecutionRuntimes.SelectMany(x => x.EvaluatedInclude.Split(";")).ToList();
  }
}