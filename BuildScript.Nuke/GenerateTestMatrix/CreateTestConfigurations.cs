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
using System.Linq;
using System.Text.RegularExpressions;
using Nuke.Common;
using Nuke.Common.Utilities.Collections;
using Serilog;

namespace Remotion.BuildScript.GenerateTestMatrix;

public class CreateTestConfigurations
{
  private const string c_testConfigurationMetadataName = "TestConfiguration";
  private const string c_testSetupBuildFileMetadataName = "TestSetupBuildFile";
  private static readonly MetadataValueCollection s_supportedConfigurationIDs = new(new[] { "debug", "release" });
  private readonly IReadOnlyCollection<string> _enabledBrowsers;
  private readonly IReadOnlyCollection<string> _enabledConfigurationIDs;
  private readonly IReadOnlyCollection<string> _enabledDatabaseSystems;
  private readonly IReadOnlyCollection<string> _enabledExecutionRuntimes;
  private readonly IReadOnlyCollection<string> _enabledPlatforms;
  private readonly IReadOnlyCollection<string> _enabledTargetRuntimes;
  private readonly IReadOnlyCollection<string> _supportedBrowsers;
  private readonly IReadOnlyCollection<string> _supportedDatabaseSystems;
  private readonly IReadOnlyCollection<string> _supportedExecutionRuntimes;
  private readonly IReadOnlyCollection<string> _supportedPlatforms;
  private readonly IReadOnlyCollection<string> _supportedTargetRuntimes;

  public readonly IReadOnlyCollection<TestProjectMetadata> _testOutputFiles;

  public CreateTestConfigurations (
      IReadOnlyCollection<string> enabledBrowsers,
      IReadOnlyCollection<string> enabledConfigurationIDs,
      IReadOnlyCollection<string> enabledDatabaseSystems,
      IReadOnlyCollection<string> enabledExecutionRuntimes,
      IReadOnlyCollection<string> enabledPlatforms,
      IReadOnlyCollection<string> enabledTargetRuntimes,
      IReadOnlyCollection<string> supportedBrowsers,
      IReadOnlyCollection<string> supportedDatabaseSystems,
      IReadOnlyCollection<string> supportedExecutionRuntimes,
      IReadOnlyCollection<string> supportedPlatforms,
      IReadOnlyCollection<string> supportedTargetRuntimes,
      IReadOnlyCollection<TestProjectMetadata> testOutputFiles)
  {
    _enabledBrowsers = enabledBrowsers;
    _enabledConfigurationIDs = enabledConfigurationIDs;
    _enabledDatabaseSystems = enabledDatabaseSystems;
    _enabledExecutionRuntimes = enabledExecutionRuntimes;
    _enabledPlatforms = enabledPlatforms;
    _enabledTargetRuntimes = enabledTargetRuntimes;
    _supportedBrowsers = supportedBrowsers;
    _supportedDatabaseSystems = supportedDatabaseSystems;
    _supportedExecutionRuntimes = supportedExecutionRuntimes;
    _supportedPlatforms = supportedPlatforms;
    _supportedTargetRuntimes = supportedTargetRuntimes;
    _testOutputFiles = testOutputFiles;
  }

  public TestConfiguration[] CreateTestMatrix ()
  {
    var supportedBrowsers = GetMetadataValueCollection(_supportedBrowsers, MetadataValueConstants.NoBrowser);
    var supportedDatabaseSystems = GetMetadataValueCollection(_supportedDatabaseSystems, MetadataValueConstants.NoDB);
    var supportedPlatforms = GetMetadataValueCollection(_supportedPlatforms);
    var supportedExecutionRuntimes = GetMetadataValueDictionary(
        _supportedExecutionRuntimes,
        MetadataValueConstants.EnforcedLocalMachine,
        MetadataValueConstants.LocalMachine);
    var supportedTargetRuntimes = GetMetadataValueCollection(_supportedTargetRuntimes);

    var testConfigurationsFactory = new TestConfigurationsFactory(
        supportedBrowsers,
        supportedDatabaseSystems,
        supportedPlatforms,
        s_supportedConfigurationIDs,
        supportedExecutionRuntimes,
        supportedTargetRuntimes);

    var enabledBrowsers = GetMetadataValueCollection(_enabledBrowsers);
    var enabledDatabaseSystems = GetMetadataValueCollection(_enabledDatabaseSystems);
    var enabledPlatforms = GetMetadataValueCollection(_enabledPlatforms);
    var enabledExecutionRuntimes = GetMetadataValueCollection(_enabledExecutionRuntimes);
    var enabledTargetRuntimes = GetMetadataValueCollection(_enabledTargetRuntimes);
    var enabledConfigurationIDs = GetMetadataValueCollection(_enabledConfigurationIDs);

    var testConfigurationsFilter = new TestConfigurationsFilter(
        supportedBrowsers,
        enabledBrowsers,
        supportedDatabaseSystems,
        enabledDatabaseSystems,
        supportedPlatforms,
        enabledPlatforms,
        supportedExecutionRuntimes,
        enabledExecutionRuntimes,
        supportedTargetRuntimes,
        enabledTargetRuntimes,
        s_supportedConfigurationIDs,
        enabledConfigurationIDs);

    try
    {
      var tempTestOutputFiles = PrepareTestProjectMetadataInput(_testOutputFiles);
      var output = tempTestOutputFiles
          .SelectMany(inputItem => CreateOutputTestConfigurations(inputItem, testConfigurationsFactory, testConfigurationsFilter)).ToArray();
      return output;
    }
    catch (InvalidOperationException ex)
    {
      Log.Logger.Error(ex.Message);
      Assert.Fail("Error during CreateTestConfiguration", ex);
    }
    catch (ArgumentException ex)
    {
      Log.Logger.Error(ex.Message);
      Assert.Fail("Error during CreateTestConfiguration", ex);
    }

    return Array.Empty<TestConfiguration>();
  }

