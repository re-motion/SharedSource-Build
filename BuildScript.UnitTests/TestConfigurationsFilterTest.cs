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
using NUnit.Framework;
using Remotion.BuildScript.BuildTasks;

namespace Remotion.BuildScript.UnitTests
{
  [TestFixture]
  public class TestConfigurationsFilterTest
  {
    private TestConfiguration _omnipresentTestConfiguration;

    [SetUp]
    public void SetUp ()
    {
      _omnipresentTestConfiguration = CreateTestConfiguration ("OmnipresentTestConfiguration");
    }

    [Test]
    public void GetFilteredTestConfigurations_AllSupportedAndEnabled_SameList ()
    {
      var testConfiguration = CreateTestConfiguration (
          platform: "x64",
          databaseSystem: "SqlServer2012",
          browser: "Firefox",
          executionRuntimeValue: "DockerImageForWinNET45");
      var testConfigurations = new[] { testConfiguration, _omnipresentTestConfiguration };
      var filter = CreateFilterTestConfigurations (
          supportedBrowsers: new[] { "Firefox" },
          enabledBrowsers: new[] { "Firefox" },
          supportedDatabaseSystems: new[] { "SqlServer2012" },
          enabledDatabaseSystems: new[] { "SqlServer2012" },
          supportedPlatforms: new[] { "x64" },
          enabledPlatforms: new[] { "x64" },
          supportedExecutionRuntimes: new Dictionary<string, string> { { "Win_NET45", "DockerImageForWinNET45" } },
          enabledExecutionRuntimes: new[] { "Win_NET45" });

      var filteredTestConfigurations = filter.GetFilteredTestConfigurations (testConfigurations);

      Assert.That (filteredTestConfigurations, Is.EqualTo (testConfigurations));
    }

    [Test]
    public void GetFilteredTestConfigurations_NotEnabledPlatForm_IsFiltered ()
    {
      var testConfiguration = CreateTestConfiguration (platform: "x86");
      var testConfigurations = new[] { testConfiguration, _omnipresentTestConfiguration };
      var filter = CreateFilterTestConfigurations (
          supportedPlatforms: new[] { "x64", "x86" },
          enabledPlatforms: new[] { "x64" });

      var filteredTestConfigurations = filter.GetFilteredTestConfigurations (testConfigurations);

      Assert.That (filteredTestConfigurations, Is.EquivalentTo (new[] { _omnipresentTestConfiguration }));
    }

    [Test]
    public void GetFilteredTestConfigurations_NotEnabledBrowser_IsFiltered ()
    {
      var testConfiguration = CreateTestConfiguration (browser: "Chrome");
      var testConfigurations = new[] { testConfiguration, _omnipresentTestConfiguration };
      var filter = CreateFilterTestConfigurations (
          enabledBrowsers: new[] { "Firefox" },
          supportedBrowsers: new[] { "Chrome", "Firefox" });

      var filteredTestConfigurations = filter.GetFilteredTestConfigurations (testConfigurations);

      Assert.That (filteredTestConfigurations, Is.EquivalentTo (new[] { _omnipresentTestConfiguration }));
    }

    [Test]
    public void GetFilteredTestConfigurations_NotEnableConfigurationID_IsFiltered ()
    {
      var testConfiguration = CreateTestConfiguration (configurationID: "release");
      var testConfigurations = new[] { testConfiguration, _omnipresentTestConfiguration };
      var filter = CreateFilterTestConfigurations (
          enabledConfigurationIDs: new[] { "debug" });

      var filteredTestConfigurations = filter.GetFilteredTestConfigurations (testConfigurations);

      Assert.That (filteredTestConfigurations, Is.EquivalentTo (new[] { _omnipresentTestConfiguration }));
    }

