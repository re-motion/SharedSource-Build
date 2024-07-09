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
using System.IO;
using System.Linq;
using System.Xml;
using GlobExpressions;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Utilities.Collections;
using Remotion.BuildScript.Documentation;
using Serilog;

namespace Remotion.BuildScript.Components.Tasks;

internal static class DocumentationTask
{
  internal static string CreateSandcastleProjectFile (
      IReadOnlyCollection<ProjectMetadata> projectMetadataList,
      AssemblyMetadata assemblyMetadata,
      Directories directories,
      SemanticVersion semanticVersion)
  {
    var documentationProjectFiles = PrepareProjectMetadata(projectMetadataList)
        .Where(projectFile => !projectFile.ExcludeFromDocumentation && projectFile.Configuration == "Debug")
        .Where(projectFile => _filterSpec.Any(filter => projectFile.TargetFrameworks.Contains(filter)))
        .ToList();
    FileSystemTasks.EnsureExistingDirectory(directories.DocumentationBaseDirectory);
    var documentationCompilationOutputDirectory = directories.DocumentationBaseDirectory / c_outputDocumentationFolderName;
    var documentationSandcastleProjectFile = directories.DocumentationBaseDirectory / c_documentationProjectFileName;
    var currentAssembly = typeof(BaseBuild).Assembly;
    using (var sandcastleProjectTemplateStream = currentAssembly.GetManifestResourceStream(c_sandcastleProjectTemplateEmbeddedResourceName))
    using (var sandcastleProjectFile = File.Open(documentationSandcastleProjectFile, FileMode.OpenOrCreate))
    {
      sandcastleProjectTemplateStream!.CopyTo(sandcastleProjectFile);
    }

    using (var contentLayoutStream = currentAssembly.GetManifestResourceStream(c_contentLayoutEmbeddedResourceName))
    using (var contentLayoutFile = File.Open(directories.DocumentationBaseDirectory / c_contentLayoutFileName, FileMode.OpenOrCreate))
    {
      contentLayoutStream!.CopyTo(contentLayoutFile);
    }

    FileSystemTasks.CopyFile(assemblyMetadata.DocumentationRootPage, directories.DocumentationBaseDirectory / c_gettingStartedFileName);
    var namespaceSummaryFiles = Glob.Files(
            directories.Solution,
            assemblyMetadata.DocumentationNamespaceSummaryFiles)
        .Where(file => !_standardExcludes.Any(file.Contains))
        .ToList();
    var sandcastlePackage = NuGetPackageResolver
        .GetLocalInstalledPackage("EWSoftware.SHFB", ToolPathResolver.NuGetAssetsConfigFile)
        .NotNull("sandcastlePackage != null");
    var documentationProperties = new Dictionary<string, string>
                                  {
                                      { SandcastleProperties.HtmlHelpName, assemblyMetadata.DocumentationFileName },
                                      { SandcastleProperties.OutputPath, documentationCompilationOutputDirectory },
                                      { SandcastleProperties.FooterText, $"<![CDATA[<p>Version: {semanticVersion.Version}</p>]]>" },
                                      { SandcastleProperties.HelpTitle, assemblyMetadata.ProductName },
                                      { SandcastleProperties.ProductTitle, assemblyMetadata.ProductName },
                                      { SandcastleProperties.CopyrightHref, assemblyMetadata.CompanyUrl },
                                      { SandcastleProperties.CopyrightText, assemblyMetadata.Copyright },
                                      { SandcastleProperties.HelpFileVersion, semanticVersion.AssemblyVersion },
                                      { SandcastleProperties.VendorName, assemblyMetadata.CompanyName },
                                      {
                                          SandcastleProperties.BuildLogFile,
                                          $"{directories.Log}Sandcastle.{assemblyMetadata.DocumentationFileName}.log"
                                      },
                                      { SandcastleProperties.WorkingPath, directories.DocumentationBaseDirectory / "Working" },
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
    return @$"{documentationCompilationOutputDirectory}\{assemblyMetadata.DocumentationFileName}.chm";
  }

  internal static void CreateDocumentationNugetPackage (
      IReadOnlyCollection<ProjectMetadata> projects,
      string documentOutputFile,
      AssemblyMetadata assemblyMetadata,
      SemanticVersion semanticVersion,
      Directories directories)
  {
    var documentationProjects = projects.Where(projectMetadata => projectMetadata.IsDocumentationFile).ToList();
    if (documentationProjects.Count == 0)
      Log.Warning("No documentation projects found!");

    documentationProjects.ForEach(projectFile =>
    {
      PrepareDocumentationProjectFile(projectFile, documentOutputFile, assemblyMetadata);
      try
      {
        NugetTask.GenerateSinglePackageWithDebugSymbols(
            projectFile,
            semanticVersion,
            assemblyMetadata,
            directories);
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

  private const string c_contentLayoutFileName = "ContentLayout.content";
  private const string c_gettingStartedFileName = "GettingStarted.aml";
  private const string c_contentLayoutEmbeddedResourceName = "Remotion.BuildScript.DocumentationFiles.ContentLayout.content";
  private const string c_sandcastleProjectTemplateEmbeddedResourceName = "Remotion.BuildScript.DocumentationFiles.SandcastleProjectTemplate.shfbproj";
  private const string c_documentationProjectFileName = "documentation.shfbproj";
  private const string c_outputDocumentationFolderName = "Output";

  private static readonly string[] _filterSpec =
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

  private static readonly string[] _standardExcludes =
  {
      ".git",
      ".svn",
      "_svn"
  };

  private static void RestoreDocumentationProjectFile (ProjectMetadata projectFile)
  {
    FileSystemTasks.CopyFile($"{projectFile.ProjectPath}.backup", projectFile.ProjectPath, FileExistsPolicy.Overwrite);
    FileSystemTasks.DeleteFile($"{projectFile.ProjectPath}.backup");
  }

  private static void PrepareDocumentationProjectFile (
      ProjectMetadata projectFile,
      string documentOutputFile,
      AssemblyMetadata assemblyMetadata)
  {
    FileSystemTasks.CopyFile(projectFile.ProjectPath, $"{projectFile.ProjectPath}.backup", FileExistsPolicy.Overwrite);

    var xmlDocument = new XmlDocument();
    xmlDocument.Load(projectFile.ProjectPath);

    var projectNode = xmlDocument.SelectSingleNode("/Project");
    var itemGroupNode = xmlDocument.CreateElement("ItemGroup");
    var noneNode = xmlDocument.CreateElement("None");
    noneNode.SetAttribute("Pack", "True");
    noneNode.SetAttribute("Include", documentOutputFile);
    var copyToOutputNode = xmlDocument.CreateElement("CopyToOutputDirectory");
    copyToOutputNode.InnerText = "PreserveNewest";
    var packagePathNode = xmlDocument.CreateElement("PackagePath");
    packagePathNode.InnerText = $"doc/{assemblyMetadata.DocumentationFileName}.chm";
    noneNode.AppendChild(packagePathNode);
    noneNode.AppendChild(copyToOutputNode);
    itemGroupNode.AppendChild(noneNode);
    projectNode!.AppendChild(itemGroupNode);

    xmlDocument.Save(projectFile.ProjectPath);
  }

  private static void PrepareSandcastleProjectFile (string filePath, Dictionary<string, string> properties)
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

  private static IReadOnlyCollection<ProjectMetadata> PrepareProjectMetadata (IReadOnlyCollection<ProjectMetadata> projectMetadataList)
  {
    return projectMetadataList.SelectMany(
        projectMetadata =>
            projectMetadata.TargetFrameworks.Select(
                targetFramework =>
                    CreateProjectMetadataByTargetFramework(projectMetadata, targetFramework))
    ).ToList();
  }

  private static ProjectMetadata CreateProjectMetadataByTargetFramework (ProjectMetadata projectMetadata, string targetFramework)
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