  private IReadOnlyCollection<TestConfiguration> CreateOutputTestConfigurations (
      TestProjectMetadata testProjectMetadata,
      TestConfigurationsFactory testConfigurationsFactory,
      TestConfigurationsFilter testConfigurationsFilter)
  {
    var testAssemblyFullPath = testProjectMetadata.AssemblyPaths.Single();
    var testConfigurationsMetadataValue = testProjectMetadata.TestConfiguration;

    if (string.IsNullOrEmpty(testConfigurationsMetadataValue))
      throw new InvalidOperationException($"Could not find item metadata 'TestConfiguration' for '{testAssemblyFullPath}'.");

    var rawTestConfigurations = testConfigurationsMetadataValue
        .Split(';')
        .Select(x => x.Trim())
        .Where(x => x != "")
        .ToArray();

    var unfilteredTestConfigurations = testConfigurationsFactory.CreateTestConfigurations(testProjectMetadata, rawTestConfigurations);
    var tmpUnfilteredTestConfigurations = unfilteredTestConfigurations.Where(testConfig =>
        testConfig.ConfigurationID == testProjectMetadata.Configuration.ToLower() &&
        testConfig.IsNetCoreExecutable ^ testConfig.IsNetFramework
    ).ToArray();
    var filteredTestConfigurations = testConfigurationsFilter.GetFilteredTestConfigurations(
        tmpUnfilteredTestConfigurations);

    return filteredTestConfigurations;
  }

  private IReadOnlyCollection<TestProjectMetadata> PrepareTestProjectMetadataInput (IReadOnlyCollection<TestProjectMetadata> projectMetadataList)
  {
    return projectMetadataList.SelectMany(
        projectMetadata =>
            projectMetadata.TargetFrameworks.Select(
                targetFramework => CreateTestProjectMetadataByTargetFramework(projectMetadata, targetFramework))
    ).ToList();
  }

  private TestProjectMetadata CreateTestProjectMetadataByTargetFramework (TestProjectMetadata projectMetadata, string targetFramework)
  {
    return new TestProjectMetadata
           {
               Configuration = projectMetadata.Configuration,
               ProjectPath = projectMetadata.ProjectPath,
               ToolsVersion = projectMetadata.ToolsVersion,
               IsMultiTargetFramework = projectMetadata.IsMultiTargetFramework,
               IsSdkProject = projectMetadata.IsSdkProject,
               AssemblyPaths = new List<string>
                               {
                                   projectMetadata.AssemblyPaths.Single(
                                       x =>
                                           !projectMetadata.IsMultiTargetFramework || x.Contains(targetFramework))
                               },
               TestConfiguration = projectMetadata.TestConfiguration,
               TestSetupBuildFile = projectMetadata.TestSetupBuildFile,
               TargetFrameworks = new List<string> { targetFramework }
           };
  }

  private MetadataValueCollection GetMetadataValueCollection (IReadOnlyCollection<string> taskItems, params string[] valuesToAppend)
  {
    var collection = taskItems.Select(x => x).Concat(valuesToAppend).ToArray();

    return new MetadataValueCollection(collection);
  }

  private MetadataValueDictionary GetMetadataValueDictionary (IReadOnlyCollection<string> taskItems, params string[] valuesToAppend)
  {
    var itemSpecs = taskItems.Select(x => x).ToArray();

    foreach (var itemSpec in itemSpecs)
      if (!Regex.IsMatch(itemSpec, "\\S+=\\S"))
        throw new FormatException($"The execution runtime key value pair '{itemSpec}' is malformed.");

    var splitItemSpecs = itemSpecs.Select(x => x.Split(new[] { "=" }, StringSplitOptions.RemoveEmptyEntries)).ToList();

    var duplicateKeys = splitItemSpecs.Select(x => x[0]).GetDuplicateValues(StringComparer.OrdinalIgnoreCase).ToList();
    if (duplicateKeys.Any())
    {
      var jointDuplicateValues = string.Join(",", duplicateKeys.Select(x => $"'{x}'"));
      throw new ArgumentException($"Duplicate keys found in dictionary: {jointDuplicateValues}.", nameof(taskItems));
    }

    var dictionary = splitItemSpecs.ToDictionary(split => split[0], split => split[1], StringComparer.OrdinalIgnoreCase);

    foreach (var value in valuesToAppend)
      dictionary.Add(value, value);

    return new MetadataValueDictionary(dictionary);
  }
}