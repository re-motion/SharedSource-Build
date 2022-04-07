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
using System.Xml;
using Nuke.Common.Utilities.Collections;
using NUnit.Framework;

namespace BuildScript.Nuke.IntegrationTests;

public class BuildRunTestTest : BuildBaseTest
{
  [Test]
  public void RunTests_WithTwoProjectsAndOneTestFails_TestRunFails ()
  {
    IReadOnlyList<TestOutputData> testOutputData = new List<TestOutputData>
                                                   {
                                                       new(4, 4, 3, 1),
                                                       new(8, 8, 8, 0)
                                                   };
    var testProjectOutputs = BuildConfigurationSetup.CreateTestProjectOutputs(ProjectOutputConfiguration.ReleaseDebug);
    testProjectOutputs.ForEach((testOutput, index) => testOutput.ExpectedTestOutputData = testOutputData[index]);
    BuildConfigurationSetup.SetProjectsProps(Array.Empty<ProjectOutput>(), testProjectOutputs);
    BuildConfigurationSetup.SetBuildConfigurationProps(
        new Dictionary<string, string>
        {
            { "SupportedTargetRuntimes", "NETSTANDARD21;NET45" }
        });
    BuildConfigurationSetup.SetNormalTestConfigurationInProjectProps(
        "LocalMachine + NETSTANDARD21 + NoBrowser + NoDB + Debug + x86;"
        + "LocalMachine + NET45 + NoBrowser + NoDB + Debug + x86;");
    var arguments = @"--TargetRuntimes NET45 NETSTANDARD21";

    var exitCode = RunTarget("RunTests", arguments);

    Assert.That(exitCode, Is.EqualTo(-1));
    CheckCorrectRunTestOutputFiles(testProjectOutputs);
  }

  [Test]
  public void RunTests_WithTwoProjectsAndIncludeCategories_TestRunSucceeds ()
  {
    IReadOnlyList<TestOutputData> testOutputData = new List<TestOutputData>
                                                   {
                                                       new(1, 1, 1, 0),
                                                       new(0, 0, 0, 0)
                                                   };
    var testProjectOutputs = BuildConfigurationSetup.CreateTestProjectOutputs(ProjectOutputConfiguration.ReleaseDebug);
    testProjectOutputs.ForEach((testOutput, index) => testOutput.ExpectedTestOutputData = testOutputData[index]);
    BuildConfigurationSetup.SetProjectsProps(Array.Empty<ProjectOutput>(), testProjectOutputs);
    BuildConfigurationSetup.SetBuildConfigurationProps(
        new Dictionary<string, string>
        {
            { "SupportedTargetRuntimes", "NET48;NET45" }
        });
    BuildConfigurationSetup.SetNormalTestConfigurationInProjectProps(
        "LocalMachine + NET48 + NoBrowser + NoDB + Debug + x86 + IncludeCategories=Category1;"
        + "LocalMachine + NET45 + NoBrowser + NoDB + Debug + x86 + IncludeCategories=Category1;");
    var arguments = @"--TargetRuntimes NET48 NET45";

    var exitCode = RunTarget("RunTests", arguments);

    Assert.That(exitCode, Is.EqualTo(0));
    CheckCorrectRunTestOutputFiles(testProjectOutputs);
  }

  [Test]
  public void RunTests_WithTwoProjectsAndExcludeCategories_TestRunFails ()
  {
    IReadOnlyList<TestOutputData> testOutputData = new List<TestOutputData>
                                                   {
                                                       new(4, 4, 3, 1),
                                                       new(8, 8, 8, 0),
                                                       new(3, 3, 2, 1),
                                                       new(8, 8, 8, 0)
                                                   };
    var testProjectOutputs = BuildConfigurationSetup.CreateTestProjectOutputs(ProjectOutputConfiguration.ReleaseDebug);
    testProjectOutputs.ForEach((testOutput, index) => testOutput.ExpectedTestOutputData = testOutputData[index]);
    BuildConfigurationSetup.SetProjectsProps(Array.Empty<ProjectOutput>(), testProjectOutputs);
    BuildConfigurationSetup.SetBuildConfigurationProps(
        new Dictionary<string, string>
        {
            { "SupportedTargetRuntimes", "NET48;NET45" }
        });
    BuildConfigurationSetup.SetNormalTestConfigurationInProjectProps(
        "LocalMachine + NET48 + NoBrowser + NoDB + Debug + x86 + ExcludeCategories=Category1;"
        + "LocalMachine + NET45 + NoBrowser + NoDB + Debug + x86;");
    var arguments = @"--TargetRuntimes NET48 NET45";

    var exitCode = RunTarget("RunTests", arguments);

    Assert.That(exitCode, Is.EqualTo(-1));
    CheckCorrectRunTestOutputFiles(testProjectOutputs);
  }

