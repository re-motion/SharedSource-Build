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

namespace Remotion.BuildScript.BuildTasks
{
  public class TestConfigurationsFactory
  {
    private readonly MetadataValueCollection _supportedBrowsers;
    private readonly MetadataValueCollection _supportedDatabaseSystems;
    private readonly MetadataValueCollection _supportedPlatforms;
    private readonly MetadataValueCollection _supportedConfigurationIDs;
    private readonly MetadataValueDictionary _supportedExecutionRuntimes;
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
        string testAssemblyFullPath,
        IReadOnlyCollection<string> rawTestConfigurations,
        string testSetupBuildFile = null)
    {
      return rawTestConfigurations
          .Select (GetConfigurationItems)
          .Select (configurationItems => CreateTestConfiguration (testAssemblyFullPath, configurationItems, testSetupBuildFile))
          .ToArray();
    }

    private IReadOnlyCollection<string> GetConfigurationItems (string configuration)
    {
      var configurationItems = configuration.Split ('+').Select (x => x.Trim()).ToArray();

      CheckNoDuplicateItemsExist (configurationItems);

      return configurationItems;
    }

    private void CheckNoDuplicateItemsExist (IReadOnlyCollection<string> configurationItems)
    {
      var duplicateValues = configurationItems.GetDuplicateValues (StringComparer.OrdinalIgnoreCase).ToList();
      if (duplicateValues.Any())
      {
        throw new ArgumentException (
            $"The following configuration values were found multiple times: '{string.Join (",", duplicateValues)}'");
      }
    }

    private TestConfiguration CreateTestConfiguration (
        string testAssemblyFullPath,
        IReadOnlyCollection<string> configurationItems,
        string testingSetupBuildFile)
    {
      var browser = GetBrowser (configurationItems);
      var databaseSystem = GetDatabaseSystem (configurationItems);
      var platform = GetPlatform (configurationItems);
      var targetRuntime = GetTargetRuntime (configurationItems);
      var executionRuntime = GetExecutionRuntime (configurationItems);
      var configurationID = GetConfigurationID (configurationItems);
      var testAssemblyFileName = Path.GetFileName (testAssemblyFullPath);
      var targetRuntimeInitialFormat = TargetRuntimeConverter.ToTargetFrameworkMoniker (targetRuntime);
      var excludeCategories = GetExcludeCategories (configurationItems);
      var excludeCategoriesIDText = string.IsNullOrEmpty (excludeCategories) ? "None" : excludeCategories;
      var includeCategories = GetIncludeCategories (configurationItems);
      var includeCategoriesIDText = string.IsNullOrEmpty (includeCategories) ? "None" : includeCategories;

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
                                                   $"ExcludeCategories={excludeCategoriesIDText}",
                                               };

