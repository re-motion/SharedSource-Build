using System;
using System.Linq;
using GlobExpressions;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.NuGet;
using Nuke.Common.Utilities.Collections;

public partial class Build : NukeBuild
{
  private const string c_symbolTmpZipFolderName = "symbolTmp";
  private const string c_symbolsNupkgFileExtensionFilter = "*.symbols.nupkg";
  private const string c_symbolsNupkgFileExtension = ".symbols.nupkg";
  private const string c_nupkgFileExtensionFilter = "*.nupkg";
  private const string c_srcFolderName = "src";
  private const string c_nugetWithSymbolServerSupportFolderName = "NuGetWithSymbolServerSupport";
  private const string c_nugetWithDebugSymbolsFolderName = "NuGetWithDebugSymbols";
  private const string c_testFolderSuffix = "-Test";
  private const string c_extraTagsPropertyKey = "ExtraTags";
  private const string c_includeReferencedProjectsPropertyKey = "IncludeReferencedProjects";
  private const string c_copyrightPropertyKey = "Copyright";


  private Target GenerateNuGetPackagesWithDebugSymbols => _ => _
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

  private Target GenerateNuGetPackagesWithSymbolServerSupport => _ => _
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
          var nugetOutputDirectory = DirectoryHelper.OutputDirectory / c_nugetWithDebugSymbolsFolderName / (config + folderSuffix);
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
          var nugetOutputDirectory = DirectoryHelper.OutputDirectory / c_nugetWithSymbolServerSupportFolderName / (config + folderSuffix);
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
            .SetProperty(c_includeReferencedProjectsPropertyKey, "True")
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
          .SetProperty(c_extraTagsPropertyKey, $"{config}Build ")
          .SetProperty(c_companyNamePropertyKey, AssemblyMetadata.CompanyName)
          .SetProperty(c_companyUrlPropertyKey, AssemblyMetadata.CompanyUrl)
          .SetProperty(c_productNamePropertyKey, AssemblyMetadata.ProductName)
          .SetProperty(c_assemblyOriginatorKeyFilePropertyKey, DirectoryHelper.SolutionKeyFile);

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
          .SetProperty(c_extraTagsPropertyKey, $"{config}Build ")
          .SetProperty(c_copyrightPropertyKey, AssemblyMetadata.Copyright)
          .SetProperty(c_companyNamePropertyKey, AssemblyMetadata.CompanyName)
          .SetProperty(c_companyUrlPropertyKey, AssemblyMetadata.CompanyUrl)
          .SetProperty(c_productNamePropertyKey, AssemblyMetadata.ProductName)
          .SetProperty(c_assemblyOriginatorKeyFilePropertyKey, DirectoryHelper.SolutionKeyFile);

  private static void RemoveSrcFolder (ProjectMetadata projectFile, AbsolutePath nugetOutputDirectoryPath)
  {
    var zipTempFolderPath = nugetOutputDirectoryPath / c_symbolTmpZipFolderName;
    var symbolFile = Glob.Files(nugetOutputDirectoryPath, c_symbolsNupkgFileExtensionFilter)
        .Single(x => x.ToLower().Contains(projectFile.ProjectPath.NameWithoutExtension.ToLower()));
    var nugetPackFile = Glob.Files(nugetOutputDirectoryPath, c_nupkgFileExtensionFilter).Single(
        x =>
            !x.Contains(c_symbolsNupkgFileExtension) && x.ToLower().Contains(projectFile.ProjectPath.NameWithoutExtension.ToLower()));
    CompressionTasks.UncompressZip(nugetOutputDirectoryPath / symbolFile, zipTempFolderPath);
    FileSystemTasks.DeleteDirectory(zipTempFolderPath / c_srcFolderName);
    FileSystemTasks.DeleteFile(nugetOutputDirectoryPath / symbolFile);
    FileSystemTasks.DeleteFile(nugetOutputDirectoryPath / nugetPackFile);
    CompressionTasks.CompressZip(zipTempFolderPath, nugetOutputDirectoryPath / nugetPackFile);
    FileSystemTasks.DeleteDirectory(zipTempFolderPath);
  }
}