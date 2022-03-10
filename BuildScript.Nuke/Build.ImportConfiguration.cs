using System;
using System.Collections.Generic;
using System.Linq;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Utilities;
using Project = Microsoft.Build.Evaluation.Project;

public partial class Build : NukeBuild
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
  private const string c_testSetupBuildFileMetaData = "TestSetupBuildFile";
  private const string c_assemblyName = "AssemblyName";

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
    ReleaseProjectFiles = Configuration.SelectMany(config =>
        releaseProjectFiles.Select(
            x => SetupProjectMetadata(x.EvaluatedInclude, config))).ToList();
    UnitTestProjectFiles = Configuration.SelectMany(config => unitTestProjectFiles.Select(
        x => SetupTestProjectMetadata(
            x.EvaluatedInclude,
            x.GetMetadataValue(c_testConfigurationMetaData),
            x.GetMetadataValue(c_testSetupBuildFileMetaData),
            config
        ))).ToList();

    IntegrationTestProjectFiles = Configuration.SelectMany(config =>
        integrationTestProjectFiles.Select(
            x => SetupTestProjectMetadata(
                x.EvaluatedInclude,
                x.GetMetadataValue(c_testConfigurationMetaData),
                x.GetMetadataValue(c_testSetupBuildFileMetaData),
                config
            ))).ToList();
    TestProjectFiles = UnitTestProjectFiles.Concat(IntegrationTestProjectFiles).ToList();
  }

  private ProjectMetadata SetupProjectMetadata (string path, string configuration)
  {
    var project = Solution.GetProject(path);
    if (project == null)
      Assert.Fail($"Error project cannot be found under: {path}");
    var msBuildProject = project.GetMSBuildProject();
    var targetFrameworkList = ExtractTargetFrameworkList(msBuildProject);
    var targetFrameworkListTmp = project.GetTargetFrameworks();
    var assemblyName = project.GetProperty(c_assemblyName);
    var assemblyPaths = new[] { $"{project!.Directory}\\bin\\{configuration}\\{assemblyName}.dll" };
    if (targetFrameworkListTmp != null)
    {
      assemblyPaths = targetFrameworkListTmp.Select(targetFramework =>
          $"{project!.Directory}\\bin\\{configuration}\\{targetFramework}\\{assemblyName}.dll").ToArray();
      targetFrameworkList = targetFrameworkListTmp;
    }        

    return new ProjectMetadata
           {
               Project = project!,
               ProjectPath = (AbsolutePath) path,
               ToolsVersion = msBuildProject.ToolsVersion,
               IsMultiTargetFramework = targetFrameworkList.Count > 1,
               IsSdkProject = !msBuildProject.Xml.Sdk.IsNullOrEmpty(),
               Configuration = configuration,
               TargetFrameworks = targetFrameworkList,
               AssemblyPaths = assemblyPaths
           };
  }

  private TestProjectMetadata SetupTestProjectMetadata (string path, string testConfiguration, string testsSetupBuildFile, string configuration)
  {
    var projectMetadata = SetupProjectMetadata(path, configuration);
    
    return new TestProjectMetadata
           {
               Project = projectMetadata.Project!,
               ProjectPath = (AbsolutePath) path,
               ToolsVersion = projectMetadata.ToolsVersion,
               IsMultiTargetFramework = projectMetadata.IsMultiTargetFramework,
               TestConfiguration = testConfiguration,
               TestSetupBuildFile = testsSetupBuildFile,
               IsSdkProject = projectMetadata.IsSdkProject,
               Configuration = configuration,
               TargetFrameworks = projectMetadata.TargetFrameworks,
               AssemblyPaths = projectMetadata.AssemblyPaths
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
    if (!SupportedPlatforms.Any())
      SupportedPlatforms = new List<string> { "x64", "x86" };
    SupportedDatabaseSystems = supportedDatabaseSystems.SelectMany(x => x.EvaluatedInclude.Split(";")).ToList();
  }

  private void ImportVersion ()
  {
    var xmlProperties = ProjectModelTasks.ParseProject(DirectoryHelper.CustomizationPath / c_versionFileName);
    var version = xmlProperties.Properties.Single(prop => prop.Name == c_versionProperty);
    SemanticVersion = new SemanticVersion(version.EvaluatedValue);
  }

  private Project LoadProjectWithSolutionDirectoryPropertySet (string configFileName)
  {
    var project = ProjectModelTasks.ParseProject(DirectoryHelper.CustomizationPath / configFileName);
    project.SetGlobalProperty(c_solutionDirectoryProperty, RootDirectory);
    project.ReevaluateIfNecessary();
    return project;
  }

  private IReadOnlyCollection<string> ExtractTargetFrameworkList (Project xmlProperties)
  {
    var targetFrameworks = xmlProperties.Properties
        .Where(x => x.Name == c_targetFrameworksProperty)
        .SelectMany(x => x.EvaluatedValue.Split(";"));
    var targetFramework = xmlProperties.Properties
        .Where(x => x.Name == "TargetFramework")
        .SelectMany(x => x.EvaluatedValue.Split(";"));
    var targetFrameworkVersion = xmlProperties.Properties
        .Where(x => x.Name == "TargetFrameworkVersion")
        .SelectMany(x => x.EvaluatedValue.Split(";"))
        .Select(oldVersion => "net" + oldVersion.Replace("v", "").Replace(".", ""));

    var targetFrameworkList = targetFrameworks.Concat(targetFramework).Concat(targetFrameworkVersion).ToList();
    return targetFrameworkList;
  }
}