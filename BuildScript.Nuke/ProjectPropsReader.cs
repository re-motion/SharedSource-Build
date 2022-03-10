using System;
using System.Collections.Generic;
using System.Linq;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Utilities;
using Project = Microsoft.Build.Evaluation.Project;

public record ProjectProps(
    IReadOnlyCollection<ProjectMetadata> ReleaseProjectMetadata,
    IReadOnlyCollection<TestProjectMetadata> UnitTestProjectMetadata,
    IReadOnlyCollection<TestProjectMetadata> IntegrationTestProjectMetadata,
    string NormalTestConfiguration
);

public class ProjectPropsReader : BasePropsReader
{
  private const string c_projectsFileName = "Projects.props";
  private const string c_normalTestConfigurationProperty = "NormalTestConfiguration";
  private const string c_releaseProjectFilesItem = "ReleaseProjectFiles";
  private const string c_unitTestProjectFilesItem = "UnitTestProjectFiles";
  private const string c_integrationTestProjectFilesItem = "IntegrationTestProjectFiles";
  private const string c_testConfigurationMetaData = "TestConfiguration";
  private const string c_targetFrameworksProperty = "TargetFrameworks";
  private const string c_testSetupBuildFileMetaData = "TestSetupBuildFile";
  private const string c_assemblyName = "AssemblyName";

  private readonly IReadOnlyCollection<string> _configuration;
  private readonly Solution _solution;

  private readonly Project _xmlProperties;

  public static ProjectProps Read (
      Solution solution,
      IReadOnlyCollection<string> configuration,
      AbsolutePath solutionDirectoryPath,
      AbsolutePath customizationDirectoryPath)
  {
    var projectPropsReader = new ProjectPropsReader(solution, configuration, solutionDirectoryPath, customizationDirectoryPath);
    return new ProjectProps(projectPropsReader.ReadReleaseProjectMetadata(),
        projectPropsReader.ReadUnitTestProjectMetadata(),
        projectPropsReader.ReadIntegrationTestProjectMetadata(),
        projectPropsReader.ReadNormalTestConfiguration()
    );
  }

  private ProjectPropsReader (
      Solution solution,
      IReadOnlyCollection<string> configuration,
      AbsolutePath solutionDirectoryPath,
      AbsolutePath customizationDirectoryPath)
      : base(solutionDirectoryPath, customizationDirectoryPath)
  {
    _solution = solution;
    _configuration = configuration;
    _xmlProperties = LoadProjectWithSolutionDirectoryPropertySet(c_projectsFileName);
  }

  private IReadOnlyCollection<ProjectMetadata> ReadReleaseProjectMetadata ()
  {
    var releaseProjectFiles = _xmlProperties.Items.Where(prop => prop.ItemType == c_releaseProjectFilesItem);
    return _configuration.SelectMany(config =>
        releaseProjectFiles.Select(
            x => SetupProjectMetadata(x.EvaluatedInclude, config))).ToList();
  }

  private IReadOnlyCollection<TestProjectMetadata> ReadUnitTestProjectMetadata ()
  {
    var unitTestProjectFiles = _xmlProperties.Items.Where(prop => prop.ItemType == c_unitTestProjectFilesItem);
    return _configuration.SelectMany(config => unitTestProjectFiles.Select(
        x => SetupTestProjectMetadata(
            x.EvaluatedInclude,
            x.GetMetadataValue(c_testConfigurationMetaData),
            x.GetMetadataValue(c_testSetupBuildFileMetaData),
            config
        ))).ToList();
  }

  private IReadOnlyCollection<TestProjectMetadata> ReadIntegrationTestProjectMetadata ()
  {
    var integrationTestProjectFiles = _xmlProperties.Items.Where(prop => prop.ItemType == c_integrationTestProjectFilesItem);
    return _configuration.SelectMany(config =>
        integrationTestProjectFiles.Select(
            x => SetupTestProjectMetadata(
                x.EvaluatedInclude,
                x.GetMetadataValue(c_testConfigurationMetaData),
                x.GetMetadataValue(c_testSetupBuildFileMetaData),
                config
            ))).ToList();
  }

  private string ReadNormalTestConfiguration ()
  {
    var normalTestConfiguration = _xmlProperties.Properties.Single(prop => prop.Name == c_normalTestConfigurationProperty);
    return normalTestConfiguration.EvaluatedValue;
  }

  private ProjectMetadata SetupProjectMetadata (string path, string configuration)
  {
    var project = _solution.GetProject(path);
    if (project == null)
      throw new InvalidOperationException($"Error project cannot be found under: {path}");
    var msBuildProject = project.GetMSBuildProject();
    var targetFrameworkList = ExtractTargetFrameworkList(msBuildProject);
    var targetFrameworkListTmp = project.GetTargetFrameworks();
    var assemblyName = project.GetProperty(c_assemblyName);
    var assemblyPaths = new[] { $"{project.Directory}\\bin\\{configuration}\\{assemblyName}.dll" };
    if (targetFrameworkListTmp != null)
    {
      assemblyPaths = targetFrameworkListTmp.Select(targetFramework =>
          $"{project.Directory}\\bin\\{configuration}\\{targetFramework}\\{assemblyName}.dll").ToArray();
      targetFrameworkList = targetFrameworkListTmp;
    }

    return new ProjectMetadata
           {
               Project = project,
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
               Project = projectMetadata.Project,
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