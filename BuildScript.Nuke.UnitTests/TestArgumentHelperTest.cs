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
using Nuke.Common.IO;
using NUnit.Framework;
using Remotion.BuildScript;
using Remotion.BuildScript.GenerateTestMatrix;

namespace BuildScript.Nuke.UnitTests;

[TestFixture]
public class TestArgumentHelperTest
{
  [Test]
  [TestCase(
      new[] { "exclude1", "exclude2" },
      new[] { "include1", "include2" },
      "SQL",
      "debug",
      "x64",
      "NET45",
      "testAssemblyFileName",
      "NET45",
      "testAssemblyFileName.debug.x64.NET45.NET45.SQL.Exclude-exclude1-exclude2.Include-include1-include2"
  )]
  [TestCase(
      new string[0],
      new string[0],
      "SQL",
      "debug",
      "x64",
      "NET45",
      "testAssemblyFileName",
      "NET45",
      "testAssemblyFileName.debug.x64.NET45.NET45.SQL"
  )]
  [TestCase(
      new string[0],
      new[] { "include1", "include2" },
      MetadataValueConstants.NoDB,
      "debug",
      "x64",
      "NET5",
      "testAssemblyFileName",
      "NET5",
      "testAssemblyFileName.debug.x64.NET5.NET5.Include-include1-include2"
  )]
  [TestCase(
      new string[0],
      new string[0],
      MetadataValueConstants.NoDB,
      "release",
      "x86",
      "NET22",
      "testFileName",
      "NET45",
      "testFileName.release.x86.NET22.NET45"
  )]
  public void CreateTestName_WithValidInput_ReturnsValidTestName (
      IReadOnlyCollection<string> mergedTestCategoriesToExclude,
      IReadOnlyCollection<string> mergedTestCategoriesToInclude,
      string databaseSystem,
      string configurationId,
      string platform,
      string executionRuntimeKey,
      string testAssemblyFileName,
      string targetRuntime,
      string expectedResult)
  {
    var testConfiguration = new TestConfiguration(
        "",
        "",
        true,
        databaseSystem,
        true,
        configurationId,
        true,
        platform,
        new ExecutionRuntime(executionRuntimeKey, "", true, ""),
        testAssemblyFileName,
        (AbsolutePath) "/x/y",
        "",
        "",
        targetRuntime,
        "NET45",
        "",
        "",
        new TestProjectMetadata
        {
            AssemblyPaths = new[] { "/x/test.dll" },
            Configuration = "Debug",
            IsMultiTargetFramework = false,
            IsSdkProject = true,
            ProjectPath = (AbsolutePath) "/x/Test.csproj",
            TargetFrameworks = new List<string> { "net45" },
            TestSetupBuildFile = "",
            ToolsVersion = "Current",
            TestConfiguration = @"LocalMachine + NET45 + NoBrowser + NoDB + Debug + x86;
                                                        LocalMachine + NET60 + NoBrowser + NoDB + Debug + x64;"
        });
    var testArgumentHelper = new TestArgumentHelper();
    var result = testArgumentHelper.CreateTestName(
        testConfiguration,
        mergedTestCategoriesToExclude,
        mergedTestCategoriesToInclude);
    Assert.That(result, Is.EqualTo(expectedResult));
  }

  [Test]
  [TestCase(
      new[] { "include1", "include2", "include3" },
      new[] { "exclude1", "exclude2", "exclude3" },
      "(TestCategory!=exclude1&TestCategory!=exclude2&TestCategory!=exclude3)&(TestCategory=include1|TestCategory=include2|TestCategory=include3)")]
  [TestCase(
      new string[0],
      new[] { "exclude1", "exclude2", "exclude3", "exclude4" },
      "TestCategory!=exclude1&TestCategory!=exclude2&TestCategory!=exclude3&TestCategory!=exclude4")]
  [TestCase(
      new[] { "include1", "include2", "include3" },
      new string[0],
      "TestCategory=include1|TestCategory=include2|TestCategory=include3")]
  [TestCase(
      new string[0],
      new string[0],
      "")]
  public void CreateDotNetTestFilter_WithValidInput_ReturnsValidTestFiler (
      string[] include,
      string[] exclude,
      string expectedResult)
  {
    var testArgumentHelper = new TestArgumentHelper();
    var result = testArgumentHelper.CreateDotNetTestFilter(exclude, include);
    Assert.That(result, Is.EqualTo(expectedResult));
  }

  [Test]
  [TestCase(
      new[] { "include1", "include2", "include3" },
      "|",
      true,
      "TestCategory=include1|TestCategory=include2|TestCategory=include3")]
  [TestCase(
      new[] { "exclude1", "exclude2", "exclude3", "exclude4" },
      "&",
      false,
      "TestCategory!=exclude1&TestCategory!=exclude2&TestCategory!=exclude3&TestCategory!=exclude4")]
  [TestCase(
      new[] { "exclude1", "exclude2", "exclude3", "exclude4" },
      "<3",
      false,
      "TestCategory!=exclude1<3TestCategory!=exclude2<3TestCategory!=exclude3<3TestCategory!=exclude4")]
  [TestCase(
      new string[0],
      " || ",
      false,
      "")]
  public void CreateDotNetFilter_WithValidInput_ReturnsValidFiler (
      IReadOnlyCollection<string> include,
      string joinSymbol,
      bool equals,
      string expectedResult)
  {
    var testArgumentHelper = new TestArgumentHelper();
    var result = testArgumentHelper.CreateDotNetFilter(include, joinSymbol, equals);
    Assert.That(result, Is.EqualTo(expectedResult));
  }
}