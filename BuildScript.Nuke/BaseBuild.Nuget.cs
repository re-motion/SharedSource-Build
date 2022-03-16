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
using JetBrains.Annotations;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.NuGet;
using Nuke.Common.Utilities.Collections;

namespace Remotion.BuildScript;

public partial class BaseBuild : NukeBuild
{
  private const string c_symbolTmpZipFolderName = "symbolTmp";
  private const string c_symbolsNupkgFileExtensionFilter = "*.symbols.nupkg";
  private const string c_symbolsNupkgFileExtension = ".symbols.nupkg";
  private const string c_nupkgFileExtensionFilter = "*.nupkg";
  private const string c_srcFolderName = "src";
  private const string c_nugetWithSymbolServerSupportFolderName = "NuGetWithSymbolServerSupport";
  private const string c_nugetWithDebugSymbolsFolderName = "NuGetWithDebugSymbols";
  private const string c_testFolderSuffix = "-Test";

  [PublicAPI]
  public Target GenerateNuGetPackagesWithDebugSymbols => _ => _
      .DependsOn(ReadConfiguration, CompileReleaseBuild, CompileTestBuild)
      .Description("Generate nuget packages with debug symbols")
      .OnlyWhenStatic(() => !SkipNuGet)
      .Executes(() =>
      {
        ReleaseProjectFiles.ForEach(projectFile =>
        {
          GenerateSinglePackageWithDebugSymbols(projectFile);
        });
      });

  [PublicAPI]
  public Target GenerateNuGetPackagesWithSymbolServerSupport => _ => _
      .DependsOn(ReadConfiguration, CompileReleaseBuild, CompileTestBuild)
      .Description("Generate nuget packages with symbol server support")
      .OnlyWhenStatic(() => !SkipNuGetOrg)
      .Executes(() =>
      {
        ReleaseProjectFiles.ForEach(projectFile =>
        {
          GenerateSinglePackageWithSymbolServerSupport(projectFile);
        });
      });

  private void GenerateSinglePackageWithDebugSymbols (ProjectMetadata projectFile, string folderSuffix = "")
  {
    Configuration.ForEach(
        config =>
        {
          var nugetOutputDirectory = Directories.Output / c_nugetWithDebugSymbolsFolderName / (config + folderSuffix);
          FileSystemTasks.EnsureExistingDirectory(nugetOutputDirectory);
          if (projectFile.IsSdkProject)
            GeneratePackagesForSdkProjectWithDebugSymbols(projectFile, config, nugetOutputDirectory);
          else
            GeneratePackagesForNonSdkProjectWithDebugSymbols(projectFile, config, nugetOutputDirectory);
        });
  }

  private void GenerateSinglePackageWithSymbolServerSupport (
      ProjectMetadata projectFile,
      string folderSuffix = "")
  {
    Configuration.ForEach(
        config =>
        {
          var nugetOutputDirectory = Directories.Output / c_nugetWithSymbolServerSupportFolderName / (config + folderSuffix);
          FileSystemTasks.EnsureExistingDirectory(nugetOutputDirectory);
          if (projectFile.IsSdkProject)
            GeneratePackagesForSdkProjectWithSymbolServerSupport(projectFile, config, nugetOutputDirectory);
          else
            GeneratePackagesForNonSdkProjectWithSymbolServerSupport(projectFile, config, nugetOutputDirectory);
        });
  }

  private void GeneratePackagesForSdkProjectWithDebugSymbols (ProjectMetadata projectFile, string config, AbsolutePath nugetOutputDirectoryPath)
  {
    DotNetTasks.DotNetPack(
        settings => SetBaseDotNetPackSettings(projectFile, config, nugetOutputDirectoryPath, settings)
            .DisableIncludeSource()
            .DisableIncludeSymbols()
            .SetProperty(MSBuildProperties.IncludeReferenced, "True")
    );
  }

  private void GeneratePackagesForSdkProjectWithSymbolServerSupport (
      ProjectMetadata projectFile,
      string config,
      AbsolutePath nugetOutputDirectoryPath)
  {
    DotNetTasks.DotNetPack(
        settings =>
            SetBaseDotNetPackSettings(projectFile, config, nugetOutputDirectoryPath, settings)
                .EnableIncludeSource()
                .EnableIncludeSymbols()
                .SetSymbolPackageFormat(DotNetSymbolPackageFormat.snupkg)
    );
  }

