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

using System.Collections.Generic;
using System.IO;
using Nuke.Common.IO;
using Nuke.Common.Utilities;
using NUnit.Framework;

namespace BuildScript.Nuke.IntegrationTests.Components;

[TestFixture]
public class CleanTest : IntegrationTestBase
{
  [Test]
  public void Clean_CleansSpecialFolders ()
  {
    var outputFolder = TestSolutionDirectory / "MyOutputFolder";
    var tempFolder = TestSolutionDirectory / "MyTempFolder";
    var logFolder = TestSolutionDirectory / "MyLogFolder";

    CreateDummyFolder(outputFolder);
    CreateDummyFolder(tempFolder);
    CreateDummyFolder(logFolder);

    RunBuildCmd(
        "Clean"
        + $" --OutputFolder {outputFolder.ToString().DoubleQuote()}"
        + $" --TempFolder {tempFolder.ToString().DoubleQuote()}"
        + $" --LogFolder {logFolder.ToString().DoubleQuote()}");

    Assert.That(Directory.Exists(outputFolder), Is.True);
    Assert.That(IsEmptyDirectory(outputFolder), Is.True);
    Assert.That(Directory.Exists(tempFolder), Is.True);
    Assert.That(IsEmptyDirectory(tempFolder), Is.True);
    Assert.That(Directory.Exists(logFolder), Is.True);
    Assert.That(IsEmptyDirectory(logFolder), Is.True);
  }

  [Test]
  public void Clean_CleansProjectFolders ()
  {
    var relevantPaths = new List<AbsolutePath>();
    foreach (var project in TestSolutionModel.Projects)
    {
      relevantPaths.Add(project.BinFolder);
      relevantPaths.Add(project.ObjFolder);
    }

    foreach (var relevantPath in relevantPaths)
    {
      CreateDummyFolder(relevantPath);
    }

    RunBuildCmd("Clean");

    foreach (var relevantPath in relevantPaths)
    {
      Assert.That(relevantPath.DirectoryExists(), Is.False);
    }
  }

  private static void CreateDummyFolder (AbsolutePath relevantFolder)
  {
    relevantFolder.CreateDirectory();
    (relevantFolder / "test.txt").WriteAllText("test");
    (relevantFolder / "sub").CreateDirectory();
    (relevantFolder / "sub" / "test2.txt").WriteAllText("test2");
    Assert.That(Directory.Exists(relevantFolder), Is.True);
  }

  private static bool IsEmptyDirectory (AbsolutePath absolutePath)
  {
    return Directory.GetFileSystemEntries(absolutePath).Length == 0;
  }
}
