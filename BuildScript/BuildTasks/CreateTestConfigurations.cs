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
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Remotion.BuildScript.BuildTasks
{
  public class CreateTestConfigurations : Task
  {
    private const string c_testConfigurationMetadataName = "TestConfiguration";
    private const string c_testSetupBuildFileMetadataName = "TestSetupBuildFile";
    private static readonly MetadataValueCollection s_supportedConfigurationIDs = new MetadataValueCollection (new[] { "debug", "release" });

    [Required]
    public ITaskItem[] Input { get; set; }

    [Required]
    public ITaskItem[] SupportedBrowsers { get; set; }

    [Required]
    public ITaskItem[] EnabledBrowsers { get; set; }

    [Required]
    public ITaskItem[] SupportedDatabaseSystems { get; set; }

    [Required]
    public ITaskItem[] EnabledDatabaseSystems { get; set; }

    [Required]
    public ITaskItem[] SupportedPlatforms { get; set; }

    [Required]
    public ITaskItem[] EnabledPlatforms { get; set; }

    [Required]
    public ITaskItem[] SupportedExecutionRuntimes { get; set; }

    [Required]
    public ITaskItem[] EnabledExecutionRuntimes { get; set; }

    [Required]
    public ITaskItem[] SupportedTargetRuntimes { get; set; }

    [Required]
    public ITaskItem[] EnabledTargetRuntimes { get; set; }

    [Required]
    public ITaskItem[] EnabledConfigurationIDs { get; set; }

    [Output]
    public ITaskItem[] Output { get; set; }

    public override bool Execute ()
    {
      var supportedBrowsers = GetMetadataValueCollection (SupportedBrowsers, MetadataValueConstants.NoBrowser);
      var supportedDatabaseSystems = GetMetadataValueCollection (SupportedDatabaseSystems, MetadataValueConstants.NoDB);
      var supportedPlatforms = GetMetadataValueCollection (SupportedPlatforms);
      var supportedExecutionRuntimes = GetMetadataValueDictionary (
          SupportedExecutionRuntimes,
          MetadataValueConstants.EnforcedLocalMachine,
          MetadataValueConstants.LocalMachine);
      var supportedTargetRuntimes = GetMetadataValueCollection (SupportedTargetRuntimes);

      var testConfigurationsFactory = new TestConfigurationsFactory (
          supportedBrowsers: supportedBrowsers,
          supportedDatabaseSystems: supportedDatabaseSystems,
          supportedPlatforms: supportedPlatforms,
          supportedConfigurationIDs: s_supportedConfigurationIDs,
          supportedExecutionRuntimes: supportedExecutionRuntimes,
          supportedTargetRuntimes: supportedTargetRuntimes);

      var enabledBrowsers = GetMetadataValueCollection (EnabledBrowsers);
      var enabledDatabaseSystems = GetMetadataValueCollection (EnabledDatabaseSystems);
      var enabledPlatforms = GetMetadataValueCollection (EnabledPlatforms);
      var enabledExecutionRuntimes = GetMetadataValueCollection (EnabledExecutionRuntimes);
      var enabledTargetRuntimes = GetMetadataValueCollection (EnabledTargetRuntimes);
      var enabledConfigurationIDs = GetMetadataValueCollection (EnabledConfigurationIDs);

      var testConfigurationsFilter = new TestConfigurationsFilter (
          supportedBrowsers: supportedBrowsers,
          enabledBrowsers: enabledBrowsers,
          supportedDatabaseSystems: supportedDatabaseSystems,
          enabledDatabaseSystems: enabledDatabaseSystems,
          supportedPlatforms: supportedPlatforms,
          enabledPlatforms: enabledPlatforms,
          supportedExecutionRuntimes: supportedExecutionRuntimes,
          enabledExecutionRuntimeKeys: enabledExecutionRuntimes,
          supportedTargetRuntimes: supportedTargetRuntimes,
          enabledTargetRuntimes: enabledTargetRuntimes,
          supportedConfigurationIDs: s_supportedConfigurationIDs,
          enabledConfigurationIDs: enabledConfigurationIDs);

      try
      {
        Output = Input.SelectMany (inputItem => CreateOutputTestConfigurations (inputItem, testConfigurationsFactory, testConfigurationsFilter)).ToArray();
      }
      catch (InvalidOperationException ex)
      {
        Log.LogError (ex.Message);
        return false;
      }
      catch (ArgumentException ex)
      {
        Log.LogError (ex.Message);
        return false;
      }

      return true;
    }

    private ITaskItem[] CreateOutputTestConfigurations (
        ITaskItem inputItem,
        TestConfigurationsFactory testConfigurationsFactory,
        TestConfigurationsFilter testConfigurationsFilter)
    {
      var testAssemblyFullPath = inputItem.ItemSpec;
      var testConfigurationsMetadataValue = inputItem.GetMetadata (c_testConfigurationMetadataName);
      var testSetupBuildFile = inputItem.GetMetadata (c_testSetupBuildFileMetadataName);

      if (string.IsNullOrEmpty (testConfigurationsMetadataValue))
        throw new InvalidOperationException ($"Could not find item metadata 'TestConfiguration' for '{testAssemblyFullPath}'.");

      var rawTestConfigurations = testConfigurationsMetadataValue
          .Split (';')
          .Select (x => x.Trim())
          .Where (x => x != "")
          .ToArray();

      var unfilteredTestConfigurations = testConfigurationsFactory.CreateTestConfigurations (
          testAssemblyFullPath,
          rawTestConfigurations,
          testSetupBuildFile);
      var filteredTestConfigurations = testConfigurationsFilter.GetFilteredTestConfigurations (unfilteredTestConfigurations);

      return filteredTestConfigurations.Select (CreateTestConfigurationTaskItem).ToArray();
    }

    private ITaskItem CreateTestConfigurationTaskItem (TestConfiguration testConfiguration)
    {
      var item = new TaskItem (testConfiguration.ID);
      item.SetMetadata (TestConfigurationMetadata.Browser, testConfiguration.Browser);
      item.SetMetadata (TestConfigurationMetadata.DatabaseSystem, testConfiguration.DatabaseSystem);
      item.SetMetadata (TestConfigurationMetadata.Platform, testConfiguration.Platform);
      item.SetMetadata (TestConfigurationMetadata.ExecutionRuntimeValue, testConfiguration.ExecutionRuntime.Value);
      item.SetMetadata (TestConfigurationMetadata.ExecutionRuntimeKey, testConfiguration.ExecutionRuntime.Key);
      item.SetMetadata (TestConfigurationMetadata.DockerImage, testConfiguration.ExecutionRuntime.DockerImage);
      item.SetMetadata (TestConfigurationMetadata.TargetRuntime, testConfiguration.TargetRuntime);
      item.SetMetadata (TestConfigurationMetadata.Use32Bit, testConfiguration.Use32Bit.ToString());
      item.SetMetadata (TestConfigurationMetadata.UseDocker, testConfiguration.ExecutionRuntime.UseDocker.ToString());
      item.SetMetadata (TestConfigurationMetadata.IsWebTest, testConfiguration.IsWebTest.ToString());
      item.SetMetadata (TestConfigurationMetadata.IsDatabaseTest, testConfiguration.IsDatabaseTest.ToString());
      item.SetMetadata (TestConfigurationMetadata.ConfigurationID, testConfiguration.ConfigurationID);
      item.SetMetadata (TestConfigurationMetadata.TestAssemblyDirectoryName, testConfiguration.TestAssemblyDirectoryName);
      item.SetMetadata (TestConfigurationMetadata.TestAssemblyFileName, testConfiguration.TestAssemblyFileName);
      item.SetMetadata (TestConfigurationMetadata.TestAssemblyFullPath, testConfiguration.TestAssemblyFullPath);
      item.SetMetadata (TestConfigurationMetadata.TestingSetupBuildFile, testConfiguration.TestSetupBuildFile);
      item.SetMetadata (TestConfigurationMetadata.ExcludeCategories, testConfiguration.ExcludeCategories);
      item.SetMetadata (TestConfigurationMetadata.IncludeCategories, testConfiguration.IncludeCategories);

      return item;
    }


    private MetadataValueCollection GetMetadataValueCollection (IEnumerable<ITaskItem> taskItems, params string[] valuesToAppend)
    {
      var collection = taskItems.Select (x => x.ItemSpec).Concat (valuesToAppend).ToArray();

      return new MetadataValueCollection (collection);
    }

    private MetadataValueDictionary GetMetadataValueDictionary (IEnumerable<ITaskItem> taskItems, params string[] valuesToAppend)
    {
      var itemSpecs = taskItems.Select (x => x.ItemSpec).ToArray();

      foreach (var itemSpec in itemSpecs)
      {
        if (!Regex.IsMatch (itemSpec, "\\S+=\\S"))
          throw new FormatException ($"The execution runtime key value pair '{itemSpec}' is malformed.");
      }

      var splitItemSpecs = itemSpecs.Select (x => x.Split (new[] { "=" }, StringSplitOptions.RemoveEmptyEntries)).ToList();

      var duplicateKeys = splitItemSpecs.Select (x => x[0]).GetDuplicateValues (StringComparer.OrdinalIgnoreCase).ToList();
      if (duplicateKeys.Any())
      {
        var jointDuplicateValues = string.Join (",", duplicateKeys.Select (x => $"'{x}'"));
        throw new ArgumentException ($"Duplicate keys found in dictionary: {jointDuplicateValues}.", nameof(taskItems));
      }

      var dictionary = splitItemSpecs.ToDictionary (split => split[0], split => split[1], StringComparer.OrdinalIgnoreCase);

      foreach (var value in valuesToAppend)
        dictionary.Add (value, value);

      return new MetadataValueDictionary (dictionary);
    }
  }
}