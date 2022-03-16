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
using GlobExpressions;
using JetBrains.Annotations;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.Docker;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Utilities.Collections;
using Remotion.BuildScript.GenerateTestMatrix;
using Serilog;

namespace Remotion.BuildScript;

public partial class BaseBuild : NukeBuild
{
  private readonly TestArgumentHelper _testArgumentHelper = new();

  [Parameter("Path to MSBuildExtensionPack for running the TestSetupBuildFile")]
  protected string MSBuildExtensionPackPath { get; } = "";

  [PublicAPI]
  public Target CleanFolders => _ => _
      .Description("Remove build output, log and temp folders")
      .Executes(() =>
      {
        FileSystemTasks.DeleteDirectory(Directories.Output);
        FileSystemTasks.DeleteDirectory(Directories.Log);
        FileSystemTasks.DeleteDirectory(Directories.Temp);
      });

  [PublicAPI]
  public Target RunTests => _ => _
      .DependsOn(CompileTestBuild, CleanFolders)
      .Description("Run all tests")
      .OnlyWhenStatic(() => !SkipTests)
      .Executes(() =>
          {
            var createTestConfigurations = new CreateTestConfigurations
            (
                supportedBrowsers: SupportedBrowsers,
                supportedPlatforms: SupportedPlatforms,
                supportedDatabaseSystems: SupportedDatabaseSystems,
                supportedExecutionRuntimes: SupportedExecutionRuntimes,
                supportedTargetRuntimes: SupportedTargetRuntimes,
                enabledBrowsers: Browsers,
                enabledDatabaseSystems: DatabaseSystems,
                enabledTargetRuntimes: TargetRuntimes,
                enabledExecutionRuntimes: ExecutionRuntimes,
                enabledPlatforms: Platforms,
                enabledConfigurationIDs: Configuration,
                testOutputFiles: TestProjectFiles
            );
            var output = createTestConfigurations.CreateTestMatrix();
            if (output.Length == 0)
            {
              Log.Warning("The created test matrix from the test configuration is empty!");
              return;
            }

            LogTestMatrix(output);

            var dockerTestConfigs = output.Where(config => config.ExecutionRuntime.UseDocker).ToList();
            PrepareDockerImages(dockerTestConfigs);

            FileSystemTasks.EnsureExistingDirectory(Directories.Log);
            Log.Information("Start running tests");
            var beforeTestTime = DateTime.Now;
            var outputResult = output.Select(RunTest).ToList();
            if (outputResult.Any(testConfig => testConfig == -1))
              Assert.Fail($"{outputResult.Sum(x => x == -1 ? 1 : 0)} Test(s) had errors!");
            if (outputResult.Any(testConfig => testConfig != 0))
              Assert.Fail($"{outputResult.Sum(x => x > 0 ? x : 0)} Test(s) failed!");
            var runTestTime = DateTime.Now - beforeTestTime;
            Log.Information($"Test run time: {runTestTime}");
          }
      );

  private void LogTestMatrix (TestConfiguration[] output)
  {
    Log.Information("Test Matrix");
    output.ForEach(testConfig =>
    {
      Log.Information(testConfig.ToString());
    });
  }

  private void PrepareDockerImages (IReadOnlyCollection<TestConfiguration> testConfigs)
  {
    Log.Information("Started pulling Docker Images for tests supporting docker");
    testConfigs.ForEach(testConfig =>
    {
      DockerTasks.DockerPull(configurator => configurator
          .SetName(testConfig.ExecutionRuntime.DockerImage)
      );
    });
    Log.Information("Finished pulling Docker Images for tests supporting docker");
  }

