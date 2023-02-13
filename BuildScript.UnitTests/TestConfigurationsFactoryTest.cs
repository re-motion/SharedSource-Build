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
using Microsoft.Build.Utilities;
using NUnit.Framework;
using Remotion.BuildScript.BuildTasks;

namespace Remotion.BuildScript.UnitTests
{
  [TestFixture]
  public class TestConfigurationsFactoryTest
  {
    [Test]
    public void CreateTestConfigurations_BrowserIsSupported_IsParsed ()
    {
      var factory = CreateTestConfigurationFactory (supportedBrowsers: new[] { "Firefox" });

      var testConfigurations = factory.CreateTestConfigurations ("C:\\Path\\To\\MyTest.dll", new[] { "Firefox+SqlServer2014+x64+Win_NET46+release+net45" });

      Assert.That (testConfigurations.Single().Browser, Is.EqualTo ("Firefox"));
    }

    [Test]
    public void CreateTestConfigurations_DatabaseSystemIsSupported_IsParsed ()
    {
      var factory = CreateTestConfigurationFactory (supportedDatabaseSystems: new[] { "SqlServer2012" });

      var testConfigurations = factory.CreateTestConfigurations ("C:\\Path\\To\\MyTest.dll", new[] { "Chrome+SqlServer2012+x64+Win_NET46+release+net45" });

      Assert.That (testConfigurations.Single().DatabaseSystem, Is.EqualTo ("SqlServer2012"));
    }

    [Test]
    public void CreateTestConfigurations_PlatformIsSupported_IsParsed ()
    {
      var factory = CreateTestConfigurationFactory (supportedPlatforms: new[] { "x86" });

      var testConfigurations = factory.CreateTestConfigurations ("C:\\Path\\To\\MyTest.dll", new[] { "Chrome+SqlServer2014+x86+Win_NET46+release+net45" });

      Assert.That (testConfigurations.Single().Platform, Is.EqualTo ("x86"));
    }

    [Test]
    public void CreateTestConfigurations_ConfigurationIDIsSupported_IsParsed ()
    {
      var factory = CreateTestConfigurationFactory (supportedConfigurationIDs: new[] { "debug" });

      var testConfigurations = factory.CreateTestConfigurations ("C:\\Path\\To\\MyTest.dll", new[] { "Chrome+SqlServer2014+x64+Win_NET46+debug+net45" });

      Assert.That (testConfigurations.Single().ConfigurationID, Is.EqualTo ("debug"));
    }

    [Test]
    public void CreateTestConfigurations_MultipleConfigurations_AreParsed ()
    {
      var factory = CreateTestConfigurationFactory (supportedBrowsers: new[] { "Firefox", "Chrome" });

      var testConfigurations = factory.CreateTestConfigurations (
              "C:\\Path\\To\\MyTest.dll",
              new[] { "Chrome+SqlServer2014+x64+Win_NET46+release+net45", "Firefox+SqlServer2014+x64+Win_NET46+release+net45" })
          .ToArray();

      Assert.That (testConfigurations[1].Browser, Is.EqualTo ("Firefox"));
      Assert.That (testConfigurations[1].DatabaseSystem, Is.EqualTo ("SqlServer2014"));
      Assert.That (testConfigurations[1].Platform, Is.EqualTo ("x64"));
      Assert.That (testConfigurations[1].ExecutionRuntime.Key, Is.EqualTo ("Win_NET46"));
      Assert.That (testConfigurations[1].ExecutionRuntime.Value, Is.EqualTo ("DockerImageName"));
      Assert.That (testConfigurations[1].ConfigurationID, Is.EqualTo ("release"));
    }

    [Test]
    public void CreateTestConfigurations_ConfigurationsWithTrailingWhitespaces_AreParsed ()
    {
      var config = $"{Environment.NewLine}    Chrome+SqlServer2014+x64+Win_NET46+release+net45{Environment.NewLine}    ";
      var factory = CreateTestConfigurationFactory();

      var testConfiguration = factory.CreateTestConfigurations ("C:\\Path\\To\\MyTest.dll", new[] { config }).Single();

      Assert.That (testConfiguration.Browser, Is.EqualTo ("Chrome"));
      Assert.That (testConfiguration.DatabaseSystem, Is.EqualTo ("SqlServer2014"));
      Assert.That (testConfiguration.Platform, Is.EqualTo ("x64"));
      Assert.That (testConfiguration.ExecutionRuntime.Key, Is.EqualTo ("Win_NET46"));
      Assert.That (testConfiguration.ExecutionRuntime.Value, Is.EqualTo ("DockerImageName"));
      Assert.That (testConfiguration.ConfigurationID, Is.EqualTo ("release"));
    }

