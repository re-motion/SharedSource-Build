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
using System.Linq;
using GlobExpressions;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.NuGet;
using Nuke.Common.Utilities;

namespace Remotion.BuildScript.Components.Tasks;

internal static class NugetTask
{
  internal static void GenerateSinglePackageWithDebugSymbols (
      ProjectMetadata projectFile,
      SemanticVersion semanticVersion,
      AssemblyMetadata assemblyMetadata,
      Directories directories)
  {
    var nugetOutputDirectory = directories.Output / c_nugetWithDebugSymbolsFolderName / projectFile.Configuration;
    FileSystemTasks.EnsureExistingDirectory(nugetOutputDirectory);
    if (projectFile.IsSdkProject)
    {
      GeneratePackagesForSdkProjectWithDebugSymbols(
          projectFile,
          semanticVersion,
          assemblyMetadata,
          directories,
          nugetOutputDirectory);
    }
    else
    {
      GeneratePackagesForNonSdkProjectWithDebugSymbols(projectFile,
          semanticVersion,
          assemblyMetadata,
          directories,
          nugetOutputDirectory);
    }
  }

  internal static void GenerateSinglePackageWithSymbolServerSupport (
      ProjectMetadata projectFile,
      SemanticVersion semanticVersion,
      AssemblyMetadata assemblyMetadata,
      Directories directories)
  {
    var nugetOutputDirectory = directories.Output / c_nugetWithSymbolServerSupportFolderName / projectFile.Configuration;
    FileSystemTasks.EnsureExistingDirectory(nugetOutputDirectory);
    if (projectFile.IsSdkProject)
    {
      GeneratePackagesForSdkProjectWithSymbolServerSupport(
          projectFile,
          semanticVersion,
          assemblyMetadata,
          directories,
          nugetOutputDirectory);
    }
    else
    {
      GeneratePackagesForNonSdkProjectWithSymbolServerSupport(projectFile,
          semanticVersion,
          assemblyMetadata,
          directories,
          nugetOutputDirectory);
    }
  }

  private const string c_symbolTmpZipFolderName = "symbolTmp";
  private const string c_symbolsNupkgFileExtensionFilter = "*.symbols.nupkg";
  private const string c_symbolsNupkgFileExtension = ".symbols.nupkg";
  private const string c_nupkgFileExtensionFilter = "*.nupkg";
  private const string c_srcFolderName = "src";
  private const string c_nugetWithSymbolServerSupportFolderName = "NuGetWithSymbolServerSupport";
  private const string c_nugetWithDebugSymbolsFolderName = "NuGetWithDebugSymbols";

  private static void GeneratePackagesForSdkProjectWithDebugSymbols (
      ProjectMetadata projectFile,
      SemanticVersion semanticVersion,
      AssemblyMetadata assemblyMetadata,
      Directories directories,
      AbsolutePath nugetOutputDirectoryPath)
  {
    DotNetTasks.DotNetPack(
        settings => SetBaseDotNetPackSettings(
                projectFile,
                semanticVersion,
                assemblyMetadata,
                directories,
                nugetOutputDirectoryPath,
                settings)
            .DisableIncludeSource()
            .DisableIncludeSymbols()
            .SetProperty(MSBuildProperties.IncludeReferenced, "True")
    );
  }

  private static void GeneratePackagesForSdkProjectWithSymbolServerSupport (
      ProjectMetadata projectFile,
      SemanticVersion semanticVersion,
      AssemblyMetadata assemblyMetadata,
      Directories directories,
      AbsolutePath nugetOutputDirectoryPath)
  {
    DotNetTasks.DotNetPack(
        settings => SetBaseDotNetPackSettings(
                projectFile,
                semanticVersion,
                assemblyMetadata,
                directories,
                nugetOutputDirectoryPath,
                settings)
            .EnableIncludeSource()
            .EnableIncludeSymbols()
            .SetSymbolPackageFormat(DotNetSymbolPackageFormat.snupkg)
    );
  }

  private static void GeneratePackagesForNonSdkProjectWithDebugSymbols (
      ProjectMetadata projectFile,
      SemanticVersion semanticVersion,
      AssemblyMetadata assemblyMetadata,
      Directories directories,
      AbsolutePath nugetOutputDirectoryPath)
  {
    NuGetTasks.NuGetPack(
        settings => SetBaseNuGetPackSettings(
            projectFile,
            semanticVersion,
            assemblyMetadata,
            directories,
            nugetOutputDirectoryPath,
            settings)
    );
    RemoveSrcFolder(projectFile, nugetOutputDirectoryPath, semanticVersion);
  }

