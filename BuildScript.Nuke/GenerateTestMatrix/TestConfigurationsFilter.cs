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

namespace Remotion.BuildScript.GenerateTestMatrix;

public class TestConfigurationsFilter
{
  private readonly MetadataValueCollection _enabledBrowsers;
  private readonly MetadataValueCollection _enabledConfigurationIDs;
  private readonly MetadataValueCollection _enabledDatabaseSystems;
  private readonly MetadataValueCollection _enabledExecutionRuntimeKeys;
  private readonly MetadataValueCollection _enabledPlatforms;
  private readonly MetadataValueCollection _enabledTargetRuntimes;
  private readonly MetadataValueCollection _supportedBrowsers;
  private readonly MetadataValueCollection _supportedConfigurationIDs;
  private readonly MetadataValueCollection _supportedDatabaseSystems;
  private readonly MetadataValueDictionary _supportedExecutionRuntimes;
  private readonly MetadataValueCollection _supportedPlatforms;
  private readonly MetadataValueCollection _supportedTargetRuntimes;

  public TestConfigurationsFilter (
      MetadataValueCollection supportedBrowsers,
      MetadataValueCollection enabledBrowsers,
      MetadataValueCollection supportedDatabaseSystems,
      MetadataValueCollection enabledDatabaseSystems,
      MetadataValueCollection supportedPlatforms,
      MetadataValueCollection enabledPlatforms,
      MetadataValueDictionary supportedExecutionRuntimes,
      MetadataValueCollection enabledExecutionRuntimeKeys,
      MetadataValueCollection supportedTargetRuntimes,
      MetadataValueCollection enabledTargetRuntimes,
      MetadataValueCollection supportedConfigurationIDs,
      MetadataValueCollection enabledConfigurationIDs)
  {
    var errors = new List<string>();
    if (enabledBrowsers.IsEmpty())
      errors.Add(GetErrorMessageForMissingEnabledValues(TestConfigurationMetadata.Browser));

    if (enabledDatabaseSystems.IsEmpty())
      errors.Add(GetErrorMessageForMissingEnabledValues(TestConfigurationMetadata.DatabaseSystem));

    if (enabledExecutionRuntimeKeys.IsEmpty())
      errors.Add(GetErrorMessageForMissingEnabledValues(TestConfigurationMetadata.ExecutionRuntimeValue));

    if (enabledPlatforms.IsEmpty())
      errors.Add(GetErrorMessageForMissingEnabledValues(TestConfigurationMetadata.Platform));

    if (enabledTargetRuntimes.IsEmpty())
      errors.Add(GetErrorMessageForMissingEnabledValues(TestConfigurationMetadata.TargetRuntime));

    if (errors.Any())
      throw new InvalidOperationException(string.Join(Environment.NewLine, errors));

    _supportedBrowsers = supportedBrowsers;
    _enabledBrowsers = enabledBrowsers;
    _supportedDatabaseSystems = supportedDatabaseSystems;
    _enabledDatabaseSystems = enabledDatabaseSystems;
    _supportedPlatforms = supportedPlatforms;
    _enabledPlatforms = enabledPlatforms;
    _supportedExecutionRuntimes = supportedExecutionRuntimes;
    _enabledExecutionRuntimeKeys = enabledExecutionRuntimeKeys;
    _supportedTargetRuntimes = supportedTargetRuntimes;
    _enabledTargetRuntimes = enabledTargetRuntimes;
    _supportedConfigurationIDs = supportedConfigurationIDs;
    _enabledConfigurationIDs = enabledConfigurationIDs;
  }

  public IReadOnlyCollection<TestConfiguration> GetFilteredTestConfigurations (IReadOnlyCollection<TestConfiguration> testConfigurationsToFilter)
  {
    var errorsForUnsupportedMetadata = GetErrorsForUnsupportedMetadata(testConfigurationsToFilter).ToArray();
    if (errorsForUnsupportedMetadata.Any())
      throw new InvalidOperationException(string.Join(Environment.NewLine, errorsForUnsupportedMetadata));

    return testConfigurationsToFilter
        .Where(HasEnabledBrowser)
        .Where(HasEnabledPlatform)
        .Where(HasEnabledDatabaseSystem)
        .Where(HasEnabledExecutionRuntime)
        .Where(HasEnabledTargetRuntime)
        .Where(HasEnabledConfigurationID)
        .ToArray();
  }