    [Test]
    public void GetFilteredTestConfigurations_NotEnableDatabaseSystem_IsFiltered ()
    {
      var testConfiguration = CreateTestConfiguration (databaseSystem: "SqlServer2014");
      var testConfigurations = new[] { testConfiguration, _omnipresentTestConfiguration };
      var filter = CreateFilterTestConfigurations (
          enabledDatabaseSystems: new[] { "SqlServer2012" },
          supportedDatabaseSystems: new[] { "SqlServer2012", "SqlServer2014" });

      var filteredTestConfigurations = filter.GetFilteredTestConfigurations (testConfigurations);

      Assert.That (filteredTestConfigurations, Is.EquivalentTo (new[] { _omnipresentTestConfiguration }));
    }

    [Test]
    public void GetFilteredTestConfigurations_NotEnabledExecutionRuntime_IsFiltered ()
    {
      var testConfiguration = CreateTestConfiguration (executionRuntimeValue: "DockerImageToBeFiltered");
      var testConfigurations = new[] { testConfiguration, _omnipresentTestConfiguration };
      var filter = CreateFilterTestConfigurations (
          enabledExecutionRuntimes: new[] { "Win_NET45" },
          supportedExecutionRuntimes: new Dictionary<string, string> { { "Win_NET45", "DockerImageForWinNET45" }, { "Win_NET46", "DockerImageToBeFiltered" } });

      var filteredTestConfigurations = filter.GetFilteredTestConfigurations (testConfigurations);

      Assert.That (filteredTestConfigurations, Is.EquivalentTo (new[] { _omnipresentTestConfiguration }));
    }

    [Test]
    public void GetFilteredTestConfigurations_SupportedPlatforms_IgnoresCase ()
    {
      var testConfiguration = CreateTestConfiguration (platform: "X64");
      var testConfigurations = new[] { testConfiguration };
      var filter = CreateFilterTestConfigurations (supportedPlatforms: new[] { "x64" });

      var filteredTestConfigurations = filter.GetFilteredTestConfigurations (testConfigurations);

      Assert.That (filteredTestConfigurations, Is.EqualTo (testConfigurations));
    }

    [Test]
    public void GetFilteredTestConfigurations_SupportedDatabaseSystems_IgnoresCase ()
    {
      var testConfiguration = CreateTestConfiguration (databaseSystem: "sqlserver2012");
      var testConfigurations = new[] { testConfiguration };
      var filter = CreateFilterTestConfigurations (supportedDatabaseSystems: new[] { "SqlServer2012" });

      var filteredTestConfigurations = filter.GetFilteredTestConfigurations (testConfigurations);

      Assert.That (filteredTestConfigurations, Is.EqualTo (testConfigurations));
    }

    [Test]
    public void GetFilteredTestConfigurations_SupportedBrowsers_IgnoresCase ()
    {
      var testConfiguration = CreateTestConfiguration (browser: "chrome");
      var testConfigurations = new[] { testConfiguration };
      var filter = CreateFilterTestConfigurations (
          supportedBrowsers: new[] { "Chrome" },
          enabledBrowsers: new[] { "Chrome" });

      var filteredTestConfigurations = filter.GetFilteredTestConfigurations (testConfigurations);

      Assert.That (filteredTestConfigurations, Is.EqualTo (testConfigurations));
    }

    [Test]
    public void GetFilteredTestConfigurations_NoBrowser_IgnoresCase ()
    {
      var testConfiguration = CreateTestConfiguration (browser: "nobrowser");
      var testConfigurations = new[] { testConfiguration };
      var filter = CreateFilterTestConfigurations (supportedBrowsers: new[] { "NoBrowser" }, enabledBrowsers: new[] { "NoBrowser" });

      var filteredTestConfigurations = filter.GetFilteredTestConfigurations (testConfigurations);

      Assert.That (filteredTestConfigurations, Is.EqualTo (testConfigurations));
    }

    [Test]
    public void GetFilteredTestConfigurations_NoDB_IgnoresCase ()
    {
      var testConfiguration = CreateTestConfiguration (databaseSystem: "nodb");
      var testConfigurations = new[] { testConfiguration };
      var filter = CreateFilterTestConfigurations (supportedDatabaseSystems: new[] { "NoDB" }, enabledDatabaseSystems: new[] { "NoDB" });

      var filteredTestConfigurations = filter.GetFilteredTestConfigurations (testConfigurations);

      Assert.That (filteredTestConfigurations, Is.EqualTo (testConfigurations));
    }