    [Test]
    public void CreateTestConfigurations_ConfigurationOrder_IsIrrelevant ()
    {
      var factory = CreateTestConfigurationFactory (
          new[] { "x64" },
          new[] { "SqlServer2012" },
          new[] { "Firefox" },
          new Dictionary<string, string> { { "Win_NET46", "DockerImageName" } },
          new[] { "debug" },
          new[] { "NET45" });

      var testConfigurations = factory.CreateTestConfigurations ("C:\\Path\\To\\MyTest.dll", new[] { "debug+Firefox+Win_NET46+SqlServer2012+x64+net45" }).Single();

      Assert.That (testConfigurations.Browser, Is.EqualTo ("Firefox"));
      Assert.That (testConfigurations.DatabaseSystem, Is.EqualTo ("SqlServer2012"));
      Assert.That (testConfigurations.Platform, Is.EqualTo ("x64"));
      Assert.That (testConfigurations.ExecutionRuntime.Value, Is.EqualTo ("DockerImageName"));
      Assert.That (testConfigurations.ExecutionRuntime.Key, Is.EqualTo ("Win_NET46"));
      Assert.That (testConfigurations.ConfigurationID, Is.EqualTo ("debug"));
    }

    [Test]
    public void CreateTestConfigurations_DuplicateConfiguration_Error ()
    {
      var factory = CreateTestConfigurationFactory (
          supportedDatabaseSystems: new[] { "SqlServer2012" },
          supportedBrowsers: new[] { "Firefox" },
          supportedPlatforms: new[] { "x64" },
          supportedConfigurationIDs: new[] { "debug" },
          supportedExecutionRuntimes: new Dictionary<string, string> { { "Win_NET46", "DockerImageName" } });

      Assert.That (
          () => factory.CreateTestConfigurations ("C:\\Path\\To\\MyTest.dll", new[] { "debug+debug+Win_NET46+SqlServer2012+x64+net45" }),
          Throws.ArgumentException.With.Message.EqualTo ("The following configuration values were found multiple times: 'debug'"));
    }

    [Test]
    public void CreateTestConfigurations_ID_IsAssemblyNameAndConfiguration ()
    {
      const string testAssemblyFileName = "MyTest.dll";
      var testAssemblyFullPath = $"C:\\DirectoryName\\{testAssemblyFileName}";
      const string config = "Chrome+SqlServer2014+x64+Win_NET46+NET45+release";
      var factory = CreateTestConfigurationFactory();

      var testConfigurations = factory.CreateTestConfigurations (testAssemblyFullPath, new[] { config });

      Assert.That (testConfigurations.Single().ID, Is.EqualTo (testAssemblyFileName + "+" + config + "+IncludeCategories=None+ExcludeCategories=None"));
    }

    [Test]
    public void CreateTestConfigurations_ID_NormalizesOrder ()
    {
      const string testAssemblyFileName = "MyTest.dll";
      var testAssemblyFullPath = $"C:\\DirectoryName\\{testAssemblyFileName}";
      const string config = "release+Chrome+x64+Win_NET46+net45+SqlServer2014";
      var factory = CreateTestConfigurationFactory();

      var testConfigurations = factory.CreateTestConfigurations (testAssemblyFullPath, new[] { config });

      Assert.That (testConfigurations.Single().ID, Is.EqualTo ("MyTest.dll+Chrome+SqlServer2014+x64+Win_NET46+NET45+release+IncludeCategories=None+ExcludeCategories=None"));
    }

    [Test]
    public void CreateTestConfigurations_IDOfMultipleConfigurations_IsSameAssemblyNameButDifferentConfigurations ()
    {
      const string testAssemblyFileName = "MyTest.dll";
      const string testAssemblyDirectoryName = "Development";
      var testAssemblyFullPath = $"C:\\{testAssemblyDirectoryName}\\{testAssemblyFileName}";
      const string config0 = "Chrome+SqlServer2014+x86+Win_NET46+NET45+release";
      const string config1 = "Firefox+SqlServer2012+x64+Win_NET46+NET45+debug";
      var factory = CreateTestConfigurationFactory (
          supportedBrowsers: new[] { "Firefox", "Chrome" },
          supportedPlatforms: new[] { "x64", "x86" },
          supportedDatabaseSystems: new[] { "SqlServer2012", "SqlServer2014" },
          supportedConfigurationIDs: new[] { "release", "debug" });

      var testConfigurations = factory.CreateTestConfigurations (testAssemblyFullPath, new[] { config0, config1 }).ToArray();

      Assert.That (testConfigurations[0].ID, Is.EqualTo (testAssemblyFileName + "+" + config0 + "+IncludeCategories=None+ExcludeCategories=None"));
      Assert.That (testConfigurations[1].ID, Is.EqualTo (testAssemblyFileName + "+" + config1 + "+IncludeCategories=None+ExcludeCategories=None"));
    }

    [Test]
    public void CreateTestConfigurations_x86_Is32BitTrue ()
    {
      var factory = CreateTestConfigurationFactory();

      var testConfigurations = factory.CreateTestConfigurations ("C:\\Path\\To\\MyTest.dll", new[] { "Chrome+SqlServer2014+x86+Win_NET46+release+net45" });

      Assert.That (testConfigurations.Single().Use32Bit, Is.True);
    }