  [Test]
  public void RunTests_WithTwoProjectsAndIncludeExcludeCategories_TestRunSucceeds ()
  {
    IReadOnlyList<TestOutputData> testOutputData = new List<TestOutputData>
                                                   {
                                                       new(1, 1, 1, 0),
                                                       new(0, 0, 0, 0)
                                                   };
    var testProjectOutputs = BuildConfigurationSetup.CreateTestProjectOutputs(ProjectOutputConfiguration.ReleaseDebug);
    testProjectOutputs.ForEach((testOutput, index) => testOutput.ExpectedTestOutputData = testOutputData[index]);
    BuildConfigurationSetup.SetProjectsProps(Array.Empty<ProjectOutput>(), testProjectOutputs);
    BuildConfigurationSetup.SetBuildConfigurationProps(
        new Dictionary<string, string>
        {
            { "SupportedTargetRuntimes", "NET48;NET45" }
        });
    BuildConfigurationSetup.SetNormalTestConfigurationInProjectProps(
        "LocalMachine + NET48 + NoBrowser + NoDB + Debug + x86 + IncludeCategories=Category1 + ExcludeCategories=Category2;"
        + "LocalMachine + NET45 + NoBrowser + NoDB + Debug + x86 + IncludeCategories=Category1 + ExcludeCategories=Category2;");
    var arguments = @"--TargetRuntimes NET48 NET45";

    var exitCode = RunTarget("RunTests", arguments);

    Assert.That(exitCode, Is.EqualTo(0));
    CheckCorrectRunTestOutputFiles(testProjectOutputs);
  }

  [Test]
  public void RunTests_SkipTests ()
  {
    var outputPath = $@"{TestSolutionPath}\BuildOutput\log";
    if (Directory.Exists(outputPath))
      Directory.Delete(outputPath, true);
    var testProjectOutputs = BuildConfigurationSetup.CreateTestProjectOutputs(ProjectOutputConfiguration.ReleaseDebug);
    BuildConfigurationSetup.SetProjectsProps(Array.Empty<ProjectOutput>(), testProjectOutputs);
    var arguments = @"--SkipTests";

    var exitCode = RunTarget("RunTests", arguments);

    Assert.That(exitCode, Is.EqualTo(0));
    Assert.That(outputPath, Does.Not.Exist);
  }

  [Test]
  public void RunTests_WithTwoProjectsAndTargetRuntimes_TestRunFails ()
  {
    IReadOnlyList<TestOutputData> testOutputData = new List<TestOutputData>
                                                   {
                                                       new(4, 4, 3, 1),
                                                       new(8, 8, 8, 0)
                                                   };
    var testProjectOutputs = BuildConfigurationSetup.CreateTestProjectOutputs(ProjectOutputConfiguration.ReleaseDebug);
    testProjectOutputs.ForEach((testOutput, index) => testOutput.ExpectedTestOutputData = testOutputData[index]);
    BuildConfigurationSetup.SetProjectsProps(Array.Empty<ProjectOutput>(), testProjectOutputs);
    BuildConfigurationSetup.SetBuildConfigurationProps(
        new Dictionary<string, string>
        {
            { "SupportedTargetRuntimes", "NET48;NET45" }
        });
    BuildConfigurationSetup.SetNormalTestConfigurationInProjectProps(
        "LocalMachine + NET48 + NoBrowser + NoDB + Debug + x86;"
        + "LocalMachine + NET45 + NoBrowser + NoDB + Debug + x86;");
    var arguments = @"--TargetRuntimes NET45 NET48";
    var exitCode = RunTarget("RunTests", arguments);
    Assert.That(exitCode, Is.EqualTo(-1));
    CheckCorrectRunTestOutputFiles(
        testProjectOutputs.Where(output => output.Name.Contains("Net45")).ToList(),
        testProjectOutputs.Where(output => output.Name.Contains("NET48")).ToList());
  }

