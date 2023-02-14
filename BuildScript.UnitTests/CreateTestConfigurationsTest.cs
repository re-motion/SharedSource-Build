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
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NUnit.Framework;
using Remotion.BuildScript.BuildTasks;

namespace Remotion.BuildScript.UnitTests
{
  [TestFixture]
  public class CreateTestConfigurationsTest
  {
    [Test]
    public void CreatedTestConfigurations_ParsedCorrectly ()
    {
      var task = new CreateTestConfigurations
                 {
                     SupportedBrowsers = new ITaskItem[] { new TaskItem ("Chrome"), new TaskItem ("Firefox") },
                     EnabledBrowsers = new ITaskItem[] { new TaskItem ("Chrome"), new TaskItem ("NoBrowser") },
                     SupportedDatabaseSystems = new ITaskItem[] { new TaskItem ("SqlServer2012"), new TaskItem ("SqlServer2014") },
                     EnabledDatabaseSystems = new ITaskItem[] { new TaskItem ("SqlServer2012"), new TaskItem ("NoDB") },
                     SupportedPlatforms = new ITaskItem[] { new TaskItem ("x64") },
                     EnabledPlatforms = new ITaskItem[] { new TaskItem ("x64") },
                     SupportedExecutionRuntimes =
                         new ITaskItem[] { new TaskItem ("Win_NET45=DockerImageForWinNet45|process"), new TaskItem ("Win_NET46=DockerImageForWinNet46") },
                     EnabledExecutionRuntimes = new ITaskItem[] { new TaskItem ("Win_NET45"), new TaskItem ("LocalMachine") },
                     SupportedTargetRuntimes = new ITaskItem[] { new TaskItem ("NET45") },
                     EnabledTargetRuntimes = new ITaskItem[] { new TaskItem ("NET45") },
                     EnabledConfigurationIDs = new ITaskItem[] { new TaskItem ("debug"), new TaskItem ("release") },
                 };


      var taskItem1 = new TaskItem (@"C:\TestAssemblyDirectory1\TestAssembly1.dll");
      taskItem1.SetMetadata (
          "TestConfiguration",
          "Chrome+SqlServer2012+Win_NET45+NET45+x64+debug+ExcludeCategories=a,b+IncludeCategories=c,d;Firefox+SqlServer2012+Win_NET45+NET45+x64+debug");
      taskItem1.SetMetadata ("TestSetupBuildFile", "TestSetupBuildFileForAssembly1");
      var taskItem2 = new TaskItem (@"C:\TestAssemblyDirectory2\TestAssembly2.dll");
      taskItem2.SetMetadata ("TestConfiguration", "NoBrowser+NoDB+LocalMachine+NET45+x64+release");

      task.Input = new ITaskItem[] { taskItem1, taskItem2 };

      var result = task.Execute();

      Assert.That (result, Is.True);
      Assert.That (task.Output.Length, Is.EqualTo(2));
      var testConfiguration1 = task.Output[0];
      var testConfiguration2 = task.Output[1];

      Assert.That (testConfiguration1.GetMetadata (TestConfigurationMetadata.Browser), Is.EqualTo ("Chrome"));
      Assert.That (testConfiguration1.GetMetadata (TestConfigurationMetadata.DatabaseSystem), Is.EqualTo ("SqlServer2012"));
      Assert.That (testConfiguration1.GetMetadata (TestConfigurationMetadata.Platform), Is.EqualTo ("x64"));
      Assert.That (testConfiguration1.GetMetadata (TestConfigurationMetadata.ExecutionRuntimeValue), Is.EqualTo ("DockerImageForWinNet45|process"));
      Assert.That (testConfiguration1.GetMetadata (TestConfigurationMetadata.ExecutionRuntimeKey), Is.EqualTo ("Win_NET45"));
      Assert.That (testConfiguration1.GetMetadata (TestConfigurationMetadata.DockerImage), Is.EqualTo ("DockerImageForWinNet45"));
      Assert.That (testConfiguration1.GetMetadata (TestConfigurationMetadata.DockerIsolationMode), Is.EqualTo ("process"));
      Assert.That (testConfiguration1.GetMetadata (TestConfigurationMetadata.TargetRuntime), Is.EqualTo ("NET-4.5"));
      Assert.That (testConfiguration1.GetMetadata (TestConfigurationMetadata.Use32Bit), Is.EqualTo ("False"));
      Assert.That (testConfiguration1.GetMetadata (TestConfigurationMetadata.UseDocker), Is.EqualTo ("True"));
      Assert.That (testConfiguration1.GetMetadata (TestConfigurationMetadata.IsDatabaseTest), Is.EqualTo ("True"));
      Assert.That (testConfiguration1.GetMetadata (TestConfigurationMetadata.IsWebTest), Is.EqualTo ("True"));
      Assert.That (testConfiguration1.GetMetadata (TestConfigurationMetadata.TestingSetupBuildFile), Is.EqualTo ("TestSetupBuildFileForAssembly1"));
      Assert.That (testConfiguration1.GetMetadata (TestConfigurationMetadata.TestAssemblyDirectoryName), Is.EqualTo (@"C:\TestAssemblyDirectory1"));
      Assert.That (testConfiguration1.GetMetadata (TestConfigurationMetadata.TestAssemblyFileName), Is.EqualTo ("TestAssembly1.dll"));
      Assert.That (testConfiguration1.GetMetadata (TestConfigurationMetadata.TestAssemblyFullPath), Is.EqualTo (@"C:\TestAssemblyDirectory1\TestAssembly1.dll"));
      Assert.That (testConfiguration1.GetMetadata (TestConfigurationMetadata.ConfigurationID), Is.EqualTo ("debug"));
      Assert.That (testConfiguration1.GetMetadata (TestConfigurationMetadata.ExcludeCategories), Is.EqualTo ("a,b"));
      Assert.That (testConfiguration1.GetMetadata (TestConfigurationMetadata.IncludeCategories), Is.EqualTo ("c,d"));

      Assert.That (testConfiguration2.GetMetadata (TestConfigurationMetadata.Browser), Is.EqualTo ("NoBrowser"));
      Assert.That (testConfiguration2.GetMetadata (TestConfigurationMetadata.DatabaseSystem), Is.EqualTo ("NoDB"));
      Assert.That (testConfiguration2.GetMetadata (TestConfigurationMetadata.Platform), Is.EqualTo ("x64"));
      Assert.That (testConfiguration2.GetMetadata (TestConfigurationMetadata.ExecutionRuntimeValue), Is.EqualTo ("LocalMachine"));
      Assert.That (testConfiguration2.GetMetadata (TestConfigurationMetadata.ExecutionRuntimeKey), Is.EqualTo ("LocalMachine"));
      Assert.That (testConfiguration2.GetMetadata (TestConfigurationMetadata.DockerImage), Is.EqualTo (""));
      Assert.That (testConfiguration2.GetMetadata (TestConfigurationMetadata.DockerIsolationMode), Is.EqualTo (""));
      Assert.That (testConfiguration2.GetMetadata (TestConfigurationMetadata.TargetRuntime), Is.EqualTo ("NET-4.5"));
      Assert.That (testConfiguration2.GetMetadata (TestConfigurationMetadata.Use32Bit), Is.EqualTo ("False"));
      Assert.That (testConfiguration2.GetMetadata (TestConfigurationMetadata.UseDocker), Is.EqualTo ("False"));
      Assert.That (testConfiguration2.GetMetadata (TestConfigurationMetadata.IsDatabaseTest), Is.EqualTo ("False"));
      Assert.That (testConfiguration2.GetMetadata (TestConfigurationMetadata.IsWebTest), Is.EqualTo ("False"));
      Assert.That (testConfiguration2.GetMetadata (TestConfigurationMetadata.TestingSetupBuildFile), Is.EqualTo (""));
      Assert.That (testConfiguration2.GetMetadata (TestConfigurationMetadata.TestAssemblyDirectoryName), Is.EqualTo (@"C:\TestAssemblyDirectory2"));
      Assert.That (testConfiguration2.GetMetadata (TestConfigurationMetadata.TestAssemblyFileName), Is.EqualTo ("TestAssembly2.dll"));
      Assert.That (testConfiguration2.GetMetadata (TestConfigurationMetadata.TestAssemblyFullPath), Is.EqualTo (@"C:\TestAssemblyDirectory2\TestAssembly2.dll"));
      Assert.That (testConfiguration2.GetMetadata (TestConfigurationMetadata.ConfigurationID), Is.EqualTo ("release"));
      Assert.That (testConfiguration2.GetMetadata (TestConfigurationMetadata.IncludeCategories), Is.EqualTo (""));
    }

