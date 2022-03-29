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

public class BuildNugetTest : BuildBaseTest
{
  [Test]
  public void GenerateNuGetPackagesWithDebugSymbols_WithAllReleaseProjects_BuildsNuGetPackages ()
  {
    var releaseProjectOutputs = BuildConfigurationSetup.CreateReleaseProjectOutputs(ProjectOutputConfiguration.ReleaseDebug);
    BuildConfigurationSetup.SetProjectsProps(releaseProjectOutputs, Array.Empty<ProjectOutput>());
    DeleteCleanDirectories(releaseProjectOutputs);
    var arguments = @" ";

    var exitCode = RunTarget("GenerateNuGetPackagesWithDebugSymbols", arguments);

    Assert.That(exitCode, Is.EqualTo(0));
    TestPackPaths(releaseProjectOutputs, "NuGetWithDebugSymbols");
  }

  [Test]
  public void GenerateNuGetPackagesWithSymbolServerSupport_WithAllReleaseProjects_BuildsNuGetPackages ()
  {
    var releaseProjectOutputs = BuildConfigurationSetup.CreateReleaseProjectOutputs(ProjectOutputConfiguration.ReleaseDebug);
    BuildConfigurationSetup.SetProjectsProps(releaseProjectOutputs, Array.Empty<ProjectOutput>());
    DeleteCleanDirectories(releaseProjectOutputs);
    var arguments = @" ";

    var exitCode = RunTarget("GenerateNuGetPackagesWithSymbolServerSupport", arguments);

    Assert.That(exitCode, Is.EqualTo(0));
    TestPackPaths(releaseProjectOutputs, "NuGetWithSymbolServerSupport");
  }

  [Test]
  public void GenerateNuGetPackagesWithDebugSymbols_SkipNuGet ()
  {
    if (Directory.Exists(@$"{TestSolutionPath}\BuildOutput"))
      Directory.Delete(@$"{TestSolutionPath}\BuildOutput", true);
    var releaseProjectOutputs = BuildConfigurationSetup.CreateReleaseProjectOutputs(ProjectOutputConfiguration.ReleaseDebug);
    BuildConfigurationSetup.SetProjectsProps(releaseProjectOutputs, Array.Empty<ProjectOutput>());
    DeleteCleanDirectories(releaseProjectOutputs);
    var arguments = @" --SkipNuGet";

    var exitCode = RunTarget("GenerateNuGetPackagesWithDebugSymbols", arguments);

    Assert.That(exitCode, Is.EqualTo(0));
    Assert.That(@$"{TestSolutionPath}\BuildOutput", Does.Not.Exist);
  }

  [Test]
  public void GenerateNuGetPackagesWithSymbolServerSupport_SkipNuGetOrg ()
  {
    if (Directory.Exists(@$"{TestSolutionPath}\BuildOutput"))
      Directory.Delete(@$"{TestSolutionPath}\BuildOutput", true);
    var releaseProjectOutputs = BuildConfigurationSetup.CreateReleaseProjectOutputs(ProjectOutputConfiguration.ReleaseDebug);
    BuildConfigurationSetup.SetProjectsProps(releaseProjectOutputs, Array.Empty<ProjectOutput>());
    DeleteCleanDirectories(releaseProjectOutputs);
    var arguments = @" --SkipNuGetOrg";

    var exitCode = RunTarget("GenerateNuGetPackagesWithSymbolServerSupport", arguments);

    Assert.That(exitCode, Is.EqualTo(0));
    Assert.That(@$"{TestSolutionPath}\BuildOutput", Does.Not.Exist);
  }

  private void TestPackPaths (IReadOnlyCollection<ProjectOutput> projectOutputs, string nugetFolder)
  {
    Assert.That(@$"{TestSolutionPath}\BuildOutput", Does.Exist);
    var nugetPath = @$"{TestSolutionPath}\BuildOutput\{nugetFolder}";
    Assert.That(nugetPath, Does.Exist);
    projectOutputs.ForEach(
        projectOutput =>
        {
          if (projectOutput.Configuration == ProjectOutputConfiguration.ReleaseDebug)
          {
            var nugetFilesDebug = Directory.GetFiles(@$"{nugetPath}\Debug\");
            var nugetFilesRelease = Directory.GetFiles(@$"{nugetPath}\Release\");
            Assert.That(nugetFilesDebug.Any(file => file.Contains(projectOutput.Name)), Is.True);
            Assert.That(nugetFilesRelease.Any(file => file.Contains(projectOutput.Name)), Is.True);
          }
          else
          {
            var nugetFiles = Directory.GetFiles(@$"{nugetPath}\{Enum.GetName(typeof(ProjectOutputConfiguration), projectOutput.Configuration)}\");
            Assert.That(nugetFiles.Any(file => file.Contains(projectOutput.Name)), Is.True);
          }
        });
  }
}