  [Test]
  [Category("DockerTest")]
  public void RunTests_WithTwoProjectsAndWithDocker_TestRunSucceeds ()
  {
    IReadOnlyList<TestOutputData> testOutputData = new List<TestOutputData>
                                                   {
                                                       new(2, 2, 2, 0),
                                                       new(8, 8, 8, 0)
                                                   };
    var testProjectOutputs = BuildConfigurationSetup.CreateTestProjectOutputs(ProjectOutputConfiguration.ReleaseDebug);
    testProjectOutputs.ForEach((testOutput, index) => testOutput.ExpectedTestOutputData = testOutputData[index]);
    BuildConfigurationSetup.SetProjectsProps(Array.Empty<ProjectOutput>(), testProjectOutputs);
    BuildConfigurationSetup.SetBuildConfigurationProps(
        new Dictionary<string, string>
        {
            {
                "SupportedTargetRuntimes", "NET48;NET45"
            },
            {
                "SupportedExecutionRuntimes",
                "Win_NET48=docker.int.rubicon-it.com/rubicon-docker/dotnet/framework/aspnet/windowssdk:4.8-windowsservercore-ltsc2016"
            }
        });
    BuildConfigurationSetup.SetNormalTestConfigurationInProjectProps(
        "Win_NET48 + NET48 + NoBrowser + NoDB + Debug + x64 + ExcludeCategories=Category2;"
        + "Win_NET48 + NET45 + NoBrowser + NoDB + Debug + x86 + ExcludeCategories=Category2;");
    var arguments = @"--ExecutionRuntimes Win_NET48";

    var exitCode = RunTarget("RunTests", arguments);

    Assert.That(exitCode, Is.EqualTo(0));
    CheckCorrectRunTestOutputFiles(testProjectOutputs);
  }

  private void CheckCorrectRunTestOutputFiles (
      IReadOnlyCollection<TestProjectOutput> testProjectOutputs,
      IReadOnlyCollection<TestProjectOutput>? testProjectDoesNotExist = null)
  {
    var outputPath = $@"{TestSolutionPath}\BuildOutput\log\";
    Assert.That(outputPath, Does.Exist);
    var outputFiles = Directory.GetFiles(outputPath);
    var xmlDocument = new XmlDocument();
    testProjectOutputs.ForEach(
        testProjectOutput =>
        {
          var outputFile = outputFiles.FirstOrDefault(file => file.Contains(testProjectOutput.Name));
          Assert.That(outputFile, Is.Not.Null, $"TestOutput file for {testProjectOutput.Name} not found!");
          xmlDocument.Load(outputFile!);
          var countersNode = xmlDocument.GetElementsByTagName("Counters")[0];
          var passed = int.Parse(countersNode!.Attributes!["passed"]!.Value);
          var total = int.Parse(countersNode.Attributes!["total"]!.Value);
          var executed = int.Parse(countersNode.Attributes!["executed"]!.Value);
          var failed = int.Parse(countersNode.Attributes!["failed"]!.Value);
          Assert.That(passed, Is.EqualTo(testProjectOutput.ExpectedTestOutputData.Passed));
          Assert.That(total, Is.EqualTo(testProjectOutput.ExpectedTestOutputData.Total));
          Assert.That(executed, Is.EqualTo(testProjectOutput.ExpectedTestOutputData.Executed));
          Assert.That(failed, Is.EqualTo(testProjectOutput.ExpectedTestOutputData.Failed));
        });
    if (testProjectDoesNotExist != null)
      testProjectDoesNotExist.ForEach(
          testProject =>
          {
            var outputFile = outputFiles.FirstOrDefault(file => file.Contains(testProject.Name));
            Assert.That(outputFile, Is.Null, $"TestOutput file for {testProject.Name} was found!");
          });
  }
}