    [Test]
    public void CreateTestConfigurations_x86CaseInsensitive_Use32BitTrue ()
    {
      var factory = CreateTestConfigurationFactory();

      var testConfigurations = factory.CreateTestConfigurations ("C:\\Path\\To\\MyTest.dll", new[] { "Chrome+SqlServer2014+X86+Win_NET46+release+net45" });

      Assert.That (testConfigurations.Single().Use32Bit, Is.True);
    }

    [Test]
    public void CreateTestConfigurations_x64_Use32BitFalse ()
    {
      var factory = CreateTestConfigurationFactory();

      var testConfigurations = factory.CreateTestConfigurations ("C:\\Path\\To\\MyTest.dll", new[] { "Chrome+SqlServer2014+x64+Win_NET46+release+net45" });

      Assert.That (testConfigurations.Single().Use32Bit, Is.False);
    }

    [Test]
    public void CreateTestConfigurations_NoDB_IsDatabaseTestFalse ()
    {
      var factory = CreateTestConfigurationFactory (supportedDatabaseSystems: new[] { "NoDB" });

      var testConfigurations = factory.CreateTestConfigurations ("C:\\Path\\To\\MyTest.dll", new[] { "Chrome+NoDb+x64+Win_NET46+release+net45" });

      Assert.That (testConfigurations.Single().IsDatabaseTest, Is.False);
    }

    [Test]
    public void CreateTestConfigurations_SupportedDB_IsDatabaseTestTrue ()
    {
      var factory = CreateTestConfigurationFactory (supportedDatabaseSystems: new[] { "SqlServer2012" });

      var testConfigurations = factory.CreateTestConfigurations ("C:\\Path\\To\\MyTest.dll", new[] { "Chrome+SqlServer2012+x64+Win_NET46+release+net45" });

      Assert.That (testConfigurations.Single().IsDatabaseTest, Is.True);
    }

    [Test]
    public void CreateTestConfigurations_CaseInsensitiveNoDB_IsDatabaseTestFalse ()
    {
      var factory = CreateTestConfigurationFactory (supportedDatabaseSystems: new[] { "NoDB" });

      var testConfigurations = factory.CreateTestConfigurations ("C:\\Path\\To\\MyTest.dll", new[] { "Chrome+nodb+x64+Win_NET46+release+net45" });

      Assert.That (testConfigurations.Single().IsDatabaseTest, Is.False);
    }

    [Test]
    public void CreateTestConfigurations_NoBrowser_IsWebTestFalse ()
    {
      var factory = CreateTestConfigurationFactory (supportedBrowsers: new[] { "NoBrowser" });

      var testConfigurations = factory.CreateTestConfigurations ("C:\\Path\\To\\MyTest.dll", new[] { "NoBrowser+SqlServer2014+x64+Win_NET46+release+net45" });

      Assert.That (testConfigurations.Single().IsWebTest, Is.False);
    }

    [Test]
    public void CreateTestConfigurations_CaseInsensitiveNoBrowser_IsWebTestFalse ()
    {
      var factory = CreateTestConfigurationFactory (supportedBrowsers: new[] { "NoBrowser" });

      var testConfigurations = factory.CreateTestConfigurations ("C:\\Path\\To\\MyTest.dll", new[] { "nobrowser+SqlServer2014+x64+Win_NET46+release+net45" });

      Assert.That (testConfigurations.Single().IsWebTest, Is.False);
    }

    [Test]
    public void CreateTestConfigurations_SupportedBrowser_IsWebTestTrue ()
    {
      var factory = CreateTestConfigurationFactory (supportedBrowsers: new[] { "Firefox" });

      var testConfigurations = factory.CreateTestConfigurations ("C:\\Path\\To\\MyTest.dll", new[] { "Firefox+SqlServer2014+x64+Win_NET46+release+net45" });

      Assert.That (testConfigurations.Single().IsWebTest, Is.True);
    }

    [Test]
    public void CreateTestConfigurations_TestAssemblyFileName_DerivesFromTestAssemblyFullPath ()
    {
      const string testAssemblyFileName = "MyTest.dll";
      var testAssemblyFullPath = $"C:\\DirectoryName\\{testAssemblyFileName}";
      var factory = CreateTestConfigurationFactory();

      var testConfigurations = factory.CreateTestConfigurations (testAssemblyFullPath, new[] { "Chrome+SqlServer2014+x64+Win_NET46+release+net45" });

      Assert.That (testConfigurations.Single().TestAssemblyFileName, Is.EqualTo (testAssemblyFileName));
    }

