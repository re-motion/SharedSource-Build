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
using Nuke.Common;

namespace Remotion.BuildScript.GenerateTestMatrix;

public class TestConfigurationsFactory
{
  private readonly MetadataValueCollection _supportedBrowsers;
  private readonly MetadataValueCollection _supportedConfigurationIDs;
  private readonly MetadataValueCollection _supportedDatabaseSystems;
  private readonly MetadataValueDictionary _supportedExecutionRuntimes;
  private readonly MetadataValueCollection _supportedPlatforms;
  private readonly MetadataValueCollection _supportedTargetRuntimes;

  public TestConfigurationsFactory (
      MetadataValueCollection supportedBrowsers,
      MetadataValueCollection supportedDatabaseSystems,
      MetadataValueCollection supportedPlatforms,
      MetadataValueCollection supportedConfigurationIDs,
      MetadataValueDictionary supportedExecutionRuntimes,
      MetadataValueCollection supportedTargetRuntimes)
  {
    _supportedBrowsers = supportedBrowsers;
    _supportedDatabaseSystems = supportedDatabaseSystems;
    _supportedPlatforms = supportedPlatforms;
    _supportedConfigurationIDs = supportedConfigurationIDs;
    _supportedExecutionRuntimes = supportedExecutionRuntimes;
    _supportedTargetRuntimes = supportedTargetRuntimes;
  }

  public IReadOnlyCollection<TestConfiguration> CreateTestConfigurations (
      TestProjectMetadata projectMetadata,
      IReadOnlyCollection<string> rawTestConfigurations
  )
  {
    return rawTestConfigurations
        .Select(GetConfigurationItems)
        .Select(configurationItems =>
            CreateTestConfiguration(projectMetadata, configurationItems))
        .ToArray();
  }

  private IReadOnlyCollection<string> GetConfigurationItems (string configuration)
  {
    var configurationItems = configuration.Split('+').Select(x => x.Trim()).ToArray();

    CheckNoDuplicateItemsExist(configurationItems);

    return configurationItems;
  }

  private void CheckNoDuplicateItemsExist (IReadOnlyCollection<string> configurationItems)
  {
    var duplicateValues = configurationItems.GetDuplicateValues(StringComparer.OrdinalIgnoreCase).ToList();
    if (duplicateValues.Any())
      throw new ArgumentException(
          $"The following configuration values were found multiple times: '{string.Join(",", duplicateValues)}'");
  }

  private TestConfiguration CreateTestConfiguration (
      TestProjectMetadata projectMetadata,
      IReadOnlyCollection<string> configurationItems
  )
  {
    var testAssemblyFullPath = projectMetadata.AssemblyPaths.Single();
    var testAssemblyFileName = Path.GetFileName(testAssemblyFullPath);
    var testAssemblyDirectoryPath = Path.GetDirectoryName(testAssemblyFullPath);
    if (string.IsNullOrEmpty(testAssemblyDirectoryPath))
      Assert.Fail($"TestAssemblyDirectoryName cannot be found, testAssemblyFullPath = {testAssemblyFullPath}");

    var browser = GetSupportedConfigurationItem(configurationItems, _supportedBrowsers, "browser");
    var databaseSystem = GetSupportedConfigurationItem(configurationItems, _supportedDatabaseSystems, "database system");
    var platform = GetSupportedConfigurationItem(configurationItems, _supportedPlatforms, "platform");
    var targetRuntime = GetTargetRuntime(configurationItems);
    var executionRuntime = GetExecutionRuntime(configurationItems);
    var configurationID = GetSupportedConfigurationItem(configurationItems, _supportedConfigurationIDs, "configuration ID");
    var targetRuntimeInitialFormat = TargetRuntimeConverter.ToTargetFrameworkMonikerFromNUnitFormat(targetRuntime);
    var excludeCategories = GetCategories(configurationItems, TestConfigurationMetadata.ExcludeCategories);
    var excludeCategoriesIDText = string.IsNullOrEmpty(excludeCategories) ? "None" : excludeCategories;
    var includeCategories = GetCategories(configurationItems, TestConfigurationMetadata.IncludeCategories);
    var includeCategoriesIDText = string.IsNullOrEmpty(includeCategories) ? "None" : includeCategories;
    var transformedConfigurationItemsForID = new[]
                                             {
                                                 testAssemblyFileName,
                                                 browser,
                                                 databaseSystem,
                                                 platform,
                                                 executionRuntime.Key,
                                                 targetRuntimeInitialFormat,
                                                 configurationID,
                                                 $"IncludeCategories={includeCategoriesIDText}",
                                                 $"ExcludeCategories={excludeCategoriesIDText}"
                                             };

    return new TestConfiguration(
        string.Join("+", transformedConfigurationItemsForID.Where(x => x != "")),
        browser,
        !string.Equals(browser, MetadataValueConstants.NoBrowser, StringComparison.OrdinalIgnoreCase),
        databaseSystem,
        !string.Equals(databaseSystem, MetadataValueConstants.NoDB, StringComparison.OrdinalIgnoreCase),
        configurationID,
        string.Equals(platform, "x86", StringComparison.OrdinalIgnoreCase),
        platform,
        executionRuntime,
        testAssemblyFileName,
        testAssemblyFullPath,
#pragma warning disable CS8604 // Possible null reference argument. Is checked with Assert.Fail
        testAssemblyDirectoryPath,
#pragma warning restore CS8604 // Possible null reference argument.
        projectMetadata.TestSetupBuildFile,
        targetRuntime,
        TargetRuntimeConverter.ToTargetFrameworkMonikerFromNUnitFormat(targetRuntime),
        excludeCategories,
        includeCategories,
        projectMetadata
    );
  }