  private void GeneratePackagesForNonSdkProjectWithDebugSymbols (ProjectMetadata projectFile, string config, AbsolutePath nugetOutputDirectoryPath)
  {
    NuGetTasks.NuGetPack(
        settings => SetBaseNuGetPackSettings(projectFile, config, nugetOutputDirectoryPath, settings)
    );
    RemoveSrcFolder(projectFile, nugetOutputDirectoryPath);
  }

  private void GeneratePackagesForNonSdkProjectWithSymbolServerSupport (
      ProjectMetadata projectFile,
      string config,
      AbsolutePath nugetOutputDirectoryPath)
  {
    NuGetTasks.NuGetPack(
        settings =>
            SetBaseNuGetPackSettings(projectFile, config, nugetOutputDirectoryPath, settings)
                .SetSymbolPackageFormat(NuGetSymbolPackageFormat.snupkg)
    );
  }

  private DotNetPackSettings SetBaseDotNetPackSettings (
      ProjectMetadata projectFile,
      string config,
      AbsolutePath nugetOutputDirectoryPath,
      DotNetPackSettings settings) =>
      settings
          .SetProject(projectFile.ProjectPath)
          .SetConfiguration(config)
          .SetVersion(SemanticVersion.AssemblyNuGetVersion)
          .SetOutputDirectory(nugetOutputDirectoryPath)
          .SetProperty(MSBuildProperties.ExtraTags, $"{config}Build ")
          .SetProperty(MSBuildProperties.CompanyName, AssemblyMetadata.CompanyName)
          .SetProperty(MSBuildProperties.CompanyUrl, AssemblyMetadata.CompanyUrl)
          .SetProperty(MSBuildProperties.ProductName, AssemblyMetadata.ProductName)
          .SetProperty(MSBuildProperties.AssemblyOriginatorKeyFile, Directories.SolutionKeyFile);

  private NuGetPackSettings SetBaseNuGetPackSettings (
      ProjectMetadata projectFile,
      string config,
      AbsolutePath nugetOutputDirectoryPath,
      NuGetPackSettings settings) =>
      settings
          .SetTargetPath(projectFile.ProjectPath)
          .SetConfiguration(config)
          .SetVersion(SemanticVersion.AssemblyNuGetVersion)
          .SetOutputDirectory(nugetOutputDirectoryPath)
          .SetIncludeReferencedProjects(true)
          .SetSymbols(true)
          .SetProperty(MSBuildProperties.ExtraTags, $"{config}Build ")
          .SetProperty(MSBuildProperties.Copyright, AssemblyMetadata.Copyright)
          .SetProperty(MSBuildProperties.CompanyName, AssemblyMetadata.CompanyName)
          .SetProperty(MSBuildProperties.CompanyUrl, AssemblyMetadata.CompanyUrl)
          .SetProperty(MSBuildProperties.ProductName, AssemblyMetadata.ProductName)
          .SetProperty(MSBuildProperties.AssemblyOriginatorKeyFile, Directories.SolutionKeyFile);

  private void RemoveSrcFolder (ProjectMetadata projectFile, AbsolutePath nugetOutputDirectoryPath)
  {
    var zipTempFolderPath = nugetOutputDirectoryPath / c_symbolTmpZipFolderName;
    var symbolFile = Glob.Files(nugetOutputDirectoryPath, c_symbolsNupkgFileExtensionFilter)
        .Single(x => x.ToLower().Contains(projectFile.ProjectPath.NameWithoutExtension.ToLower()));
    var nugetPackFile = Glob.Files(nugetOutputDirectoryPath, c_nupkgFileExtensionFilter).Single(
        file =>
            !file.Contains(c_symbolsNupkgFileExtension) &&
            file.ToLower().Contains($"{projectFile.ProjectPath.NameWithoutExtension.ToLower()}.{SemanticVersion.AssemblyNuGetVersion}"));
    CompressionTasks.UncompressZip(nugetOutputDirectoryPath / symbolFile, zipTempFolderPath);
    FileSystemTasks.DeleteDirectory(zipTempFolderPath / c_srcFolderName);
    FileSystemTasks.DeleteFile(nugetOutputDirectoryPath / symbolFile);
    FileSystemTasks.DeleteFile(nugetOutputDirectoryPath / nugetPackFile);
    CompressionTasks.CompressZip(zipTempFolderPath, nugetOutputDirectoryPath / nugetPackFile);
    FileSystemTasks.DeleteDirectory(zipTempFolderPath);
  }
}