    [Test]
    public void GetFilteredTestConfigurations_UnsupportedBrowser_ThrowsException ()
    {
      const string unsupportedBrowser = "SomeUnsupportedBrowser";
      var testConfiguration = CreateTestConfiguration (browser: unsupportedBrowser);
      var testConfigurations = new[] { testConfiguration };
      var supportedBrowsers = new[] { "Chrome" };
      var filter = CreateFilterTestConfigurations (supportedBrowsers: supportedBrowsers);

      Assert.That (
          () => filter.GetFilteredTestConfigurations (testConfigurations),
          Throws.InvalidOperationException.With.Message.EqualTo ($"Cannot filter with unsupported '{TestConfigurationMetadata.Browser}' '{unsupportedBrowser}'"));
    }

    [Test]
    public void GetFilteredTestConfigurations_UnsupportedPlatform_ThrowsException ()
    {
      const string unsupportedPlatform = "SomeUnsupportedPlatform";
      var testConfiguration = CreateTestConfiguration (platform: unsupportedPlatform);
      var testConfigurations = new[] { testConfiguration };
      var supportedPlatforms = new[] { "x86" };
      var filter = CreateFilterTestConfigurations (supportedPlatforms: supportedPlatforms);

      Assert.That (
          () => filter.GetFilteredTestConfigurations (testConfigurations),
          Throws.InvalidOperationException.With.Message.EqualTo ($"Cannot filter with unsupported '{TestConfigurationMetadata.Platform}' '{unsupportedPlatform}'"));
    }

    [Test]
    public void GetFilteredTestConfigurations_UnsupportedDatabaseSystem_ThrowsException ()
    {
      const string unsupportedDatabaseSystem = "SomeUnsupportedDatabaseSystem";
      var testConfiguration = CreateTestConfiguration (databaseSystem: unsupportedDatabaseSystem);
      var testConfigurations = new[] { testConfiguration };
      var supportedDatabaseSystems = new[] { "SqlServer2014" };

      var filter = CreateFilterTestConfigurations (supportedDatabaseSystems: supportedDatabaseSystems);

      Assert.That (
          () => filter.GetFilteredTestConfigurations (testConfigurations),
          Throws.InvalidOperationException
              .With.Message.EqualTo ($"Cannot filter with unsupported '{TestConfigurationMetadata.DatabaseSystem}' '{unsupportedDatabaseSystem}'"));
    }

    [Test]
    public void GetFilteredTestConfigurations_FilterForUnsupportedExecutionRuntimes_ThrowsException ()
    {
      const string unsupportedExecutionRuntimeKey = "SomeUnsupportedExecutionRuntimeKey";
      var testConfiguration = CreateTestConfiguration (executionRuntimeKey: unsupportedExecutionRuntimeKey);
      var testConfigurations = new[] { testConfiguration };
      var supportedExecutionRuntimes = new Dictionary<string, string> { { "Win_NET48", "DockerImageForWinNET48" } };

      var filter = CreateFilterTestConfigurations (supportedExecutionRuntimes: supportedExecutionRuntimes);

      Assert.That (
          () => filter.GetFilteredTestConfigurations (testConfigurations),
          Throws.InvalidOperationException
              .With.Message.EqualTo ($"Cannot filter with unsupported '{TestConfigurationMetadata.ExecutionRuntimeKey}' '{unsupportedExecutionRuntimeKey}'"));
    }

