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

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using Assert = NUnit.Framework.Assert;

namespace BuildScript.Nuke.IntegrationTests;

public abstract class IntegrationTestBase
{
  private AbsolutePath _testInfoFile = default!;

  public AbsolutePath TestSolutionDirectory { get; set; } = default!;

  public TestSolutionModel TestSolutionModel { get; set; } = default!;

  [SetUp]
  public void SetUp ()
  {
    var outputFolder = (AbsolutePath)Path.GetDirectoryName(typeof(IntegrationTestBase).Assembly.Location);
    var testSolutionTemplateFolder = Path.GetFullPath(outputFolder + "../../../../TestSolution");
    if (!Directory.Exists(testSolutionTemplateFolder))
      throw new InvalidOperationException($"Cannot find the test solution template '{testSolutionTemplateFolder}'.");

    // Use hash for folder names to prevent running into path length problems
    var testName = TestContext.CurrentContext.Test.ClassName + "." + TestContext.CurrentContext.Test.Name;
    var testNameHashPart = testName.GetSHA256Hash()[..10];
    TestSolutionDirectory = outputFolder / "TempSolutions" / testNameHashPart;

    // But leave a similarly named file with the test name to make identifying folders easier
    var sanitizedTestName = Regex.Replace(testName, "[^a-zA-Z_.]", "_");
    _testInfoFile = TestSolutionDirectory + "." + sanitizedTestName;
    _testInfoFile.WriteAllText("");

    TestSolutionDirectory.DeleteDirectory();

    FileSystemTasks.CopyDirectoryRecursively(testSolutionTemplateFolder, TestSolutionDirectory);

    TestSolutionModel = TestSolutionModel.Create(TestSolutionDirectory);
  }

  [TearDown]
  public void TearDown ()
  {
    // Do not delete folder in case of errors to promote debugging -> setup will clean up before the next test run
    var testOutcome = TestContext.CurrentContext.Result.Outcome;
    if (testOutcome != ResultState.Error && testOutcome != ResultState.Failure)
    {
      // Build process might still be exiting so we retry with delays before quiting
      for (int i = 5; i > 0; i--)
      {
        try
        {
          TestSolutionDirectory.DeleteDirectory();
          _testInfoFile.DeleteFile();
        }
        catch (Exception ex)
        {
          if (i == 0)
            throw;

          Thread.Sleep(500);
          continue;
        }
        break;
      }
    }
  }

  protected BuildRunResult RunBuildCmd (string arguments, bool ignoreNonStatusExitCode = false)
  {
    var processStartInfo = new ProcessStartInfo(Path.Combine(TestSolutionDirectory, "build.cmd"), $"{arguments}")
                           {
                               UseShellExecute = false,
                               StandardOutputEncoding = Encoding.UTF8,
                               RedirectStandardOutput = true,
                               StandardErrorEncoding = Encoding.UTF8,
                               RedirectStandardError = true,
                               WorkingDirectory = TestSolutionDirectory,
                           };
    processStartInfo.EnvironmentVariables.Add("NUKE_TEST_HOST", "true");

    var processOutput = new StringBuilder();

    var process = new Process();
    process.StartInfo = processStartInfo;
    var outputFinished = new AutoResetEvent(false);
    process.OutputDataReceived += (sender, args) =>
    {
      lock (processOutput)
      {
        if (args.Data != null)
          processOutput.AppendLine(args.Data);
        else
          outputFinished.Set();
      }
    };
    var errorFinished = new AutoResetEvent(false);
    process.ErrorDataReceived += (sender, args) =>
    {
      lock (processOutput)
      {
        if (args.Data != null)
          processOutput.AppendLine(args.Data);
        else
          errorFinished.Set();
      }
    };
    process.Start();

    process.BeginOutputReadLine();
    process.BeginErrorReadLine();

    const int timeout = 60_000;
    if (!process.WaitForExit(timeout) || !outputFinished.WaitOne(timeout) || !errorFinished.WaitOne(timeout))
    {
      Assert.Fail("build.cmd invocation did not finish within the timeout");
    }

    var sanitizedProcessOutput = SanitizeOutput(processOutput.ToString());
    if (Debugger.IsAttached || process.ExitCode != 0)
    {
      Console.WriteLine("build.cmd output:");
      Console.WriteLine(sanitizedProcessOutput);
    }

    if (!ignoreNonStatusExitCode)
      Assert.That(process.ExitCode, Is.EqualTo(0));

    return new BuildRunResult(process.ExitCode, sanitizedProcessOutput);
  }

  private string SanitizeOutput (string output)
  {
    output = Regex.Replace(output, @"\d\d:\d\d:\d\d", "xx:xx:xx");
    output = output.Replace(TestSolutionDirectory, "$SOLUTIONDIR$");

    return output;
  }
}