      return new TestConfiguration (
          id: string.Join ("+", transformedConfigurationItemsForID.Where (x => x != "")),
          browser: browser,
          isWebTest: !string.Equals (browser, MetadataValueConstants.NoBrowser, StringComparison.OrdinalIgnoreCase),
          databaseSystem: databaseSystem,
          isDatabaseTest: !string.Equals (databaseSystem, MetadataValueConstants.NoDB, StringComparison.OrdinalIgnoreCase),
          configurationID: configurationID,
          use32Bit: string.Equals (platform, "x86", StringComparison.OrdinalIgnoreCase),
          platform: platform,
          executionRuntime: executionRuntime,
          testAssemblyFileName: testAssemblyFileName,
          testAssemblyFullPath: testAssemblyFullPath,
          testAssemblyDirectoryName: Path.GetDirectoryName (testAssemblyFullPath),
          testSetupBuildFile: testingSetupBuildFile,
          targetRuntime: targetRuntime,
          excludeCategories: excludeCategories,
          includeCategories: includeCategories);
    }

    private string GetConfigurationID (IEnumerable<string> configurationItems)
    {
      return _supportedConfigurationIDs.Single (configurationItems, () => CreateMissingConfigurationItemException ("configuration ID"));
    }

    private string GetPlatform (IEnumerable<string> configurationItems)
    {
      return _supportedPlatforms.Single (configurationItems, () => CreateMissingConfigurationItemException ("platform"));
    }

    private ExecutionRuntime GetExecutionRuntime (IEnumerable<string> configurationItems)
    {
      var enumeratedConfigurationItems = configurationItems.ToArray();
      var possibleExecutionRuntimes = _supportedExecutionRuntimes.Intersect (enumeratedConfigurationItems).ToList();

      if (possibleExecutionRuntimes.Count == 0)
        throw CreateMissingConfigurationItemException ("execution runtime");

      var hasLocalMachine = possibleExecutionRuntimes.Remove (MetadataValueConstants.LocalMachine);
      var hasEnforcedLocalMachine = possibleExecutionRuntimes.Remove (MetadataValueConstants.EnforcedLocalMachine);

      if (hasLocalMachine && hasEnforcedLocalMachine)
        CreateMultipleExecutionRuntimesException (MetadataValueConstants.LocalMachine, MetadataValueConstants.EnforcedLocalMachine);

      if (possibleExecutionRuntimes.Count > 1)
      {
        var executionRuntimeKeys = possibleExecutionRuntimes.Select (kvp => kvp.Key).ToArray();
        CreateMultipleExecutionRuntimesException (executionRuntimeKeys);
      }

      var executionRuntime = possibleExecutionRuntimes.SingleOrDefault();

      var executionContextParts = (executionRuntime.Value ?? string.Empty).Split('|');
      var dockerImage = executionContextParts[0];
      var dockerIsolationMode = executionContextParts.Length >= 2 && !string.IsNullOrWhiteSpace(executionContextParts[1])
          ? executionContextParts[1]
          : null;

      if (hasLocalMachine)
        return new ExecutionRuntime (MetadataValueConstants.LocalMachine, MetadataValueConstants.LocalMachine, false, "", dockerIsolationMode);
      else if (hasEnforcedLocalMachine)
        return new ExecutionRuntime (MetadataValueConstants.EnforcedLocalMachine, MetadataValueConstants.EnforcedLocalMachine,false, dockerImage, dockerIsolationMode);
      else
        return new ExecutionRuntime (executionRuntime.Key, executionRuntime.Value, true, dockerImage, dockerIsolationMode);
    }

    private static void CreateMultipleExecutionRuntimesException (params string[] executionRuntimeKeys)
    {
      throw new InvalidOperationException ($"Found multiple execution runtimes: {string.Join (",", executionRuntimeKeys)}");
    }

    private string GetDatabaseSystem (IEnumerable<string> configurationItems)
    {
      return _supportedDatabaseSystems.Single (configurationItems, () => CreateMissingConfigurationItemException ("database system"));
    }

    private string GetBrowser (IEnumerable<string> configurationItems)
    {
      return _supportedBrowsers.Single (configurationItems, () => CreateMissingConfigurationItemException ("browser"));
    }

    private string GetTargetRuntime (IEnumerable<string> configurationItems)
    {
      var rawTargetRuntime = _supportedTargetRuntimes.Single (configurationItems, () => CreateMissingConfigurationItemException ("target runtime"));

      return TargetRuntimeConverter.ToNUnitFormat (rawTargetRuntime);
    }

    private string GetExcludeCategories (IEnumerable<string> configurationItems)
    {
      var keyValuePair =
          configurationItems.SingleOrDefault (
              x => x.StartsWith (TestConfigurationMetadata.ExcludeCategories + "=", StringComparison.OrdinalIgnoreCase));

      return keyValuePair?.Split ('=')[1] ?? "";
    }

    private string GetIncludeCategories (IEnumerable<string> configurationItems)
    {
      var keyValuePair =
          configurationItems.SingleOrDefault (
              x => x.StartsWith (TestConfigurationMetadata.IncludeCategories + "=", StringComparison.OrdinalIgnoreCase));

      return keyValuePair?.Split ('=')[1] ?? "";
    }

    private InvalidOperationException CreateMissingConfigurationItemException (string configurationItemName)
    {
      return new InvalidOperationException ($"Could not find a supported '{configurationItemName}'.");
    }
  }
}