using System;
using System.Collections.Generic;
using System.Linq;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Project = Microsoft.Build.Evaluation.Project;

internal partial class Build : NukeBuild
{
  private const string c_projectsFileName = "Projects.props";
  private const string c_propertiesFileName = "Properties.props";
  private const string c_buildConfigFileName = "BuildConfiguration.props";
  private const string c_normalTestConfigurationProperty = "NormalTestConfiguration";
  private const string c_releaseProjectFilesItem = "ReleaseProjectFiles";
  private const string c_unitTestProjectFilesItem = "UnitTestProjectFiles";
  private const string c_integrationTestProjectFilesItem = "IntegrationTestProjectFiles";
  private const string c_assemblyInfoFileProperty = "AssemblyInfoFile";
  private const string c_companyNameProperty = "CompanyName";
  private const string c_companyUrlProperty = "CompanyUrl";
  private const string c_copyrightProperty = "Copyright";
  private const string c_productNameProperty = "ProductName";
  private const string c_supportedTargetRuntimesProperty = "SupportedTargetRuntimes";
  private const string c_supportedExecutionRuntimesProperty = "SupportedExecutionRuntimes";
  private const string c_supportedBrowsersProperty = "SupportedBrowsers";
  private const string c_supportedDatabaseSystemsProperty = "SupportedDatabaseSystems";
  private const string c_supportedPlatformsProperty = "SupportedPlatforms";
  private const string c_versionFileName = "Version.props";
  private const string c_versionProperty = "Version";
  private const string c_testConfigurationMetaData = "TestConfiguration";
  private const string c_solutionDirectoryProperty = "SolutionDirectory";
  private const string c_targetFrameworksProperty = "TargetFrameworks";
  private string NormalTestConfiguration { get; set; } = "";

  [Parameter("Added to the AssemblyInformationalVersion")]
  private string AdditionalBuildMetadata { get; } = "";

  private IReadOnlyCollection<string> SupportedTargetRuntimes { get; set; } = Array.Empty<string>();
  private IReadOnlyCollection<string> SupportedExecutionRuntimes { get; set; } = Array.Empty<string>();
  private IReadOnlyCollection<string> SupportedBrowsers { get; set; } = Array.Empty<string>();
  private IReadOnlyCollection<string> SupportedDatabaseSystems { get; set; } = Array.Empty<string>();
  private IReadOnlyCollection<string> SupportedPlatforms { get; set; } = Array.Empty<string>();

  private Target ImportDefinitions => _ => _
      .Executes(() =>
      {
        ImportProjectDefinition();
        ImportPropertiesDefinition();
        ImportBuildConfigurationDefinition();
        ImportVersion();
      });

  private void ImportProjectDefinition ()
  {
    var xmlProperties = LoadProjectWithSolutionDirectoryPropertySet(c_projectsFileName);
    var normalTestConfiguration = xmlProperties.Properties.Single(prop => prop.Name == c_normalTestConfigurationProperty);
    var releaseProjectFiles = xmlProperties.Items.Where(prop => prop.ItemType == c_releaseProjectFilesItem);
    var unitTestProjectFiles = xmlProperties.Items.Where(prop => prop.ItemType == c_unitTestProjectFilesItem);
    var integrationTestProjectFiles = xmlProperties.Items.Where(prop => prop.ItemType == c_integrationTestProjectFilesItem);

    NormalTestConfiguration = normalTestConfiguration.EvaluatedValue;
    ReleaseProjectFiles = releaseProjectFiles.Select(
        x => SetupReleaseProjectMetadata(x.EvaluatedInclude)).ToList();
    UnitTestProjectFiles = unitTestProjectFiles.Select(
        x => SetupTestProjectMetadata(
            x.EvaluatedInclude,
            x.GetMetadataValue(c_testConfigurationMetaData))).ToList();

    IntegrationTestProjectFiles = integrationTestProjectFiles.Select(
        x => SetupTestProjectMetadata(
            x.EvaluatedInclude,
            x.GetMetadataValue(c_testConfigurationMetaData))).ToList();
    TestProjectFiles = UnitTestProjectFiles.Concat(IntegrationTestProjectFiles).ToList();
  }