    [Test]
    public void CreateTestConfigurations_TestAssemblyFullPath_IsParsed ()
    {
      const string testAssemblyFileName = "MyTest.dll";
      var testAssemblyFullPath = $"C:\\DirectoryName\\{testAssemblyFileName}";
      var factory = CreateTestConfigurationFactory();

      var testConfigurations = factory.CreateTestConfigurations (testAssemblyFullPath, new[] { "Chrome+SqlServer2014+x64+Win_NET46+release+net45" });

      Assert.That (testConfigurations.Single().TestAssemblyFullPath, Is.EqualTo (testAssemblyFullPath));
    }

    [Test]
    public void CreateTestConfigurations_TestAssemblyDirectoryName_IsParsed ()
    {
      const string testAssemblyDirectoryName = "C:\\Development";
      var testAssemblyFullPath = $"{testAssemblyDirectoryName}\\File.name";
      var factory = CreateTestConfigurationFactory();

      var testConfigurations = factory.CreateTestConfigurations (testAssemblyFullPath, new[] { "Chrome+SqlServer2014+x64+Win_NET46+release+net45" });

      Assert.That (testConfigurations.Single().TestAssemblyDirectoryName, Is.EqualTo (testAssemblyDirectoryName));
    }

    [Test]
    public void CreateTestConfigurations_TestSetupBuildFile_IsParsed ()
    {
      var factory = CreateTestConfigurationFactory();

      var testConfigurations = factory.CreateTestConfigurations (
          "C:\\Path\\To\\MyTest.dll",
          new[] { "Chrome+SqlServer2014+x64+Win_NET46+release+net45" },
          "MyTestSetupBuildFile");

      Assert.That (testConfigurations.Single().TestSetupBuildFile, Is.EqualTo ("MyTestSetupBuildFile"));
    }

    [Test]
    public void CreateTestConfigurations_LocalMachine_UseDockerFalse ()
    {
      var factory = CreateTestConfigurationFactory (supportedExecutionRuntimes: new Dictionary<string, string> { { "LocalMachine", "LocalMachine" } });

      var testConfigurations = factory.CreateTestConfigurations ("C:\\Path\\To\\MyTest.dll", new[] { "Chrome+SqlServer2014+x64+LocalMachine+release+net45" });

      Assert.That (testConfigurations.Single().ExecutionRuntime.UseDocker, Is.False);
    }

    [Test]
    public void CreateTestConfigurations_EnforcedLocalMachine_UseDockerFalse ()
    {
      var factory = CreateTestConfigurationFactory (supportedExecutionRuntimes: new Dictionary<string, string> { { "EnforcedLocalMachine", "EnforcedLocalMachine" } });

      var testConfigurations = factory.CreateTestConfigurations ("C:\\Path\\To\\MyTest.dll", new[] { "Chrome+SqlServer2014+x64+EnforcedLocalMachine+release+net45" });

      Assert.That (testConfigurations.Single().ExecutionRuntime.UseDocker, Is.False);
    }

    [Test]
    public void CreateTestConfigurations_ExecutionRuntime_HandlesSameKeyAndValue ()
    {
      var factory = CreateTestConfigurationFactory (supportedExecutionRuntimes: new Dictionary<string, string> { { "LocalMachine", "LocalMachine" } });

      var testConfigurations = factory.CreateTestConfigurations ("C:\\Path\\To\\MyTest.dll", new[] { "Chrome+SqlServer2014+x64+LocalMachine+release+net45" });

      Assert.That (testConfigurations.Single().ExecutionRuntime.Key, Is.EqualTo ("LocalMachine"));
      Assert.That (testConfigurations.Single().ExecutionRuntime.Value, Is.EqualTo ("LocalMachine"));
    }

    [Test]
    public void CreateTestConfigurations_CaseInsensitiveLocalMachine_UseDockerFalse ()
    {
      var factory = CreateTestConfigurationFactory (supportedExecutionRuntimes: new Dictionary<string, string> { { "LocalMachine", "LocalMachine" } });

      var testConfigurations = factory.CreateTestConfigurations ("C:\\Path\\To\\MyTest.dll", new[] { "Chrome+SqlServer2014+x64+lOcalmAchine+release+net45" });

      Assert.That (testConfigurations.Single().ExecutionRuntime.UseDocker, Is.False);
    }

    [Test]
    public void CreateTestConfigurations_CaseInsensitiveConfigurationID_LowerCase ()
    {
      var factory = CreateTestConfigurationFactory();

      var testConfigurations = factory.CreateTestConfigurations ("C:\\Path\\To\\MyTest.dll", new[] { "Chrome+SqlServer2014+x64+Win_NET46+releAse+net45" });

      Assert.That (testConfigurations.Single().ConfigurationID, Is.EqualTo ("release"));
    }

    [Test]
    public void CreateTestConfigurations_TargetFramework_IsParsed ()
    {
      var factory = CreateTestConfigurationFactory (supportedTargetRuntimes: new[] { "NET45" });

      var testConfigurations = factory.CreateTestConfigurations ("C:\\Path\\To\\MyTest.dll", new[] { "Chrome+SqlServer2014+x64+Win_NET46+release+NET45" });

      Assert.That (testConfigurations.Single().TargetRuntime, Is.EqualTo ("NET-4.5"));
    }

