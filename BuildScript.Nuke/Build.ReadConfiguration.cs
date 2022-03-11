using System;
using System.Collections.Generic;
using System.Linq;
using Nuke.Common;

public partial class Build : NukeBuild
{
  private string NormalTestConfiguration { get; set; } = "";

  [Parameter("Added to the AssemblyInformationalVersion")]
  private string AdditionalBuildMetadata { get; } = "";

  private IReadOnlyCollection<string> SupportedTargetRuntimes { get; set; } = Array.Empty<string>();
  private IReadOnlyCollection<string> SupportedExecutionRuntimes { get; set; } = Array.Empty<string>();
  private IReadOnlyCollection<string> SupportedBrowsers { get; set; } = Array.Empty<string>();
  private IReadOnlyCollection<string> SupportedDatabaseSystems { get; set; } = Array.Empty<string>();
  private IReadOnlyCollection<string> SupportedPlatforms { get; set; } = Array.Empty<string>();

  private Target ReadConfiguration => _ => _
      .Executes(() =>
      {
        try
        {
          var projectProps = ProjectPropsReader.Read(Solution, Configuration, RootDirectory, DirectoryHelper.CustomizationPath);
          ReleaseProjectFiles = projectProps.ReleaseProjectMetadata;
          UnitTestProjectFiles = projectProps.UnitTestProjectMetadata;
          IntegrationTestProjectFiles = projectProps.IntegrationTestProjectMetadata;
          NormalTestConfiguration = projectProps.NormalTestConfiguration;
          TestProjectFiles = UnitTestProjectFiles.Concat(IntegrationTestProjectFiles).ToList();
        }
        catch (InvalidOperationException exception)
        {
          Assert.Fail(exception.Message, exception);
        }

        var buildConfigurationProps = BuildConfigurationPropsReader.Read(RootDirectory, DirectoryHelper.CustomizationPath);
        SupportedTargetRuntimes = buildConfigurationProps.SupportedTargetRuntimes;
        SupportedBrowsers = buildConfigurationProps.SupportedBrowsers;
        SupportedPlatforms = buildConfigurationProps.SupportedPlatforms;
        SupportedDatabaseSystems = buildConfigurationProps.SupportedDatabaseSystems;
        SupportedExecutionRuntimes = buildConfigurationProps.SupportedExecutionRuntimes;

        SemanticVersion = VersionPropsReader.Read(RootDirectory, DirectoryHelper.CustomizationPath);
        AssemblyMetadata = PropertiesPropsReader.Read(RootDirectory, DirectoryHelper.CustomizationPath);
      });
}