  private ProjectMetadata SetupReleaseProjectMetadata (string path)
  {
    var xmlProperties = ProjectModelTasks.ParseProject(path);
    var targetFrameworks = xmlProperties.Properties
        .Where(x => x.Name == c_targetFrameworksProperty)
        .SelectMany(x => x.EvaluatedValue.Split(";"))
        .Count();
    return new ProjectMetadata
           {
               Path = (AbsolutePath) path,
               ToolsVersion = xmlProperties.ToolsVersion,
               IsMultiTargetFramework = targetFrameworks > 1
           };
  }

  private TestProjectMetadata SetupTestProjectMetadata (string path, string testConfiguration)
  {
    var xmlProperties = ProjectModelTasks.ParseProject(path);
    var targetFrameworks = xmlProperties.Properties
        .Where(x => x.Name == c_targetFrameworksProperty)
        .SelectMany(x => x.EvaluatedValue.Split(";"))
        .Count();
    return new TestProjectMetadata
           {
               Path = (AbsolutePath) path,
               ToolsVersion = xmlProperties.ToolsVersion,
               IsMultiTargetFramework = targetFrameworks > 1,
               TestConfiguration = testConfiguration
           };
  }

  private void ImportPropertiesDefinition ()
  {
    var xmlProperties = LoadProjectWithSolutionDirectoryPropertySet(c_propertiesFileName);
    var assemblyInfoFile = xmlProperties.Properties.Single(prop => prop.Name == c_assemblyInfoFileProperty);
    var companyName = xmlProperties.Properties.Single(prop => prop.Name == c_companyNameProperty);
    var companyUrl = xmlProperties.Properties.Single(prop => prop.Name == c_companyUrlProperty);
    var copyright = xmlProperties.Properties.Single(prop => prop.Name == c_copyrightProperty);
    var productName = xmlProperties.Properties.Single(prop => prop.Name == c_productNameProperty);

    AssemblyMetadata = new AssemblyMetadata
                       {
                           AssemblyInfoFile = assemblyInfoFile.EvaluatedValue,
                           CompanyName = companyName.EvaluatedValue,
                           CompanyUrl = companyUrl.EvaluatedValue,
                           Copyright = copyright.EvaluatedValue,
                           ProductName = productName.EvaluatedValue
                       };
  }

  private void ImportBuildConfigurationDefinition ()
  {
    var xmlProperties = LoadProjectWithSolutionDirectoryPropertySet(c_buildConfigFileName);
    var supportedTargetRuntimes = xmlProperties.Items.Where(prop => prop.ItemType == c_supportedTargetRuntimesProperty);
    var supportedExecutionRuntimes = xmlProperties.Items.Where(prop => prop.ItemType == c_supportedExecutionRuntimesProperty);
    var supportedBrowsers = xmlProperties.Items.Where(prop => prop.ItemType == c_supportedBrowsersProperty);
    var supportedPlatforms = xmlProperties.Items.Where(prop => prop.ItemType == c_supportedPlatformsProperty);
    var supportedDatabaseSystems = xmlProperties.Items.Where(prop => prop.ItemType == c_supportedDatabaseSystemsProperty);

    SupportedTargetRuntimes = supportedTargetRuntimes.SelectMany(x => x.EvaluatedInclude.Split(";")).ToList();
    SupportedExecutionRuntimes = supportedExecutionRuntimes.SelectMany(x => x.EvaluatedInclude.Split(";")).ToList();
    SupportedBrowsers = supportedBrowsers.SelectMany(x => x.EvaluatedInclude.Split(";")).ToList();
    SupportedPlatforms = supportedPlatforms.SelectMany(x => x.EvaluatedInclude.Split(";")).ToList();
    SupportedDatabaseSystems = supportedDatabaseSystems.SelectMany(x => x.EvaluatedInclude.Split(";")).ToList();
  }

  private void ImportVersion ()
  {
    var xmlProperties = ProjectModelTasks.ParseProject(DirectoryHelper.CustomizationPath / c_versionFileName);
    var version = xmlProperties.Properties.Single(prop => prop.Name == c_versionProperty);
    VersionHelper = new VersionHelper(version.EvaluatedValue);
  }

  private Project LoadProjectWithSolutionDirectoryPropertySet (string configFileName)
  {
    var project = ProjectModelTasks.ParseProject(DirectoryHelper.CustomizationPath / configFileName);
    project.SetGlobalProperty(c_solutionDirectoryProperty, RootDirectory);
    project.ReevaluateIfNecessary();
    return project;
  }
}