  private static void GeneratePackagesForNonSdkProjectWithSymbolServerSupport (
      ProjectMetadata projectFile,
      SemanticVersion semanticVersion,
      AssemblyMetadata assemblyMetadata,
      Directories directories,
      AbsolutePath nugetOutputDirectoryPath)
  {
    NuGetTasks.NuGetPack(
        settings => SetBaseNuGetPackSettings(
                projectFile,
                semanticVersion,
                assemblyMetadata,
                directories,
                nugetOutputDirectoryPath,
                settings)
            .SetSymbolPackageFormat(NuGetSymbolPackageFormat.snupkg)
    );
  }

  private static DotNetPackSettings SetBaseDotNetPackSettings (
      ProjectMetadata projectFile,
      SemanticVersion semanticVersion,
      AssemblyMetadata assemblyMetadata,
      Directories directories,
      AbsolutePath nugetOutputDirectoryPath,
      DotNetPackSettings settings) =>
      settings
          .SetProject(projectFile.ProjectPath)
          .SetConfiguration(projectFile.Configuration)
          .SetVersion(semanticVersion.AssemblyNuGetVersion)
          .SetOutputDirectory(nugetOutputDirectoryPath)
          .SetProperty(MSBuildProperties.ExtraTags, $"{projectFile.Configuration}Build ")
          .SetProperty(MSBuildProperties.CompanyName, assemblyMetadata.CompanyName)
          .SetProperty(MSBuildProperties.CompanyUrl, assemblyMetadata.CompanyUrl)
          .SetProperty(MSBuildProperties.ProductName, assemblyMetadata.ProductName)
          .SetAuthors(assemblyMetadata.CompanyName)
          .SetCopyright(assemblyMetadata.Copyright.EscapeMSBuild().Replace(",", "%2C"))
          .SetProperty(MSBuildProperties.AssemblyOriginatorKeyFile, directories.SolutionKeyFile)
          .EnableNoRestore()
          .EnableNoBuild();

  private static NuGetPackSettings SetBaseNuGetPackSettings (
      ProjectMetadata projectFile,
      SemanticVersion semanticVersion,
      AssemblyMetadata assemblyMetadata,
      Directories directories,
      AbsolutePath nugetOutputDirectoryPath,
      NuGetPackSettings settings) =>
      settings
          .SetTargetPath(projectFile.ProjectPath)
          .SetConfiguration(projectFile.Configuration)
          .SetVersion(semanticVersion.AssemblyNuGetVersion)
          .SetOutputDirectory(nugetOutputDirectoryPath)
          .EnableIncludeReferencedProjects()
          .EnableSymbols()
          .DisableBuild()
          .SetProperty(MSBuildProperties.ExtraTags, $"{projectFile.Configuration}Build ")
          .SetProperty(MSBuildProperties.Copyright, assemblyMetadata.Copyright)
          .SetProperty(MSBuildProperties.CompanyName, assemblyMetadata.CompanyName)
          .SetProperty(MSBuildProperties.CompanyUrl, assemblyMetadata.CompanyUrl)
          .SetProperty(MSBuildProperties.ProductName, assemblyMetadata.ProductName)
          .SetProperty(MSBuildProperties.Copyright, assemblyMetadata.Copyright.EscapeMSBuild().EscapeBraces())
          .SetProperty(MSBuildProperties.Authors, assemblyMetadata.CompanyName)
          .SetProperty(MSBuildProperties.AssemblyOriginatorKeyFile, directories.SolutionKeyFile);

  private static void RemoveSrcFolder (ProjectMetadata projectFile, AbsolutePath nugetOutputDirectoryPath, SemanticVersion semanticVersion)
  {
    var zipTempFolderPath = nugetOutputDirectoryPath / c_symbolTmpZipFolderName;
    var symbolFile = Glob.Files(nugetOutputDirectoryPath, c_symbolsNupkgFileExtensionFilter)
        .Single(x => x.ToLower().Contains(projectFile.ProjectPath.NameWithoutExtension.ToLower()));
    var nugetPackFile = Glob.Files(nugetOutputDirectoryPath, c_nupkgFileExtensionFilter).Single(
        file =>
            !file.Contains(c_symbolsNupkgFileExtension) &&
            file.ToLower().Contains($"{projectFile.ProjectPath.NameWithoutExtension.ToLower()}.{semanticVersion.AssemblyNuGetVersion}"));
    CompressionTasks.UncompressZip(nugetOutputDirectoryPath / symbolFile, zipTempFolderPath);
    FileSystemTasks.DeleteDirectory(zipTempFolderPath / c_srcFolderName);
    FileSystemTasks.DeleteFile(nugetOutputDirectoryPath / symbolFile);
    FileSystemTasks.DeleteFile(nugetOutputDirectoryPath / nugetPackFile);
    CompressionTasks.CompressZip(zipTempFolderPath, nugetOutputDirectoryPath / nugetPackFile);
    FileSystemTasks.DeleteDirectory(zipTempFolderPath);
  }
}