  private IEnumerable<string> GetErrorsForUnsupportedMetadata (IEnumerable<TestConfiguration> testConfigurations)
  {
    foreach (var testConfiguration in testConfigurations)
    {
      if (!IsSupportedBrowser(testConfiguration.Browser))
        yield return GetErrorMessageForUnsupportedFilterValue(TestConfigurationMetadata.Browser, testConfiguration.Browser);

      if (!IsSupportedPlatform(testConfiguration.Platform))
        yield return GetErrorMessageForUnsupportedFilterValue(TestConfigurationMetadata.Platform, testConfiguration.Platform);

      if (!IsSupportedDatabaseSystem(testConfiguration.DatabaseSystem))
        yield return GetErrorMessageForUnsupportedFilterValue(TestConfigurationMetadata.DatabaseSystem, testConfiguration.DatabaseSystem);

      if (!IsSupportedExecutionRuntime(testConfiguration.ExecutionRuntime.Key))
        yield return GetErrorMessageForUnsupportedFilterValue(TestConfigurationMetadata.ExecutionRuntimeKey, testConfiguration.ExecutionRuntime.Key);

      if (!IsSupportedTargetRuntime(testConfiguration.TargetRuntime))
        yield return GetErrorMessageForUnsupportedFilterValue(TestConfigurationMetadata.TargetRuntime, testConfiguration.TargetRuntime);

      if (!IsSupportedConfigurationID(testConfiguration.ConfigurationID))
        yield return GetErrorMessageForUnsupportedFilterValue(TestConfigurationMetadata.ConfigurationID, testConfiguration.TargetRuntime);
    }
  }

  private string GetErrorMessageForUnsupportedFilterValue (string metadata, string value) => $"Cannot filter with unsupported '{metadata}' '{value}'";

  private string GetErrorMessageForMissingEnabledValues (string metadata) => $"No enabled '{metadata}s' were provided.";

  private bool IsSupportedPlatform (string platform) => _supportedPlatforms.Contains(platform);

  private bool IsSupportedDatabaseSystem (string database) => _supportedDatabaseSystems.Contains(database);

  private bool IsSupportedExecutionRuntime (string executionRuntimeKey) => _supportedExecutionRuntimes.ContainsKey(executionRuntimeKey);

  private bool IsSupportedBrowser (string browser) => _supportedBrowsers.Contains(browser);

  private bool IsSupportedTargetRuntime (string targetRuntime) =>
      _supportedTargetRuntimes.Contains(TargetRuntimeConverter.ToTargetFrameworkMonikerFromNUnitFormat(targetRuntime));

  private bool IsSupportedConfigurationID (string configurationID) => _supportedConfigurationIDs.Contains(configurationID);

  private bool HasEnabledPlatform (TestConfiguration testConfiguration) => _enabledPlatforms.Contains(testConfiguration.Platform);

  private bool HasEnabledBrowser (TestConfiguration testConfiguration) => _enabledBrowsers.Contains(testConfiguration.Browser);

  private bool HasEnabledDatabaseSystem (TestConfiguration testConfiguration) => _enabledDatabaseSystems.Contains(testConfiguration.DatabaseSystem);

  private bool HasEnabledConfigurationID (TestConfiguration testConfiguration) =>
      _enabledConfigurationIDs.Contains(testConfiguration.ConfigurationID);

  private bool HasEnabledExecutionRuntime (TestConfiguration testConfiguration)
  {
    var keys = _supportedExecutionRuntimes.GetKeysForValue(testConfiguration.ExecutionRuntime.Value);

    return keys.Any(key => _enabledExecutionRuntimeKeys.Contains(key));
  }

  private bool HasEnabledTargetRuntime (TestConfiguration testingConfiguration)
  {
    var targetFrameworkMoniker = TargetRuntimeConverter.ToTargetFrameworkMonikerFromNUnitFormat(testingConfiguration.TargetRuntime);

    return _enabledTargetRuntimes.Contains(targetFrameworkMoniker);
  }
}