    [Test]
    public void CreateTestConfigurations_TargetFramework_IsCaseInsensitive ()
    {
      var factory = CreateTestConfigurationFactory (supportedTargetRuntimes: new[] { "net45" });

      var testConfigurations = factory.CreateTestConfigurations ("C:\\Path\\To\\MyTest.dll", new[] { "Chrome+SqlServer2014+x64+Win_NET46+release+nEt45" });

      Assert.That (testConfigurations.Single().TargetRuntime, Is.EqualTo ("NET-4.5"));
    }

    [Test]
    public void CreateTestConfigurations_Browser_IsCaseSensitiveToConformToWebTestConfigurationSection ()
    {
      var factory = CreateTestConfigurationFactory (supportedBrowsers: new[] { "InternetExplorer" });

      var testConfigurations = factory.CreateTestConfigurations ("C:\\Path\\To\\MyTest.dll", new[] { "InternetExplorer+SqlServer2014+x64+Win_NET46+release+NET45" });

      Assert.That (testConfigurations.Single().Browser, Is.EqualTo ("InternetExplorer"));
    }

    [Test]
    public void CreateTestConfigurations_SupportedExecutionRuntimes_IsKeyValuePair ()
    {
      const string executionRuntimeKey = "ExecutionRuntimeKey";
      const string executionRuntimeValue = "ExecutionRuntimeValue";
      var factory = CreateTestConfigurationFactory (supportedExecutionRuntimes: new Dictionary<string, string> { { executionRuntimeKey, executionRuntimeValue } });

      var testConfigurations = factory.CreateTestConfigurations (
          "C:\\Path\\To\\MyTest.dll",
          new[] { $"Chrome+SqlServer2014+x64+{executionRuntimeKey}+release+NET45" });

      Assert.That (testConfigurations.Single().ExecutionRuntime.Key, Is.EqualTo (executionRuntimeKey));
      Assert.That (testConfigurations.Single().ExecutionRuntime.Value, Is.EqualTo (executionRuntimeValue));
    }

    [Test]
    public void CreateTestConfigurations_SupportedExecutionRuntimes_SupportsColonInName ()
    {
      const string executionRuntimeKey = "ExecutionRuntimeKey";
      const string executionRuntimeValueWithColon = "ExecutionRuntime:Value";
      var factory = CreateTestConfigurationFactory (
          supportedExecutionRuntimes: new Dictionary<string, string> { { executionRuntimeKey, executionRuntimeValueWithColon } });

      var testConfigurations = factory.CreateTestConfigurations (
          "C:\\Path\\To\\MyTest.dll",
          new[] { $"Chrome+SqlServer2014+x64+{executionRuntimeKey}+release+NET45" });

      Assert.That (testConfigurations.Single().ExecutionRuntime.Key, Is.EqualTo (executionRuntimeKey));
      Assert.That (testConfigurations.Single().ExecutionRuntime.Value, Is.EqualTo (executionRuntimeValueWithColon));
    }

    [Test]
    public void CreateTestConfigurations_SupportedExecutionRuntimes_IgnoresCase ()
    {
      const string executionRuntimeKey = "ExecutionRuntimeKey";
      const string executionRuntimeValue = "ExecutionRuntimeValue";
      var factory = CreateTestConfigurationFactory (supportedExecutionRuntimes: new Dictionary<string, string> { { executionRuntimeKey, executionRuntimeValue } });

      var testConfigurations = factory.CreateTestConfigurations (
          "C:\\Path\\To\\MyTest.dll",
          new[] { "Chrome+SqlServer2014+x64+execuTionruntimekey+release+NET45" });

      Assert.That (testConfigurations.Single().ExecutionRuntime.Key, Is.EqualTo (executionRuntimeKey));
      Assert.That (testConfigurations.Single().ExecutionRuntime.Value, Is.EqualTo (executionRuntimeValue));
    }

    [Test]
    public void CreateTestConfigurations_SupportedExecutionRuntimes_ConsidersAllEntries ()
    {
      const string executionRuntimeKey1 = "ExecutionRuntimeKey1";
      const string executionRuntimeValue1 = "ExecutionRuntimeValue1";
      const string executionRuntimeKey2 = "ExecutionRuntimeKey2";
      const string executionRuntimeValue2 = "ExecutionRuntimeValue2";
      var supportedExecutionRuntimes = new Dictionary<string, string>
                                       {
                                           { executionRuntimeKey1, executionRuntimeValue1 },
                                           { executionRuntimeKey2, executionRuntimeValue2 }
                                       };
      var factory = CreateTestConfigurationFactory (supportedExecutionRuntimes: supportedExecutionRuntimes);

      var testConfigurations = factory.CreateTestConfigurations (
          "C:\\Path\\To\\MyTest.dll",
          new[] { $"Chrome+SqlServer2014+x64+{executionRuntimeKey2}+release+NET45" });

      Assert.That (testConfigurations.Single().ExecutionRuntime.Key, Is.EqualTo (executionRuntimeKey2));
      Assert.That (testConfigurations.Single().ExecutionRuntime.Value, Is.EqualTo (executionRuntimeValue2));
    }