    [Test]
    public void CreatedTestConfigurations_SupportsWellKnownValues ()
    {
      var task = new CreateTestConfigurations
                 {
                     SupportedBrowsers = new ITaskItem[] { new TaskItem ("Chrome") },
                     EnabledBrowsers = new ITaskItem[] { new TaskItem ("NoBrowser") },
                     SupportedDatabaseSystems = new ITaskItem[] { new TaskItem ("SqlServer2012") },
                     EnabledDatabaseSystems = new ITaskItem[] { new TaskItem ("NoDB") },
                     SupportedPlatforms = new ITaskItem[] { new TaskItem ("x64") },
                     EnabledPlatforms = new ITaskItem[] { new TaskItem ("x64") },
                     SupportedExecutionRuntimes = new ITaskItem[] { new TaskItem ("Win_NET45=DockerImageForWinNet45") },
                     EnabledExecutionRuntimes = new ITaskItem[] { new TaskItem ("LocalMachine"), new TaskItem ("EnforcedLocalMachine") },
                     SupportedTargetRuntimes = new ITaskItem[] { new TaskItem ("NET45") },
                     EnabledTargetRuntimes = new ITaskItem[] { new TaskItem ("NET45") },
                     EnabledConfigurationIDs = new ITaskItem[] { new TaskItem ("debug"), new TaskItem ("release") }
                 };


      var taskItem = new TaskItem (@"C:\TestAssemblyDirectory1\TestAssembly1.dll");
      taskItem.SetMetadata ("TestConfiguration", "NoBrowser+NoDB+LocalMachine+NET45+x64+debug;NoBrowser+NoDB+EnforcedLocalMachine+NET45+x64+debug");

      task.Input = new ITaskItem[] { taskItem };

      var result = task.Execute();

      Assert.That (result, Is.True);
      Assert.That (task.Output[0].GetMetadata (TestConfigurationMetadata.Browser), Is.EqualTo ("NoBrowser"));
      Assert.That (task.Output[0].GetMetadata (TestConfigurationMetadata.DatabaseSystem), Is.EqualTo ("NoDB"));
      Assert.That (task.Output[0].GetMetadata (TestConfigurationMetadata.ExecutionRuntimeValue), Is.EqualTo ("LocalMachine"));
      Assert.That (task.Output[0].GetMetadata (TestConfigurationMetadata.UseDocker), Is.EqualTo ("False"));
      Assert.That (task.Output[0].GetMetadata (TestConfigurationMetadata.IsDatabaseTest), Is.EqualTo ("False"));
      Assert.That (task.Output[0].GetMetadata (TestConfigurationMetadata.IsWebTest), Is.EqualTo ("False"));

      Assert.That (task.Output[1].GetMetadata (TestConfigurationMetadata.ExecutionRuntimeValue), Is.EqualTo ("EnforcedLocalMachine"));
      Assert.That (task.Output[1].GetMetadata (TestConfigurationMetadata.UseDocker), Is.EqualTo ("False"));
    }

