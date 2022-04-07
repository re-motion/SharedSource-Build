using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using GlobExpressions;
using JetBrains.Annotations;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Utilities.Collections;
using Remotion.BuildScript.Documentation;
using Serilog;

namespace Remotion.BuildScript;

public partial class BaseBuild : NukeBuild
{
  private const string c_contentLayoutFileName = "ContentLayout.content";
  private const string c_gettingStartedFileName = "GettingStarted.aml";
  private const string c_contentLayoutEmbeddedResourceName = "Remotion.BuildScript.DocumentationFiles.ContentLayout.content";
  private const string c_sandcastleProjectTemplateEmbeddedResourceName = "Remotion.BuildScript.DocumentationFiles.SandcastleProjectTemplate.shfbproj";
  private const string c_documentationProjectFileName = "documentation.shfbproj";
  private const string c_outputDocumentationFolderName = "Output";

  private readonly string[] _filterSpec =
  {
      "net48",
      "net472",
      "net471",
      "net47",
      "net462",
      "net461",
      "net46",
      "net452",
      "net451",
      "net45"
  };

  private readonly string[] _standardExcludes =
  {
      ".git",
      ".svn",
      "_svn"
  };

  public string DocumentOutputFile { get; set; } = "";

  [PublicAPI]
  public Target GenerateDocumentation => _ => _
      .DependsOn(CompileReleaseBuild, CleanFolders)
      .OnlyWhenStatic(() => !SkipDocumentation)
      .Executes(() =>
      {
        CreateSandcastleProjectFile(ReleaseProjectFiles);
        CreateDocumentationNugetPackage();
      });

  private void CreateSandcastleProjectFile (IReadOnlyCollection<ProjectMetadata> projectMetadataList)
  {
    var documentationProjectFiles = PrepareProjectMetadata(projectMetadataList)
        .Where(projectFile => !projectFile.ExcludeFromDocumentation && projectFile.Configuration == "Debug")
        .Where(projectFile => _filterSpec.Any(filter => projectFile.TargetFrameworks.Contains(filter)))
        .ToList();
    FileSystemTasks.EnsureExistingDirectory(Directories.DocumentationBaseDirectory);
    var documentationCompilationOutputDirectory = Directories.DocumentationBaseDirectory / c_outputDocumentationFolderName;
    var documentationSandcastleProjectFile = Directories.DocumentationBaseDirectory / c_documentationProjectFileName;
    var currentAssembly = typeof(BaseBuild).Assembly;
    using (var sandcastleProjectTemplateStream = currentAssembly.GetManifestResourceStream(c_sandcastleProjectTemplateEmbeddedResourceName))
    using (var sandcastleProjectFile = File.Open(documentationSandcastleProjectFile, FileMode.OpenOrCreate))
    {
      sandcastleProjectTemplateStream!.CopyTo(sandcastleProjectFile);
    }

    using (var contentLayoutStream = currentAssembly.GetManifestResourceStream(c_contentLayoutEmbeddedResourceName))
    using (var contentLayoutFile = File.Open(Directories.DocumentationBaseDirectory / c_contentLayoutFileName, FileMode.OpenOrCreate))
    {
      contentLayoutStream!.CopyTo(contentLayoutFile);
    }

    FileSystemTasks.CopyFile(AssemblyMetadata.DocumentationRootPage, Directories.DocumentationBaseDirectory / c_gettingStartedFileName);
    var namespaceSummaryFiles = Glob.Files(
            Directories.Solution,
            AssemblyMetadata.DocumentationNamespaceSummaryFiles)
        .Where(file => !_standardExcludes.Any(file.Contains))
        .ToList();
    var sandcastlePackage = NuGetPackageResolver
        .GetLocalInstalledPackage("EWSoftware.SHFB", ToolPathResolver.NuGetAssetsConfigFile)
        .NotNull("sandcastlePackage != null");
    var documentationProperties = new Dictionary<string, string>
                                  {
                                      { SandcastleProperties.HtmlHelpName, AssemblyMetadata.DocumentationFileName },
                                      { SandcastleProperties.OutputPath, documentationCompilationOutputDirectory },
                                      { SandcastleProperties.FooterText, $"<![CDATA[<p>Version: {SemanticVersion.Version}</p>]]>" },
                                      { SandcastleProperties.HelpTitle, AssemblyMetadata.ProductName },
                                      { SandcastleProperties.ProductTitle, AssemblyMetadata.ProductName },
                                      { SandcastleProperties.CopyrightHref, AssemblyMetadata.CompanyUrl },
                                      { SandcastleProperties.CopyrightText, AssemblyMetadata.Copyright },
                                      { SandcastleProperties.HelpFileVersion, SemanticVersion.AssemblyVersion },
                                      { SandcastleProperties.VendorName, AssemblyMetadata.CompanyName },
                                      {
                                          SandcastleProperties.BuildLogFile,
                                          $"{Directories.Log}Sandcastle.{AssemblyMetadata.DocumentationFileName}.log"
                                      },
                                      { SandcastleProperties.WorkingPath, Directories.DocumentationBaseDirectory / "Working" },
                                      { SandcastleProperties.SHFBROOT, sandcastlePackage.Directory / "tools" },
                                      { SandcastleProperties.ComponentPath, $"{sandcastlePackage.Directory.Parent.Parent}" }
                                  };
    PrepareSandcastleProjectFile(documentationSandcastleProjectFile, documentationProperties);

    var sandcastleProjectBuilder = new SandcastleProjectBuilder(
        documentationSandcastleProjectFile,
        documentationProjectFiles,
        namespaceSummaryFiles);
    sandcastleProjectBuilder.UpdateSandcastleProject();
    MSBuildTasks.MSBuild(settings => settings
        .SetTargetPath(documentationSandcastleProjectFile)
        .SetProcessEnvironmentVariable(SandcastleProperties.SHFBROOT, sandcastlePackage.Directory / "tools"));
    DocumentOutputFile = @$"{documentationCompilationOutputDirectory}\{AssemblyMetadata.DocumentationFileName}.chm";
  }