    [Test]
    public void CreateTestConfigurations_MissingBrowser_ThrowsException ()
    {
      const string config = "SqlServer2014+x64+Win_NET46+release+net45";
      var factory = CreateTestConfigurationFactory();

      Assert.That (
          () => factory.CreateTestConfigurations ("C:\\Path\\To\\MyTest.dll", new[] { config }),
          Throws.InvalidOperationException.With.Message.EqualTo ("Could not find a supported 'browser'."));
    }

    [Test]
    public void CreateTestConfigurations_MissingTargetRuntime_ThrowsException ()
    {
      const string config = "SqlServer2014+x64+Win_NET46+release+Chrome";
      var factory = CreateTestConfigurationFactory();

      Assert.That (
          () => factory.CreateTestConfigurations ("C:\\Path\\To\\MyTest.dll", new[] { config }),
          Throws.InvalidOperationException.With.Message.EqualTo ("Could not find a supported 'target runtime'."));
    }

    [Test]
    public void CreateTestConfigurations_MissingDatabaseSystem_ThrowsException ()
    {
      const string config = "x64+Win_NET46+release+Chrome+net45";
      var factory = CreateTestConfigurationFactory();

      Assert.That (
          () => factory.CreateTestConfigurations ("C:\\Path\\To\\MyTest.dll", new[] { config }),
          Throws.InvalidOperationException.With.Message.EqualTo ("Could not find a supported 'database system'."));
    }

    [Test]
    public void CreateTestConfigurations_MissingExecutionRuntime_ThrowsException ()
    {
      const string config = "SqlServer2014+x64+release+Chrome+net45";
      var factory = CreateTestConfigurationFactory();

      Assert.That (
          () => factory.CreateTestConfigurations ("C:\\Path\\To\\MyTest.dll", new[] { config }),
          Throws.InvalidOperationException.With.Message.EqualTo ("Could not find a supported 'execution runtime'."));
    }

    [Test]
    public void CreateTestConfigurations_MissingPlatform_ThrowsException ()
    {
      const string config = "SqlServer2014+Win_NET46+release+Chrome+net45";
      var factory = CreateTestConfigurationFactory();

      Assert.That (
          () => factory.CreateTestConfigurations ("C:\\Path\\To\\MyTest.dll", new[] { config }),
          Throws.InvalidOperationException.With.Message.EqualTo ("Could not find a supported 'platform'."));
    }

    [Test]
    public void CreateTestConfigurations_MissingConfigurationID_ThrowsException ()
    {
      const string config = "SqlServer2014+x64+Win_NET46+Chrome+net45";
      var factory = CreateTestConfigurationFactory();

      Assert.That (
          () => factory.CreateTestConfigurations ("C:\\Path\\To\\MyTest.dll", new[] { config }),
          Throws.InvalidOperationException.With.Message.EqualTo ("Could not find a supported 'configuration ID'."));
    }

    [Test]
    public void CreateTestConfigurations_ExcludeCategories_IsParsed ()
    {
      var factory = CreateTestConfigurationFactory();

      var testConfigurations = factory.CreateTestConfigurations (
          "C:\\Path\\To\\MyTest.dll",
          new[] { "release+SqlServer2014+x64+Win_NET46+Chrome+net45+ExcludeCategories=a,b,c" });

      Assert.That (testConfigurations.Single().ExcludeCategories, Is.EqualTo ("a,b,c"));
    }

    [Test]
    public void CreateTestConfigurations_ExcludeCategoriesNotProvided_Empty ()
    {
      var factory = CreateTestConfigurationFactory();

      var testConfigurations = factory.CreateTestConfigurations ("C:\\Path\\To\\MyTest.dll", new[] { "release+SqlServer2014+x64+Win_NET46+Chrome+net45" });

      Assert.That (testConfigurations.Single().ExcludeCategories, Is.EqualTo (""));
    }

    [Test]
    public void CreateTestConfigurations_IncludeCategoriesNotProvided_Empty ()
    {
      var factory = CreateTestConfigurationFactory();

      var testConfigurations = factory.CreateTestConfigurations ("C:\\Path\\To\\MyTest.dll", new[] { "release+SqlServer2014+x64+Win_NET46+Chrome+net45" });

      Assert.That (testConfigurations.Single().IncludeCategories, Is.EqualTo (""));
    }