    [Test]
    public void GetFilteredTestConfigurations_LocalMachineNotEnabled_IsFiltered ()
    {
      var testConfiguration = CreateTestConfiguration (executionRuntimeValue: "LocalMachine");
      var testConfigurations = new[] { testConfiguration, _omnipresentTestConfiguration };
      var supportedExecutionRuntimes = new Dictionary<string, string> { { "Win_NET45", "DockerImageForWinNET45" }, { "LocalMachine", "LocalMachine" } };
      var enabledExecutionRuntimes = new[] { "Win_NET45" };
      var filter = CreateFilterTestConfigurations (
          supportedExecutionRuntimes: supportedExecutionRuntimes,
          enabledExecutionRuntimes: enabledExecutionRuntimes);

      var filteredTestConfigurations = filter.GetFilteredTestConfigurations (testConfigurations);

      Assert.That (filteredTestConfigurations, Is.EquivalentTo (new[] { _omnipresentTestConfiguration }));
    }

    [Test]
    public void Initialize_WithoutEnabledBrowser_ThrowsException ()
    {
      var enabledBrowsers = new string[0];

      Assert.That (
          () => CreateFilterTestConfigurations (enabledBrowsers: enabledBrowsers),
          Throws.InvalidOperationException.With.Message.EqualTo ("No enabled 'Browsers' were provided."));
    }

    [Test]
    public void Initialize_WithoutEnabledDatabaseSystem_ThrowsException ()
    {
      var enabledDatabaseSystems = new string[0];

      Assert.That (
          () => CreateFilterTestConfigurations (enabledDatabaseSystems: enabledDatabaseSystems),
          Throws.InvalidOperationException.With.Message.EqualTo ("No enabled 'DatabaseSystems' were provided."));
    }

    [Test]
    public void Initialize_WithoutEnabledExecutionRuntime_ThrowsException ()
    {
      var enabledExecutionRuntimes = new string[0];

      Assert.That (
          () => CreateFilterTestConfigurations (enabledExecutionRuntimes: enabledExecutionRuntimes),
          Throws.InvalidOperationException.With.Message.EqualTo ("No enabled 'ExecutionRuntimeValues' were provided."));
    }

    [Test]
    public void Initialize_WithoutEnabledPlatform_ThrowsException ()
    {
      var enabledPlatforms = new string[0];

      Assert.That (
          () => CreateFilterTestConfigurations (enabledPlatforms: enabledPlatforms),
          Throws.InvalidOperationException.With.Message.EqualTo ("No enabled 'Platforms' were provided."));
    }

    [Test]
    public void GetFilteredTestConfigurations_NotEnabledTargetRuntime_IsFiltered ()
    {
      var testConfiguration = CreateTestConfiguration (targetRuntime: "NET-4.6");
      var testConfigurations = new[] { testConfiguration, _omnipresentTestConfiguration };
      var filter = CreateFilterTestConfigurations (
          enabledTargetRuntimes: new[] { "NET45" });

      var filteredTestConfigurations = filter.GetFilteredTestConfigurations (testConfigurations);

      Assert.That (filteredTestConfigurations, Is.EquivalentTo (new[] { _omnipresentTestConfiguration }));
    }

    [Test]
    public void GetFilteredTestConfigurations_EnforcedLocalMachine_CanBeEnabled ()
    {
      var testConfiguration = CreateTestConfiguration (executionRuntimeKey: "EnforcedLocalMachine", executionRuntimeValue: "EnforcedLocalMachine", browser: "Chrome");
      var testConfigurations = new[] { testConfiguration };
      var filter = CreateFilterTestConfigurations (
          supportedExecutionRuntimes: new Dictionary<string, string> { { "EnforcedLocalMachine", "EnforcedLocalMachine" } },
          enabledExecutionRuntimes: new[] { "EnforcedLocalMachine" },
          enabledBrowsers: new[] { "Chrome" },
          supportedBrowsers: new[] { "Chrome" });

      var filteredTestConfigurations = filter.GetFilteredTestConfigurations (testConfigurations);

      Assert.That (filteredTestConfigurations, Is.EquivalentTo (new[] { testConfiguration }));
    }

    [Test]
    public void Initialize_WithoutEnabledTargetRuntime_ThrowsException ()
    {
      var enabledTargetRuntimes = new string[0];

      Assert.That (
          () => CreateFilterTestConfigurations (enabledTargetRuntimes: enabledTargetRuntimes),
          Throws.InvalidOperationException.With.Message.EqualTo ("No enabled 'TargetRuntimes' were provided."));
    }