    [Test]
    public void LogsErrorForMalformedExecutionRuntimes ()
    {
      var task = new CreateTestConfigurations
                 {
                     SupportedBrowsers = new ITaskItem[] { new TaskItem ("Chrome") },
                     EnabledBrowsers = new ITaskItem[] { new TaskItem ("NoBrowser") },
                     SupportedDatabaseSystems = new ITaskItem[] { new TaskItem ("SqlServer2012") },
                     EnabledDatabaseSystems = new ITaskItem[] { new TaskItem ("NoDB") },
                     SupportedPlatforms = new ITaskItem[] { new TaskItem ("x64") },
                     EnabledPlatforms = new ITaskItem[] { new TaskItem ("x64") },
                     SupportedExecutionRuntimes = new ITaskItem[] { new TaskItem ("Win_NET45=DockerImageForWinNet45"), new TaskItem ("A=") },
                     EnabledExecutionRuntimes = new ITaskItem[] { new TaskItem ("LocalMachine"), new TaskItem ("EnforcedLocalMachine") },
                     SupportedTargetRuntimes = new ITaskItem[] { new TaskItem ("NET45") },
                     EnabledTargetRuntimes = new ITaskItem[] { new TaskItem ("NET45") },
                     EnabledConfigurationIDs = new ITaskItem[] { new TaskItem ("debug"), new TaskItem ("release") }
                 };

      var taskItem = new TaskItem (@"C:\TestAssemblyDirectory1\TestAssembly1.dll");
      taskItem.SetMetadata ("TestConfiguration", "NoBrowser+NoDB+LocalMachine+NET45+x64+debug;NoBrowser+NoDB+EnforcedLocalMachine+NET45+x64+debug");

      task.Input = new ITaskItem[] { taskItem };

      Assert.That (
          () => task.Execute(),
          Throws.InstanceOf<FormatException>().With.Message.EqualTo ("The execution runtime key value pair 'A=' is malformed."));
    }