  private ExecutionRuntime GetExecutionRuntime (IEnumerable<string> configurationItems)
  {
    var enumeratedConfigurationItems = configurationItems.ToArray();
    var possibleExecutionRuntimes = _supportedExecutionRuntimes.Intersect(enumeratedConfigurationItems).ToList();

    if (possibleExecutionRuntimes.Count == 0)
      throw CreateMissingConfigurationItemException("execution runtime");

    var hasLocalMachine =
        possibleExecutionRuntimes.Remove(KeyValuePair.Create(MetadataValueConstants.LocalMachine, MetadataValueConstants.LocalMachine));
    var hasEnforcedLocalMachine =
        possibleExecutionRuntimes.Remove(
            KeyValuePair.Create(MetadataValueConstants.EnforcedLocalMachine, MetadataValueConstants.EnforcedLocalMachine));

    if (hasLocalMachine && hasEnforcedLocalMachine)
      CreateMultipleExecutionRuntimesException(MetadataValueConstants.LocalMachine, MetadataValueConstants.EnforcedLocalMachine);

    if (possibleExecutionRuntimes.Count > 1)
    {
      var executionRuntimeKeys = possibleExecutionRuntimes.Select(kvp => kvp.Key).ToArray();
      CreateMultipleExecutionRuntimesException(executionRuntimeKeys);
    }

    var dockerImage = possibleExecutionRuntimes.SingleOrDefault();

    if (hasLocalMachine)
      return new ExecutionRuntime(MetadataValueConstants.LocalMachine, MetadataValueConstants.LocalMachine, false, "");
    if (hasEnforcedLocalMachine)
      return new ExecutionRuntime(MetadataValueConstants.EnforcedLocalMachine, MetadataValueConstants.EnforcedLocalMachine, false,
          dockerImage.Value ?? "");
    return new ExecutionRuntime(dockerImage.Key, dockerImage.Value, true, dockerImage.Value);
  }

  private string GetSupportedConfigurationItem (
      IEnumerable<string> configurationItems,
      MetadataValueCollection supportItemCollection,
      string configurationItemName)
  {
    return supportItemCollection.Single(configurationItems, () => CreateMissingConfigurationItemException(configurationItemName));
  }

  private string GetTargetRuntime (IEnumerable<string> configurationItems)
  {
    var rawTargetRuntime = GetSupportedConfigurationItem(configurationItems, _supportedTargetRuntimes, "target runtime");
    return TargetRuntimeConverter.ToNUnitFormatFromMoniker(rawTargetRuntime);
  }

  private string GetCategories (IEnumerable<string> configurationItems, string categoryName)
  {
    var keyValuePair =
        configurationItems.SingleOrDefault(
            x => x.StartsWith(categoryName + "=", StringComparison.OrdinalIgnoreCase));

    return keyValuePair?.Split('=')[1] ?? "";
  }

  private InvalidOperationException CreateMissingConfigurationItemException (string configurationItemName) =>
      new($"Could not find a supported '{configurationItemName}'.");

  private void CreateMultipleExecutionRuntimesException (params string[] executionRuntimeKeys)
  {
    throw new InvalidOperationException($"Found multiple execution runtimes: {string.Join(",", executionRuntimeKeys)}");
  }
}