  private void CreateDocumentationNugetPackage ()
  {
    var documentationProjects = ReleaseProjectFiles.Where(projectMetadata => projectMetadata.IsDocumentationFile).ToList();
    if (documentationProjects.Count == 0)
      Log.Warning("No documentation projects found!");

    documentationProjects.ForEach(projectFile =>
    {
      PrepareDocumentationProjectFile(projectFile);
      try
      {
        GenerateSinglePackageWithDebugSymbols(projectFile);
      }
      catch (Exception ex)
      {
        Log.Error(ex, "Error while generating nuget package for documentation");
      }
      finally
      {
        RestoreDocumentationProjectFile(projectFile);
      }
    });
  }

  private void RestoreDocumentationProjectFile (ProjectMetadata projectFile)
  {
    FileSystemTasks.CopyFile($"{projectFile.ProjectPath}.backup", projectFile.ProjectPath, FileExistsPolicy.Overwrite);
    FileSystemTasks.DeleteFile($"{projectFile.ProjectPath}.backup");
  }

  private void PrepareDocumentationProjectFile (ProjectMetadata projectFile)
  {
    FileSystemTasks.CopyFile(projectFile.ProjectPath, $"{projectFile.ProjectPath}.backup", FileExistsPolicy.Overwrite);

    var xmlDocument = new XmlDocument();
    xmlDocument.Load(projectFile.ProjectPath);

    var projectNode = xmlDocument.SelectSingleNode("/Project");
    var itemGroupNode = xmlDocument.CreateElement("ItemGroup");
    var noneNode = xmlDocument.CreateElement("None");
    noneNode.SetAttribute("Pack", "True");
    noneNode.SetAttribute("Include", DocumentOutputFile);
    var copyToOutputNode = xmlDocument.CreateElement("CopyToOutputDirectory");
    copyToOutputNode.InnerText = "PreserveNewest";
    var packagePathNode = xmlDocument.CreateElement("PackagePath");
    packagePathNode.InnerText = $"doc/{AssemblyMetadata.DocumentationFileName}.chm";
    noneNode.AppendChild(packagePathNode);
    noneNode.AppendChild(copyToOutputNode);
    itemGroupNode.AppendChild(noneNode);
    projectNode!.AppendChild(itemGroupNode);

    xmlDocument.Save(projectFile.ProjectPath);
  }

  private void PrepareSandcastleProjectFile (string filePath, Dictionary<string, string> properties)
  {
    var xmlDocument = new XmlDocument();
    xmlDocument.Load(filePath);

    properties.ForEach(property =>
    {
      var nodes = xmlDocument.GetElementsByTagName(property.Key);
      if (nodes.Count > 0)
        nodes[0]!.InnerText = property.Value;
    });

    xmlDocument.Save(filePath);
  }

  private IReadOnlyCollection<ProjectMetadata> PrepareProjectMetadata (IReadOnlyCollection<ProjectMetadata> projectMetadataList)
  {
    return projectMetadataList.SelectMany(
        projectMetadata =>
            projectMetadata.TargetFrameworks.Select(
                targetFramework =>
                    CreateProjectMetadataByTargetFramework(projectMetadata, targetFramework))
    ).ToList();
  }

  private ProjectMetadata CreateProjectMetadataByTargetFramework (ProjectMetadata projectMetadata, string targetFramework)
  {
    if (!projectMetadata.IsMultiTargetFramework)
      return projectMetadata;
    return new ProjectMetadata
           {
               Configuration = projectMetadata.Configuration,
               ProjectPath = projectMetadata.ProjectPath,
               ToolsVersion = projectMetadata.ToolsVersion,
               IsMultiTargetFramework = false,
               IsSdkProject = projectMetadata.IsSdkProject,
               AssemblyPaths = new List<string>
                               {
                                   projectMetadata.AssemblyPaths.Single(
                                       x =>
                                           !projectMetadata.IsMultiTargetFramework || x.Contains(targetFramework))
                               },
               TargetFrameworks = new List<string> { targetFramework }
           };
  }
}