    [Test]
    public void LogsErrorForDuplicateExecutionRuntimeKeys ()
    {
      var task = new CreateTestConfigurations
                 {
                     SupportedBrowsers = new ITaskItem[] { new TaskItem ("Chrome") },
                     EnabledBrowsers = new ITaskItem[] { new TaskItem ("NoBrowser") },
                     SupportedDatabaseSystems = new ITaskItem[] { new TaskItem ("SqlServer2012") },
                     EnabledDatabaseSystems = new ITaskItem[] { new TaskItem ("NoDB") },
                     SupportedPlatforms = new ITaskItem[] { new TaskItem ("x64") },
                     EnabledPlatforms = new ITaskItem[] { new TaskItem ("x64") },
                     SupportedExecutionRuntimes =
                         new ITaskItem[] { new TaskItem ("Win_NET45=A"), new TaskItem ("win_net45=B"), new TaskItem ("Win_NET46=C"), new TaskItem ("win_net46=D") },
                     EnabledExecutionRuntimes = new ITaskItem[] { new TaskItem ("LocalMachine"), new TaskItem ("EnforcedLocalMachine") },
                     SupportedTargetRuntimes = new ITaskItem[] { new TaskItem ("NET45") },
                     EnabledTargetRuntimes = new ITaskItem[] { new TaskItem ("NET45") },
                     EnabledConfigurationIDs = new ITaskItem[] { new TaskItem ("debug"), new TaskItem ("release") }
                 };

      var taskItem = new TaskItem (@"C:\TestAssemblyDirectory1\TestAssembly1.dll");
      taskItem.SetMetadata ("TestConfiguration", "NoBrowser+NoDB+LocalMachine+NET45+x64+debug;NoBrowser+NoDB+EnforcedLocalMachine+NET45+x64+debug");

      task.Input = new ITaskItem[] { taskItem };

      Assert.That (
          () => task.Execute(),
          Throws.ArgumentException.With.Message.Contains ("Duplicate keys found in dictionary: 'Win_NET45','Win_NET46'."));
    }

    [Test]
    public void CreatedTestConfigurations_EnforcedExecutionRuntimeCanBeExtraFlag ()
    {
        var task = new CreateTestConfigurations
                   {
                           SupportedBrowsers = new ITaskItem[] { new TaskItem ("Chrome"), new TaskItem ("Firefox") },
                           EnabledBrowsers = new ITaskItem[] { new TaskItem ("Chrome"), new TaskItem ("NoBrowser") },
                           SupportedDatabaseSystems = new ITaskItem[] { new TaskItem ("SqlServer2012"), new TaskItem ("SqlServer2014") },
                           EnabledDatabaseSystems = new ITaskItem[] { new TaskItem ("SqlServer2012"), new TaskItem ("NoDB") },
                           SupportedPlatforms = new ITaskItem[] { new TaskItem ("x64") },
                           EnabledPlatforms = new ITaskItem[] { new TaskItem ("x64") },
                           SupportedExecutionRuntimes =
                                   new ITaskItem[] { new TaskItem ("Win_NET45=DockerImageForWinNet45"), new TaskItem ("Win_NET46=DockerImageForWinNet46") },
                           EnabledExecutionRuntimes = new ITaskItem[] { new TaskItem ("EnforcedLocalMachine"), new TaskItem ("Win_NET45") },
                           SupportedTargetRuntimes = new ITaskItem[] { new TaskItem ("NET45") },
                           EnabledTargetRuntimes = new ITaskItem[] { new TaskItem ("NET45") },
                           EnabledConfigurationIDs = new ITaskItem[] { new TaskItem ("debug"), new TaskItem ("release") },
                   };


        var taskItem = new TaskItem (@"C:\TestAssemblyDirectory1\TestAssembly1.dll");
        taskItem.SetMetadata (
                "TestConfiguration",
                "Chrome+SqlServer2012+Win_NET45+NET45+x64+debug+EnforcedLocalMachine");

        task.Input = new ITaskItem[] { taskItem };

        var result = task.Execute();

        Assert.That (result, Is.True);
        var testConfiguration = task.Output[0];

        Assert.That (testConfiguration.GetMetadata (TestConfigurationMetadata.ExecutionRuntimeValue), Is.EqualTo ("EnforcedLocalMachine"));
        Assert.That (testConfiguration.GetMetadata (TestConfigurationMetadata.ExecutionRuntimeKey), Is.EqualTo ("EnforcedLocalMachine"));
        Assert.That (testConfiguration.GetMetadata (TestConfigurationMetadata.DockerImage), Is.EqualTo ("DockerImageForWinNet45"));
        Assert.That (testConfiguration.GetMetadata (TestConfigurationMetadata.DockerIsolationMode), Is.EqualTo ("default"));
        Assert.That (testConfiguration.GetMetadata (TestConfigurationMetadata.UseDocker), Is.EqualTo ("False"));
    }
  }
}