  private int RunTest (TestConfiguration testConfig)
  {
    if (!Glob.Files(testConfig.TestAssemblyDirectoryPath, "*TestAdapter.dll").Any())
      Assert.Fail(
          $"While testing {testConfig.TestAssemblyFullPath} no TestAdapter has been found. Please install the corresponding nuget package for your test framework.");

    if (!string.IsNullOrEmpty(testConfig.TestSetupBuildFile))
      MSBuildTasks.MSBuild(s => s
          .SetTargetPath(testConfig.TestSetupBuildFile)
          .SetProperty("MSBuildExtensionPackPath", MSBuildExtensionPackPath)
          .SetProperty("BuildInParallel", false)
          .SetProperty("LogDirectory", Directories.Log)
          .SetTargetPlatform(testConfig.Platform)
          .SetProperty("AppConfigFile", $"{testConfig.TestAssemblyFullPath}.config")
          .SetProperty("DatabaseSystem", testConfig.DatabaseSystem)
          .SetProperty("Browser", testConfig.Browser)
          .SetProperty("DockerImage", testConfig.ExecutionRuntime.DockerImage)
      );

    var mergedTestCategoriesToExclude = testConfig.ExcludeCategories;
    var mergedTestCategoriesToInclude = testConfig.IncludeCategories;
    var testFilter = _testArgumentHelper.CreateDotNetTestFilter(mergedTestCategoriesToExclude, mergedTestCategoriesToInclude);
    var testName = _testArgumentHelper.CreateTestName(testConfig, mergedTestCategoriesToExclude, mergedTestCategoriesToInclude);
    var testResultOutputPath = $"{Directories.Log / testName}.xml";

    var teamCityLoggerPath = GetTeamCityLoggerPath();

    var dotNetSettings = new Configure<DotNetTestSettings>(settings => settings
        .SetProjectFile(testConfig.TestAssemblyFullPath)
        .AddLoggers($"trx;LogFileName={testResultOutputPath}")
        .SetFilter(testFilter)
        .SetFramework(testConfig.TargetRuntimeMoniker)
        .AddLoggers("teamcity")
        .SetTestAdapterPath(teamCityLoggerPath)
        .SetRunSetting("RunConfiguration.TargetPlatform", testConfig.Platform)
    )(new DotNetTestSettings());
    var exitCode = 0;
    if (testConfig.ExecutionRuntime.UseDocker)
    {
      var dotNetTestRunnerPath = DotNetTasks.DotNetPath;
      var dotNetTestFolderPath = Path.GetDirectoryName(dotNetTestRunnerPath);
      exitCode = RunProcessWithoutExitCodeCheck(CreateDockerRunSettings(
              testConfig,
              dotNetTestRunnerPath,
              dotNetTestFolderPath!,
              dotNetSettings.GetProcessArguments().RenderForExecution()
          ).AddVolume($"\"{teamCityLoggerPath}:{teamCityLoggerPath}\"")
      );
    }
    else
    {
      exitCode = RunProcessWithoutExitCodeCheck(dotNetSettings);
    }

    if (exitCode < 0 || !((AbsolutePath) testResultOutputPath).FileExists())
    {
      Log.Error($"Error {exitCode} occurred while running test: {testConfig.TestAssemblyFullPath}");
      return -1;
    }

    var testTime = "0";
    var startTime = XmlTasks.XmlPeekSingle(testResultOutputPath, "//@start");
    var endTime = XmlTasks.XmlPeekSingle(testResultOutputPath, "//@finish");
    if (!string.IsNullOrEmpty(startTime) && !string.IsNullOrEmpty(endTime))
      testTime = (DateTime.Parse(endTime) - DateTime.Parse(startTime)).ToString();
    var testPassed = XmlTasks.XmlPeekSingle(testResultOutputPath, "//@passed");
    var testFailed = XmlTasks.XmlPeekSingle(testResultOutputPath, "//@failed");
    var testCaseCount = XmlTasks.XmlPeekSingle(testResultOutputPath, "//@total");
    if (testFailed == null && testPassed == null && testCaseCount == null)
      Log.Information(
          $"Tests run {testConfig.TestAssemblyFullPath}: duration {testTime}, tests run {testCaseCount}, tests passed {testPassed}, tests failed {testFailed}");
    return string.IsNullOrEmpty(testFailed) ? -1 : int.Parse(testFailed);
  }

  private AbsolutePath GetTeamCityLoggerPath ()
  {
    var teamcityPackage = NuGetPackageResolver
        .GetLocalInstalledPackage("TeamCity.Dotnet.Integration", ToolPathResolver.NuGetAssetsConfigFile)
        .NotNull("teamcityPackage != null");
    var loggerPath = teamcityPackage.Directory / "build" / "_common" / "vstest15";
    Assert.DirectoryExists(loggerPath);
    return loggerPath;
  }

  private DockerRunSettings CreateDockerRunSettings (TestConfiguration testConfig, string runnerPath, string runnerFolderPath, string arguments)
  {
    return new Configure<DockerRunSettings>(settings => settings
        .EnableRm()
        .AddVolume($"{runnerFolderPath}:{runnerFolderPath}")
        .AddVolume($"{testConfig.TestAssemblyDirectoryPath}:{testConfig.TestAssemblyDirectoryPath}")
        .AddVolume($"{Directories.Log}:{Directories.Log}")
        .SetEntrypoint(runnerPath)
        .SetImage(testConfig.ExecutionRuntime.DockerImage)
        .SetArgs(arguments.Split(" ")
            .Select(param => param.Contains(":\\") ? $"\"{param}\"" : param).ToArray())
    )(new DockerRunSettings());
  }

  private int RunProcessWithoutExitCodeCheck<T> (T settings)
      where T : ToolSettings, new()
  {
    using var process = ProcessTasks.StartProcess(settings);
    process.WaitForExit();
    return process.ExitCode;
  }
}