    [Test]
    public void CreateTestConfigurations_TestingCategories_ToleratesAlienItems ()
    {
      var taskItem = new TaskItem ("");
      const string config = "release+SqlServer2014+x64+Win_NET46+Chrome+net45+SomeRandomString";
      taskItem.SetMetadata ("TestingConfiguration", config);
      var factory = CreateTestConfigurationFactory();

      Assert.That (() => factory.CreateTestConfigurations ("C:\\Path\\To\\MyTest.dll", new[] { config }), Throws.Nothing);
    }

    [Test]
    public void CreateTestConfigurations_IncludeCategories_IsParsed ()
    {
      var factory = CreateTestConfigurationFactory();

      var testConfigurations = factory.CreateTestConfigurations (
          "C:\\Path\\To\\MyTest.dll",
          new[] { "release+SqlServer2014+x64+Win_NET46+Chrome+net45+IncludeCategories=a,b,c" });

      Assert.That (testConfigurations.Single().IncludeCategories, Is.EqualTo ("a,b,c"));
    }

    [Test]
    public void CreateTestConfigurations_Categories_ArePartOfID ()
    {
      var factory = CreateTestConfigurationFactory();

      var testConfigurations = factory.CreateTestConfigurations (
          "C:\\Path\\To\\MyTest.dll",
          new[] { "release+SqlServer2014+x64+Win_NET46+Chrome+net45+IncludeCategories=a,b,c+ExcludeCategories=d,e" });

      Assert.That (
          testConfigurations.Single().ID,
          Is.EqualTo ("MyTest.dll+Chrome+SqlServer2014+x64+Win_NET46+NET45+release+IncludeCategories=a,b,c+ExcludeCategories=d,e"));
    }

    [Test]
    public void CreateTestConfigurations_EnforcedLocalMachine_CanBeExtraFlag ()
    {
      var factory = CreateTestConfigurationFactory (
          supportedExecutionRuntimes: new Dictionary<string, string>
                                      {
                                          { "WIN_NET462", "DockerImageValue" },
                                          { "EnforcedLocalMachine", "EnforcedLocalMachine" }
                                      });

      var testConfigurations = factory.CreateTestConfigurations (
          "C:\\Path\\To\\MyTest.dll",
          new[] { "Chrome+SqlServer2014+x64+WIN_NET462+release+net45+EnforcedLocalMachine" });

      Assert.That (testConfigurations.Single().ExecutionRuntime.UseDocker, Is.False);
      Assert.That (testConfigurations.Single().ExecutionRuntime.Key, Is.EqualTo ("EnforcedLocalMachine"));
      Assert.That (testConfigurations.Single().ExecutionRuntime.Value, Is.EqualTo ("EnforcedLocalMachine"));
      Assert.That (testConfigurations.Single().ExecutionRuntime.DockerImage, Is.EqualTo ("DockerImageValue"));
      Assert.That (testConfigurations.Single().ExecutionRuntime.DockerIsolationMode, Is.EqualTo ("default"));
    }

    [Test]
    public void CreateTestConfigurations_ExecutionRuntimeWithDocker_ExposesAdditionalDockerImage ()
    {
      var factory = CreateTestConfigurationFactory (supportedExecutionRuntimes: new Dictionary<string, string> { { "Win_NET48", "DockerImageWinNet48" } });

      var testConfigurations = factory.CreateTestConfigurations ("C:\\Path\\To\\MyTest.dll", new[] { "Chrome+SqlServer2014+x64+Win_NET48+release+net45" });

      Assert.That (testConfigurations.Single().ExecutionRuntime.Key, Is.EqualTo ("Win_NET48"));
      Assert.That (testConfigurations.Single().ExecutionRuntime.Value, Is.EqualTo ("DockerImageWinNet48"));
      Assert.That (testConfigurations.Single().ExecutionRuntime.DockerImage, Is.EqualTo ("DockerImageWinNet48"));
      Assert.That (testConfigurations.Single().ExecutionRuntime.DockerIsolationMode, Is.EqualTo ("default"));
    }

    [Test]
    public void CreateTestConfigurations_ExecutionRuntimeWithDockerAndIsolationMode_UsesSpecifiedIsolationMode ()
    {
      var factory = CreateTestConfigurationFactory (supportedExecutionRuntimes: new Dictionary<string, string> { { "Win_NET48", "DockerImageWinNet48|hyperv" } });

      var testConfigurations = factory.CreateTestConfigurations ("C:\\Path\\To\\MyTest.dll", new[] { "Chrome+SqlServer2014+x64+Win_NET48+release+net45" });

      Assert.That (testConfigurations.Single().ExecutionRuntime.Key, Is.EqualTo ("Win_NET48"));
      Assert.That (testConfigurations.Single().ExecutionRuntime.Value, Is.EqualTo ("DockerImageWinNet48|hyperv"));
      Assert.That (testConfigurations.Single().ExecutionRuntime.DockerImage, Is.EqualTo ("DockerImageWinNet48"));
      Assert.That (testConfigurations.Single().ExecutionRuntime.DockerIsolationMode, Is.EqualTo ("hyperv"));
    }

