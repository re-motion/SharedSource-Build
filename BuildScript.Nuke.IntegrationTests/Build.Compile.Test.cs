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
using Nuke.Common.Utilities.Collections;
using NUnit.Framework;

namespace BuildScript.Nuke.IntegrationTests;

public class BuildCompileTest : BuildBaseTest
{
  [Test]
  public void CompileReleaseBuild_WithAllReleaseProjectsWithDebugRelease_BuildsAllReleaseAssemblies ()
  {
    var releaseProjectOutputs = BuildConfigurationSetup.CreateReleaseProjectOutputs(ProjectOutputConfiguration.ReleaseDebug);
    BuildConfigurationSetup.SetProjectsProps(releaseProjectOutputs, Array.Empty<ProjectOutput>());
    DeleteCleanDirectories(releaseProjectOutputs);
    TestNotExistsOutputPaths(releaseProjectOutputs);
    var arguments = @" ";

    var exitCode = RunTarget("CompileReleaseBuild", arguments);

    Assert.That(exitCode, Is.EqualTo(0));
    TestCorrectCompileOutputPaths(releaseProjectOutputs);
  }

  [Test]
  public void CompileReleaseBuild_WithOneReleaseProjectWithRelease_BuildsOneProjectReleaseAssembly ()
  {
    var releaseProjectOutputs = BuildConfigurationSetup.CreateReleaseProjectOutputs(ProjectOutputConfiguration.Release).Take(1).ToList();
    BuildConfigurationSetup.SetProjectsProps(releaseProjectOutputs, Array.Empty<ProjectOutput>());
    DeleteCleanDirectories(releaseProjectOutputs);
    TestNotExistsOutputPaths(releaseProjectOutputs);
    var arguments = @"--configuration Release";

    var exitCode = RunTarget("CompileReleaseBuild", arguments);

    Assert.That(exitCode, Is.EqualTo(0));
    TestCorrectCompileOutputPaths(releaseProjectOutputs);
  }

  [Test]
  public void CompileReleaseBuild_WithTwoReleaseProjectsWithDebug_BuildsTwoProjectDebugAssemblies ()
  {
    var releaseProjectOutputs = BuildConfigurationSetup.CreateReleaseProjectOutputs(ProjectOutputConfiguration.Debug).Take(2).ToList();
    BuildConfigurationSetup.SetProjectsProps(releaseProjectOutputs, Array.Empty<ProjectOutput>());
    DeleteCleanDirectories(releaseProjectOutputs);
    TestNotExistsOutputPaths(releaseProjectOutputs);
    var arguments = @"--configuration Debug";

    var exitCode = RunTarget("CompileReleaseBuild", arguments);

    Assert.That(exitCode, Is.EqualTo(0));
    TestCorrectCompileOutputPaths(releaseProjectOutputs);
  }

  [Test]
  public void CompileTestBuild_WithAllTestProjectsAndDebugRelease_BuildsAllTestAssemblies ()
  {
    var testProjectOutputs = BuildConfigurationSetup.CreateTestProjectOutputs(ProjectOutputConfiguration.ReleaseDebug);
    BuildConfigurationSetup.SetProjectsProps(Array.Empty<ProjectOutput>(), testProjectOutputs);
    DeleteCleanDirectories(testProjectOutputs);
    TestNotExistsOutputPaths(testProjectOutputs);
    var arguments = @" ";

    var exitCode = RunTarget("CompileTestBuild", arguments);

    Assert.That(exitCode, Is.EqualTo(0));
    TestCorrectCompileOutputPaths(testProjectOutputs);
  }

  [Test]
  public void CompileTestBuild_WithOneTestProjectAndDebug_BuildsOneTestAssembly ()
  {
    var testProjectOutputs = BuildConfigurationSetup.CreateTestProjectOutputs(ProjectOutputConfiguration.Debug).Take(1).ToList();
    BuildConfigurationSetup.SetProjectsProps(Array.Empty<ProjectOutput>(), testProjectOutputs);
    DeleteCleanDirectories(testProjectOutputs);
    TestNotExistsOutputPaths(testProjectOutputs);
    var arguments = @"--configuration Debug";

    var exitCode = RunTarget("CompileTestBuild", arguments);

    Assert.That(exitCode, Is.EqualTo(0));
    TestCorrectCompileOutputPaths(testProjectOutputs);
  }

  [Test]
  public void Restore_WithReleaseAndTestProjects_RestoresAllNugetPackages ()
  {
    var testProjectOutputs = BuildConfigurationSetup.CreateTestProjectOutputs(ProjectOutputConfiguration.Debug);
    var releaseProjectOutputs = BuildConfigurationSetup.CreateReleaseProjectOutputs(ProjectOutputConfiguration.Debug);
    BuildConfigurationSetup.SetProjectsProps(releaseProjectOutputs, testProjectOutputs);
    DeleteCleanDirectories(testProjectOutputs);
    DeleteCleanDirectories(releaseProjectOutputs);
    TestNotExistsOutputPaths(testProjectOutputs);
    TestNotExistsOutputPaths(releaseProjectOutputs);
    var arguments = @" ";

    var exitCode = RunTarget("Restore", arguments);

    Assert.That(exitCode, Is.EqualTo(0));
    TestRestorePaths(releaseProjectOutputs);
    TestRestorePaths(testProjectOutputs);
  }

  private void TestRestorePaths (IReadOnlyCollection<ProjectOutput> projectOutputs)
  {
    projectOutputs.ForEach(
        projectOutput =>
        {
          if (projectOutput.IsSdkProject)
            Assert.That(@$"{TestSolutionPath}\{projectOutput.Name}\obj", Does.Exist);
        });
  }

  private void TestNotExistsOutputPaths (IReadOnlyCollection<ProjectOutput> projectOutputs)
  {
    projectOutputs.ForEach(
        projectOutput =>
            projectOutput.OutputPaths.ForEach(
                outputPath =>
                    Assert.That(outputPath, Does.Not.Exist)));
  }

  private void TestCorrectCompileOutputPaths (IReadOnlyCollection<ProjectOutput> projectOutputs)
  {
    projectOutputs.ForEach(
        projectOutput =>
        {
          projectOutput.OutputPathsExclude.ForEach(
              outputPathExclude =>
              {
                if (Directory.Exists(outputPathExclude))
                {
                  var outputFiles = Directory.GetFiles(outputPathExclude);
                  Assert.That(outputFiles, Is.Empty);
                }
              }
          );
          projectOutput.OutputPaths.ForEach(outputPath => Assert.That(outputPath, Does.Exist));
        }
    );
  }
}