    [Test]
    public void GetFilteredTestConfigurations_MultipleKeysSameDockerImage_IsPossible ()
    {
      var testConfiguration = CreateTestConfiguration (executionRuntimeKey: "Key1", executionRuntimeValue: "DockerImage");
      var testConfigurations = new[] { testConfiguration };
      var filter = CreateFilterTestConfigurations (
          enabledExecutionRuntimes: new[] { "Key2" },
          supportedExecutionRuntimes: new Dictionary<string, string> { { "Key1", "DockerImage" }, { "Key2", "DockerImage" } });

      var filteredTestConfigurations = filter.GetFilteredTestConfigurations (testConfigurations);

      Assert.That (filteredTestConfigurations, Is.EqualTo (testConfigurations));
    }

    private TestConfigurationsFilter CreateFilterTestConfigurations (
        IReadOnlyCollection<string> supportedPlatforms = null,
        IReadOnlyCollection<string> supportedDatabaseSystems = null,
        IReadOnlyCollection<string> supportedBrowsers = null,
        IDictionary<string, string> supportedExecutionRuntimes = null,
        IReadOnlyCollection<string> enabledPlatforms = null,
        IReadOnlyCollection<string> enabledBrowsers = null,
        IReadOnlyCollection<string> enabledDatabaseSystems = null,
        IReadOnlyCollection<string> enabledExecutionRuntimes = null,
        IReadOnlyCollection<string> enabledTargetRuntimes = null,
        IReadOnlyCollection<string> enabledConfigurationIDs = null)
    {
      return new TestConfigurationsFilter (
          new MetadataValueCollection (supportedBrowsers ?? new[] { "Firefox" }),
          new MetadataValueCollection (enabledBrowsers ?? new[] { "Firefox" }),
          new MetadataValueCollection (supportedDatabaseSystems ?? new[] { "SqlServer2012" }),
          new MetadataValueCollection (enabledDatabaseSystems ?? new[] { "SqlServer2012" }),
          new MetadataValueCollection (supportedPlatforms ?? new[] { "x64" }),
          new MetadataValueCollection (enabledPlatforms ?? new[] { "x64" }),
          new MetadataValueDictionary (supportedExecutionRuntimes ?? new Dictionary<string, string> { { "Win_NET45", "DockerImageForWinNET45" } }),
          new MetadataValueCollection (enabledExecutionRuntimes ?? new[] { "Win_NET45" }),
          new MetadataValueCollection (new[] { "NET45", "NET46", "NET48" }),
          new MetadataValueCollection (enabledTargetRuntimes ?? new[] { "NET45" }),
          new MetadataValueCollection (new[] { "debug", "release" }),
          new MetadataValueCollection (enabledConfigurationIDs ?? new[] { "debug" }));
    }

    private TestConfiguration CreateTestConfiguration (
        string id = null,
        string platform = null,
        string databaseSystem = null,
        string browser = null,
        string executionRuntimeValue = null,
        string executionRuntimeKey = null,
        string dockerIsolationMode = null,
        string testAssemblyFullPath = null,
        string targetRuntime = null,
        string configurationID = null)
    {
      return new TestConfiguration (
          id ?? "TestConfiguration",
          browser ?? "Firefox",
          true,
          databaseSystem ?? "SqlServer2012",
          true,
          configurationID ?? "debug",
          false,
          platform ?? "x64",
          new ExecutionRuntime (
              executionRuntimeKey ?? "Win_NET45",
              executionRuntimeValue ?? "DockerImageForWinNET45",
              true,
              executionRuntimeValue ?? "DockerImageForWinNET45",
              dockerIsolationMode),
          "TestAssembly.dll",
          testAssemblyFullPath ?? "C:\\Path\\To\\TestAssembly.dll''",
          "DirectoryFileName",
          null,
          targetRuntime ?? "NET-4.5",
          null,
          null
          );
    }
  }
}