    [Test]
    public void CreateTestConfigurations_LocalMachine_ExposesEmptyDockerImage ()
    {
      var factory = CreateTestConfigurationFactory (supportedExecutionRuntimes: new Dictionary<string, string> { { "LocalMachine", "LocalMachine" } });

      var testConfigurations = factory.CreateTestConfigurations ("C:\\Path\\To\\MyTest.dll", new[] { "Chrome+SqlServer2014+x64+LocalMachine+release+net45" });

      Assert.That (testConfigurations.Single().ExecutionRuntime.Key, Is.EqualTo ("LocalMachine"));
      Assert.That (testConfigurations.Single().ExecutionRuntime.Value, Is.EqualTo ("LocalMachine"));
      Assert.That (testConfigurations.Single().ExecutionRuntime.DockerImage, Is.EqualTo (""));
      Assert.That (testConfigurations.Single().ExecutionRuntime.DockerIsolationMode, Is.EqualTo ("default"));
    }

    [Test]
    public void CreateTestConfigurations_EnforcedLocalMachine_ExposesEmptyDockerImage ()
    {
      var factory = CreateTestConfigurationFactory (supportedExecutionRuntimes: new Dictionary<string, string> { { "EnforcedLocalMachine", "EnforcedLocalMachine" } });

      var testConfigurations = factory.CreateTestConfigurations ("C:\\Path\\To\\MyTest.dll", new[] { "Chrome+SqlServer2014+x64+EnforcedLocalMachine+release+net45" });

      Assert.That (testConfigurations.Single().ExecutionRuntime.Key, Is.EqualTo ("EnforcedLocalMachine"));
      Assert.That (testConfigurations.Single().ExecutionRuntime.Value, Is.EqualTo ("EnforcedLocalMachine"));
      Assert.That (testConfigurations.Single().ExecutionRuntime.DockerImage, Is.EqualTo (""));
      Assert.That (testConfigurations.Single().ExecutionRuntime.DockerIsolationMode, Is.EqualTo ("default"));
    }

    [Test]
    public void CreateTestConfigurations_MultipleExecutionRuntimes_ThrowsException ()
    {
      var factory = CreateTestConfigurationFactory (
          supportedExecutionRuntimes: new Dictionary<string, string>
                                      {
                                          { "Win_NET472", "DockerNet472" },
                                          { "Win_NET48", "DockerNet48" }
                                      });

      Assert.That (
          () => factory.CreateTestConfigurations ("C:\\Path\\To\\MyTest.dll", new[] { "Chrome+SqlServer2014+x64+Win_NET472+Win_NET48+release+net45" }),
          Throws.InvalidOperationException.With.Message.EqualTo ("Found multiple execution runtimes: Win_NET472,Win_NET48")
          );
    }

    [Test]
    public void CreateTestConfigurations_MultipleLocalExecutionRuntimes_ThrowsException ()
    {
      var factory = CreateTestConfigurationFactory (
          supportedExecutionRuntimes: new Dictionary<string, string>
                                      {
                                          { MetadataValueConstants.LocalMachine, MetadataValueConstants.LocalMachine },
                                          { MetadataValueConstants.EnforcedLocalMachine, MetadataValueConstants.EnforcedLocalMachine }
                                      });

      Assert.That (
          () => factory.CreateTestConfigurations ("C:\\Path\\To\\MyTest.dll", new[] { "Chrome+SqlServer2014+x64+LocalMachine+EnforcedLocalMachine+release+net45" }),
          Throws.InvalidOperationException.With.Message.EqualTo ("Found multiple execution runtimes: LocalMachine,EnforcedLocalMachine")
          );
    }

    private TestConfigurationsFactory CreateTestConfigurationFactory (
        IReadOnlyCollection<string> supportedPlatforms = null,
        IReadOnlyCollection<string> supportedDatabaseSystems = null,
        IReadOnlyCollection<string> supportedBrowsers = null,
        IDictionary<string, string> supportedExecutionRuntimes = null,
        IReadOnlyCollection<string> supportedConfigurationIDs = null,
        IReadOnlyCollection<string> supportedTargetRuntimes = null)
    {
      return new TestConfigurationsFactory (
          new MetadataValueCollection (supportedBrowsers ?? new[] { "Chrome" }),
          new MetadataValueCollection (supportedDatabaseSystems ?? new[] { "SqlServer2014" }),
          new MetadataValueCollection (supportedPlatforms ?? new[] { "x64", "x86" }),
          new MetadataValueCollection (supportedConfigurationIDs ?? new[] { "release" }),
          new MetadataValueDictionary (supportedExecutionRuntimes ?? new Dictionary<string, string> { { "Win_NET46", "DockerImageName" } }),
          new MetadataValueCollection (supportedTargetRuntimes ?? new[] { "NET45" }));
    }
  }
}