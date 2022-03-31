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
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Nuke.Common.Utilities.Collections;
using NUnit.Framework;

namespace BuildScript.Nuke.IntegrationTests;

public class BuildBaseTest
{
  private readonly string _customizationsDefaultPath;
  private readonly string _customizationsPath;
  protected readonly BuildConfigurationSetup BuildConfigurationSetup;
  protected readonly string TestSolutionPath;
  protected readonly string TestBuildOutputPath;

  public BuildBaseTest ()
  {
    TestSolutionPath = Environment.CurrentDirectory;
    if (!Environment.CurrentDirectory.Contains("TestSolution"))
      TestSolutionPath = $"{Directory.GetParent(Environment.CurrentDirectory)!.Parent!.Parent}/TestSolution";

    _customizationsPath = $"{TestSolutionPath}/Build/Customizations";
    _customizationsDefaultPath = $"{TestSolutionPath}/Build/CustomizationsDefault";
    BuildConfigurationSetup = new BuildConfigurationSetup(TestSolutionPath, _customizationsPath);
    TestBuildOutputPath = $"{TestSolutionPath}/BuildOutput";
  }

  [SetUp]
  public void Setup ()
  {
    if (!Directory.Exists(_customizationsPath))
      Directory.CreateDirectory(_customizationsPath);

    foreach (var newPath in Directory.GetFiles(_customizationsDefaultPath))
      File.Copy(newPath, newPath.Replace(_customizationsDefaultPath, _customizationsPath), true);
  }

  [TearDown]
  public void Cleanup ()
  {
    Directory.GetFiles(_customizationsPath).ForEach(File.Delete);
  }

  protected int RunTarget(string target, string arguments, [CallerMemberName] string testOutputFileName = "testOutput")
  {
    Environment.CurrentDirectory = TestSolutionPath;
    var processStartInfo = new ProcessStartInfo($"{TestSolutionPath}/build.cmd", $"{target} {arguments}")
                           {
                               RedirectStandardOutput = true,
                               StandardOutputEncoding = Encoding.UTF8
                           };
    var process = Process.Start(processStartInfo);
    var consoleOutput = ReadConsoleOutput(process!);
    File.WriteAllText($"{testOutputFileName}.log", consoleOutput, Encoding.UTF8);
    process!.WaitForExit(60000);
    return process.ExitCode;
  }

  protected void DeleteCleanDirectories(IReadOnlyCollection<ProjectOutput> projectOutputs)
  {
    projectOutputs.ForEach(
        projectOutput =>
            projectOutput.CleanDirectories.ForEach(
                projectOutputCleanDirectory =>
                {
                  if (Directory.Exists(projectOutputCleanDirectory))
                    Directory.Delete(projectOutputCleanDirectory, true);
                }));
    if (Directory.Exists(TestBuildOutputPath))
      Directory.Delete(TestBuildOutputPath, true);
  }

  private string ReadConsoleOutput (Process process)
  {
    return process.StandardOutput.ReadToEnd();
  }
}