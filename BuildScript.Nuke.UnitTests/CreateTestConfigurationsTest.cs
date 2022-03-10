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
using Nuke.Common.IO;
using NUnit.Framework;
using Remotion.BuildScript.Nuke.GenerateTestMatrix;

namespace BuildScript.Nuke.UnitTests;

[TestFixture]
public class CreateTestConfigurationsTest
{
  public CreateTestConfigurations CreateTestConfigurations (
      IReadOnlyCollection<TestProjectMetadata> projectMetadata,
      IReadOnlyCollection<string>? supportedBrowsers = null,
      IReadOnlyCollection<string>? supportedPlatforms = null,
      IReadOnlyCollection<string>? supportedDatabaseSystems = null,
      IReadOnlyCollection<string>? supportedExecutionRuntimes = null,
      IReadOnlyCollection<string>? supportedTargetRuntimes = null,
      IReadOnlyCollection<string>? enabledBrowsers = null,
      IReadOnlyCollection<string>? enabledDatabaseSystems = null,
      IReadOnlyCollection<string>? enabledExecutionRuntimes = null,
      IReadOnlyCollection<string>? enabledTargetRuntimes = null,
      IReadOnlyCollection<string>? enabledPlatforms = null,
      IReadOnlyCollection<string>? enabledConfigurationIDs = null
  )
  {
    return new CreateTestConfigurations(
        supportedBrowsers: supportedBrowsers ?? Array.Empty<string>(),
        supportedPlatforms: supportedPlatforms ?? new[] { "x86", "x64" },
        supportedDatabaseSystems: supportedDatabaseSystems ?? Array.Empty<string>(),
        supportedExecutionRuntimes: supportedExecutionRuntimes ?? Array.Empty<string>(),
        supportedTargetRuntimes: supportedTargetRuntimes ?? new[] { "NET45", "NET50", "NET461" },
        enabledBrowsers: enabledBrowsers ?? new[] { "NoBrowser" },
        enabledDatabaseSystems: enabledDatabaseSystems ?? new[] { "NoDB" },
        enabledTargetRuntimes: enabledTargetRuntimes ?? new[] { "NET48", "NET45", "NET50", "NET461" },
        enabledExecutionRuntimes: enabledExecutionRuntimes ?? new[] { "LocalMachine" },
        enabledPlatforms: enabledPlatforms ?? new[] { "x86", "x64" },
        enabledConfigurationIDs: enabledConfigurationIDs ?? new[] { "Debug", "Release" },
        testOutputFiles: projectMetadata
    );
  }

  public TestProjectMetadata CreateTestProjectMetadata (
      IReadOnlyList<string>? assemblyPaths = null,
      string configuration = "Debug",
      bool isMultiTargetFramework = false,
      bool isSdkProject = true,
      AbsolutePath? projectPath = null,
      List<string>? targetFrameworks = null,
      string testSetupBuildFile = "",
      string toolsVersion = "Current",
      string testConfiguration = "LocalMachine + NET45 + NoBrowser + NoDB + Debug + x86;"
  )
  {
    return new TestProjectMetadata
           {
               AssemblyPaths = assemblyPaths ?? new[] { "/x/net45/test.dll" },
               Configuration = configuration,
               IsMultiTargetFramework = isMultiTargetFramework,
               IsSdkProject = isSdkProject,
               ProjectPath = projectPath == null ? (AbsolutePath) "/x/Test.csproj" : projectPath,
               TargetFrameworks = targetFrameworks == null ? new[] { "net45" } : targetFrameworks,
               TestSetupBuildFile = testSetupBuildFile,
               ToolsVersion = toolsVersion,
               TestConfiguration = testConfiguration
           };
  }

  public TestConfiguration CreateTestConfiguration (
      string id,
      TestProjectMetadata projectMetadata,
      string browser = "NoBrowser",
      bool isWebTest = false,
      string databaseSystem = "NoDB",
      bool isDatabaseTest = false,
      string configurationID = "debug",
      bool use32Bit = true,
      string platform = "x86",
      ExecutionRuntime? executionRuntime = null,
      string testAssemblyFileName = "test.dll",
      string testAssemblyFullPath = "/x/test.dll",
      string testAssemblyDirectoryName = "\\x",
      string testSetupBuildFile = "",
      string targetRuntime = "NET-4.5",
      string targetRuntimeMoniker = "NET45",
      string excludeCategories = "",
      string includeCategories = ""
  )
  {
    return new TestConfiguration(
        id,
        browser,
        isWebTest,
        databaseSystem,
        isDatabaseTest,
        configurationID,
        use32Bit,
        platform,
        executionRuntime ?? new ExecutionRuntime("LocalMachine", "LocalMachine", false, ""),
        testAssemblyFileName,
        testAssemblyFullPath,
        testAssemblyDirectoryName,
        testSetupBuildFile,
        targetRuntime,
        targetRuntimeMoniker,
        excludeCategories,
        includeCategories,
        projectMetadata
    );
  }

  [Test]
  public void CreateTestMatrix_WithOneTestProjectAndTest_ReturnsOneValidTestConfiguration ()
  {
    var projectMetadata = new List<TestProjectMetadata>
                          {
                              CreateTestProjectMetadata(new[] { "/x/test.dll" })
                          };
    var createTestConfigurations = CreateTestConfigurations(projectMetadata);
    var expectedTestMatrix = new[]
    {
                                 CreateTestConfiguration(
                                     "test.dll+NoBrowser+NoDB+x86+LocalMachine+NET45+debug+IncludeCategories=None+ExcludeCategories=None",
                                     configurationID: "debug",
                                     targetRuntime: "NET-4.5",
                                     projectMetadata: projectMetadata.First()
                                 )
                             };
    var resultTestMatrix = createTestConfigurations.CreateTestMatrix();
    Assert.That(resultTestMatrix, Is.EqualTo(expectedTestMatrix));
  }

  [Test]
  public void CreateTestMatrix_WithTwoTestProjectsAndTest_ReturnsTwoValidTestConfigurations ()
  {
    var projectMetadata = new List<TestProjectMetadata>
                          {
                              CreateTestProjectMetadata(
                                  new[] { "/x/test.dll" },
                                  targetFrameworks: new List<string> { "net45" },
                                  testConfiguration:
                                  "LocalMachine + NET45 + NoBrowser + NoDB + Debug + x86;LocalMachine + NET50 + NoBrowser + NoDB + Debug + x64;"),

                              CreateTestProjectMetadata(
                                  new[] { "/x/test.dll" },
                                  targetFrameworks: new List<string> { "net5.0" },
                                  testConfiguration:
                                  "LocalMachine + NET45 + NoBrowser + NoDB + Debug + x86;LocalMachine + NET50 + NoBrowser + NoDB + Debug + x64;")
                          };
    var createTestConfigurations = CreateTestConfigurations(projectMetadata);
    var expectedTestMatrix = new[]
                             {
                                 CreateTestConfiguration(
                                     "test.dll+NoBrowser+NoDB+x86+LocalMachine+NET45+debug+IncludeCategories=None+ExcludeCategories=None",
                                     configurationID: "debug",
                                     targetRuntime: "NET-4.5",
                                     targetRuntimeMoniker: "NET45",
                                     platform: "x86",
                                     use32Bit: true,
                                     projectMetadata: projectMetadata[0]
                                 ),
                                 CreateTestConfiguration(
                                     "test.dll+NoBrowser+NoDB+x64+LocalMachine+NET50+debug+IncludeCategories=None+ExcludeCategories=None",
                                     configurationID: "debug",
                                     targetRuntime: "NET-5.0",
                                     targetRuntimeMoniker: "NET50",
                                     platform: "x64",
                                     use32Bit: false,
                                     projectMetadata: projectMetadata[1]
                                 )
                             };
    var resultTestMatrix = createTestConfigurations.CreateTestMatrix();
    Assert.That(resultTestMatrix, Is.EqualTo(expectedTestMatrix));
  }

  [Test]
  public void CreateTestMatrix_WithOneMultiTargetTestProjectAndTest_ReturnsTwoValidTestConfigurations ()
  {
    var projectMetadata = new List<TestProjectMetadata>
                          {
                              CreateTestProjectMetadata(
                                  new[] { "/net45/test.dll", "/net5.0/test.dll" },
                                  isMultiTargetFramework: true,
                                  targetFrameworks: new List<string> { "net45", "net5.0" },
                                  projectPath: (AbsolutePath) "/z/Test.csproj",
                                  testConfiguration:
                                  "LocalMachine + NET45 + NoBrowser + NoDB + Debug + x86; LocalMachine + NET50 + NoBrowser + NoDB + Debug + x64;")
                          };
    var createTestConfigurations = CreateTestConfigurations(projectMetadata);
    var expectedTestMatrix = new[]
                             {
                                 CreateTestConfiguration(
                                     "test.dll+NoBrowser+NoDB+x86+LocalMachine+NET45+debug+IncludeCategories=None+ExcludeCategories=None",
                                     configurationID: "debug",
                                     targetRuntime: "NET-4.5",
                                     targetRuntimeMoniker: "NET45",
                                     platform: "x86",
                                     use32Bit: true,
                                     testAssemblyFullPath: (AbsolutePath) "/net45/test.dll",
                                     testAssemblyDirectoryName: "\\net45",
                                     projectMetadata: CreateTestProjectMetadata(
                                         new[] { "/net45/test.dll" },
                                         isMultiTargetFramework: true,
                                         targetFrameworks: new List<string> { "net45" },
                                         projectPath: (AbsolutePath) "/z/Test.csproj",
                                         testConfiguration:
                                         "LocalMachine + NET45 + NoBrowser + NoDB + Debug + x86; LocalMachine + NET50 + NoBrowser + NoDB + Debug + x64;")
                                 ),
                                 CreateTestConfiguration(
                                     "test.dll+NoBrowser+NoDB+x64+LocalMachine+NET50+debug+IncludeCategories=None+ExcludeCategories=None",
                                     configurationID: "debug",
                                     targetRuntime: "NET-5.0",
                                     targetRuntimeMoniker: "NET50",
                                     platform: "x64",
                                     use32Bit: false,
                                     testAssemblyFullPath: (AbsolutePath) "/net5.0/test.dll",
                                     testAssemblyDirectoryName: "\\net5.0",
                                     projectMetadata: CreateTestProjectMetadata(
                                         new[] { "/net5.0/test.dll" },
                                         isMultiTargetFramework: true,
                                         targetFrameworks: new List<string> { "net5.0" },
                                         projectPath: (AbsolutePath) "/z/Test.csproj",
                                         testConfiguration:
                                         "LocalMachine + NET45 + NoBrowser + NoDB + Debug + x86; LocalMachine + NET50 + NoBrowser + NoDB + Debug + x64;")
                                 )
                             };
    var resultTestMatrix = createTestConfigurations.CreateTestMatrix();
    Assert.That(resultTestMatrix, Is.EqualTo(expectedTestMatrix));
  }

  [Test]
  public void CreateTestMatrix_WithOneTestProjectAndTest_ReturnsTwoValidTestConfigurations ()
  {
    var projectMetadata = new List<TestProjectMetadata>
                          {
                              CreateTestProjectMetadata(
                                  new[] { "/x/test.dll" },
                                  targetFrameworks: new List<string> { "net45" },
                                  projectPath: (AbsolutePath) "/x/Test.csproj",
                                  testConfiguration:
                                  "LocalMachine + NET45 + NoBrowser + NoDB + Debug + x86; LocalMachine + NET45 + NoBrowser + NoDB + Debug + x64;"
                              )
                          };

    var createTestConfigurations = CreateTestConfigurations(projectMetadata);
    var expectedTestMatrix = new[]
                             {
                                 CreateTestConfiguration(
                                     "test.dll+NoBrowser+NoDB+x86+LocalMachine+NET45+debug+IncludeCategories=None+ExcludeCategories=None",
                                     configurationID: "debug",
                                     targetRuntime: "NET-4.5",
                                     platform: "x86",
                                     use32Bit: true,
                                     projectMetadata: projectMetadata[0]
                                 ),
                                 CreateTestConfiguration(
                                     "test.dll+NoBrowser+NoDB+x64+LocalMachine+NET45+debug+IncludeCategories=None+ExcludeCategories=None",
                                     configurationID: "debug",
                                     targetRuntime: "NET-4.5",
                                     platform: "x64",
                                     use32Bit: false,
                                     projectMetadata: projectMetadata[0]
                                 )
                             };
    var resultTestMatrix = createTestConfigurations.CreateTestMatrix();
    Assert.That(resultTestMatrix, Is.EqualTo(expectedTestMatrix));
  }

  [Test]
  public void CreateTestMatrix_WithNotSupportedTargetFramework_ThrowsApplicationException ()
  {
    var projectMetadata = new List<TestProjectMetadata>
                          {
                              CreateTestProjectMetadata(
                                  new[] { "/x/test.dll" },
                                  targetFrameworks: new List<string> { "net45" },
                                  projectPath: (AbsolutePath) "/x/Test.csproj",
                                  testConfiguration:
                                  "LocalMachine + NET45 + NoBrowser + NoDB + Debug + x86; LocalMachine + NET60 + NoBrowser + NoDB + Debug + x64;"
                              )
                          };
    var createTestConfigurations = CreateTestConfigurations(
        projectMetadata,
        supportedTargetRuntimes: new[] { "NET45", "NET50" }
    );
    Assert.That(() => createTestConfigurations.CreateTestMatrix(), Throws.InstanceOf<ApplicationException>());
  }

  [Test]
  public void CreateTestMatrix_WithEmptyEnabledTargetFrameworks_ThrowsInvalidOperationException ()
  {
    var projectMetadata = new List<TestProjectMetadata>
                          {
                              CreateTestProjectMetadata(
                                  new[] { "/x/test.dll" },
                                  targetFrameworks: new List<string> { "net48" },
                                  projectPath: (AbsolutePath) "/x/Test.csproj",
                                  testConfiguration:
                                  "LocalMachine + NET45 + NoBrowser + NoDB + Debug + x86; LocalMachine + NET48 + NoBrowser + NoDB + Debug + x64;"
                              )
                          };
    var createTestConfigurations = CreateTestConfigurations(
        projectMetadata,
        supportedTargetRuntimes: new[] { "NET45", "NET48" },
        enabledTargetRuntimes: Array.Empty<string>()
    );
    Assert.That(() => createTestConfigurations.CreateTestMatrix(), Throws.InvalidOperationException);
  }

  [Test]
  public void CreateTestMatrix_WithInvalidEnabledTargetFrameworks_ReturnsEmptyResult ()
  {
    var projectMetadata = new List<TestProjectMetadata>
                          {
                              CreateTestProjectMetadata(
                                  new[] { "/x/test.dll" },
                                  targetFrameworks: new List<string> { "net48" },
                                  projectPath: (AbsolutePath) "/x/Test.csproj",
                                  testConfiguration:
                                  "LocalMachine + NET45 + NoBrowser + NoDB + Debug + x86; LocalMachine + NET48 + NoBrowser + NoDB + Debug + x64;"
                              )
                          };
    var createTestConfigurations = CreateTestConfigurations(
        projectMetadata,
        supportedTargetRuntimes: new[] { "NET45", "NET48" },
        enabledTargetRuntimes: new[] { "NET50" }
    );
    Assert.That(createTestConfigurations.CreateTestMatrix(), Is.Empty);
  }

  [Test]
  public void CreateTestMatrix_WithTwoTestProjectsAndOneEnabledTargetRuntime_ReturnsOneValidTestConfiguration ()
  {
    var projectMetadata = new List<TestProjectMetadata>
                          {
                              CreateTestProjectMetadata(
                                  new[] { "/x/test.dll" },
                                  targetFrameworks: new List<string> { "net45" },
                                  projectPath: (AbsolutePath) "/x/Test.csproj",
                                  testConfiguration:
                                  "LocalMachine + NET45 + NoBrowser + NoDB + Debug + x86; LocalMachine + NET50 + NoBrowser + NoDB + Debug + x64;"
                              ),

                              CreateTestProjectMetadata(
                                  new[] { "/x/test.dll" },
                                  targetFrameworks: new List<string> { "net5.0" },
                                  projectPath: (AbsolutePath) "/x/Test.csproj",
                                  testConfiguration:
                                  "LocalMachine + NET45 + NoBrowser + NoDB + Debug + x86; LocalMachine + NET50 + NoBrowser + NoDB + Debug + x64;"
                              )
                          };

    var createTestConfigurations = CreateTestConfigurations(
        projectMetadata,
        supportedTargetRuntimes: new[] { "NET50", "NET45" },
        enabledTargetRuntimes: new[] { "NET50" }
    );
    var expectedTestMatrix = new[]
                             {
                                 CreateTestConfiguration(
                                     "test.dll+NoBrowser+NoDB+x64+LocalMachine+NET50+debug+IncludeCategories=None+ExcludeCategories=None",
                                     configurationID: "debug",
                                     targetRuntime: "NET-5.0",
                                     targetRuntimeMoniker: "NET50",
                                     platform: "x64",
                                     use32Bit: false,
                                     projectMetadata: projectMetadata[1]
                                 )
                             };
    var resultTestMatrix = createTestConfigurations.CreateTestMatrix();
    Assert.That(resultTestMatrix, Is.EqualTo(expectedTestMatrix));
  }

  [Test]
  public void CreateTestMatrix_WithTwoTestProjectsAndOneEnabledDatabaseSystems_ReturnsOneValidTestConfiguration ()
  {
    var projectMetadata = new List<TestProjectMetadata>
                          {
                              CreateTestProjectMetadata(
                                  new[] { "/x/test.dll" },
                                  targetFrameworks: new List<string> { "net45" },
                                  projectPath: (AbsolutePath) "/x/Test.csproj",
                                  testConfiguration:
                                  "LocalMachine + NET45 + NoBrowser + MSSQL + Debug + x86; LocalMachine + NET50 + NoBrowser + MARIADB + Debug + x64;"
                              ),

                              CreateTestProjectMetadata(
                                  new[] { "/x/test.dll" },
                                  targetFrameworks: new List<string> { "net5.0" },
                                  projectPath: (AbsolutePath) "/x/Test.csproj",
                                  testConfiguration:
                                  "LocalMachine + NET45 + NoBrowser + MSSQL + Debug + x86; LocalMachine + NET50 + NoBrowser + MARIADB + Debug + x64;"
                              )
                          };

    var createTestConfigurations = CreateTestConfigurations(
        projectMetadata,
        supportedDatabaseSystems: new[] { "MSSQL", "MARIADB" },
        enabledDatabaseSystems: new[] { "MARIADB" }
    );

    var expectedTestMatrix = new[]
                             {
                                 CreateTestConfiguration(
                                     "test.dll+NoBrowser+MARIADB+x64+LocalMachine+NET50+debug+IncludeCategories=None+ExcludeCategories=None",
                                     configurationID: "debug",
                                     targetRuntime: "NET-5.0",
                                     targetRuntimeMoniker: "NET50",
                                     platform: "x64",
                                     use32Bit: false,
                                     databaseSystem: "MARIADB",
                                     isDatabaseTest: true,
                                     projectMetadata: projectMetadata[1]
                                 )
                             };
    var resultTestMatrix = createTestConfigurations.CreateTestMatrix();
    Assert.That(resultTestMatrix, Is.EqualTo(expectedTestMatrix));
  }

  [Test]
  public void CreateTestMatrix_WithTwoTestProjectsAndOneEnabledExecutionRuntimes_ReturnsOneValidTestConfiguration ()
  {
    var projectMetadata = new List<TestProjectMetadata>
                          {
                              CreateTestProjectMetadata(
                                  new[] { "/x/test.dll" },
                                  targetFrameworks: new List<string> { "net45" },
                                  projectPath: (AbsolutePath) "/x/Test.csproj",
                                  testConfiguration:
                                  "LocalMachine + NET45 + NoBrowser + NoDB + Debug + x86; Win_NET48 + NET50 + NoBrowser + NoDB + Debug + x64;"
                              ),

                              CreateTestProjectMetadata(
                                  new[] { "/x/test.dll" },
                                  targetFrameworks: new List<string> { "net5.0" },
                                  projectPath: (AbsolutePath) "/x/Test.csproj",
                                  testConfiguration:
                                  "LocalMachine + NET45 + NoBrowser + NoDB + Debug + x86; Win_NET48 + NET50 + NoBrowser + NoDB + Debug + x64;"
                              )
                          };

    var createTestConfigurations = CreateTestConfigurations(
        projectMetadata,
        supportedExecutionRuntimes: new[] { "Win_NET48=Win_NET48" },
        enabledExecutionRuntimes: new[] { "Win_NET48" }
    );

    var expectedTestMatrix = new[]
                             {
                                 CreateTestConfiguration(
                                     "test.dll+NoBrowser+NoDB+x64+Win_NET48+NET50+debug+IncludeCategories=None+ExcludeCategories=None",
                                     configurationID: "debug",
                                     targetRuntime: "NET-5.0",
                                     targetRuntimeMoniker: "NET50",
                                     platform: "x64",
                                     use32Bit: false,
                                     executionRuntime: new ExecutionRuntime("Win_NET48", "Win_NET48", true, "Win_NET48"),
                                     projectMetadata: projectMetadata[1]
                                 )
                             };
    var resultTestMatrix = createTestConfigurations.CreateTestMatrix();
    Assert.That(resultTestMatrix, Is.EqualTo(expectedTestMatrix));
  }

  [Test]
  public void CreateTestMatrix_WithTwoTestProjectsAndOneEnabledPlatforms_ReturnsOneValidTestConfiguration ()
  {
    var projectMetadata = new List<TestProjectMetadata>
                          {
                              CreateTestProjectMetadata(
                                  new[] { "/x/test.dll" },
                                  targetFrameworks: new List<string> { "net45" },
                                  projectPath: (AbsolutePath) "/x/Test.csproj",
                                  testConfiguration:
                                  "LocalMachine + NET45 + NoBrowser + NoDB + Debug + x86; LocalMachine + NET50 + NoBrowser + NoDB + Debug + x64;"
                              ),

                              CreateTestProjectMetadata(
                                  new[] { "/x/test.dll" },
                                  targetFrameworks: new List<string> { "net5.0" },
                                  projectPath: (AbsolutePath) "/x/Test.csproj",
                                  testConfiguration:
                                  "LocalMachine + NET45 + NoBrowser + NoDB + Debug + x86; LocalMachine + NET50 + NoBrowser + NoDB + Debug + x64;"
                              )
                          };

    var createTestConfigurations = CreateTestConfigurations(
        projectMetadata,
        supportedPlatforms: new[] { "x86", "x64" },
        enabledPlatforms: new[] { "x64" }
    );

    var expectedTestMatrix = new[]
                             {
                                 CreateTestConfiguration(
                                     "test.dll+NoBrowser+NoDB+x64+LocalMachine+NET50+debug+IncludeCategories=None+ExcludeCategories=None",
                                     configurationID: "debug",
                                     targetRuntime: "NET-5.0",
                                     targetRuntimeMoniker: "NET50",
                                     platform: "x64",
                                     use32Bit: false,
                                     projectMetadata: projectMetadata[1]
                                 )
                             };
    var resultTestMatrix = createTestConfigurations.CreateTestMatrix();
    Assert.That(resultTestMatrix, Is.EqualTo(expectedTestMatrix));
  }

  [Test]
  public void CreateTestMatrix_WithTwoTestProjectsAndOneEnabledBrowser_ReturnsOneValidTestConfiguration ()
  {
    var projectMetadata = new List<TestProjectMetadata>
                          {
                              CreateTestProjectMetadata(
                                  new[] { "/x/test.dll" },
                                  targetFrameworks: new List<string> { "net45" },
                                  projectPath: (AbsolutePath) "/x/Test.csproj",
                                  testConfiguration:
                                  "LocalMachine + NET45 + Chrome + NoDB + Debug + x86; LocalMachine + NET50 + FireFox + NoDB + Debug + x64;"
                              ),
                              CreateTestProjectMetadata(
                                  new[] { "/x/test.dll" },
                                  targetFrameworks: new List<string> { "net5.0" },
                                  projectPath: (AbsolutePath) "/x/Test.csproj",
                                  testConfiguration:
                                  "LocalMachine + NET45 + Chrome + NoDB + Debug + x86; LocalMachine + NET50 + FireFox + NoDB + Debug + x64;"
                              )
                          };

    var createTestConfigurations = CreateTestConfigurations(
        projectMetadata,
        new[] { "Chrome", "FireFox" },
        enabledBrowsers: new[] { "FireFox" }
    );

    var expectedTestMatrix = new[]
                             {
                                 CreateTestConfiguration(
                                     "test.dll+FireFox+NoDB+x64+LocalMachine+NET50+debug+IncludeCategories=None+ExcludeCategories=None",
                                     configurationID: "debug",
                                     targetRuntime: "NET-5.0",
                                     targetRuntimeMoniker: "NET50",
                                     platform: "x64",
                                     use32Bit: false,
                                     browser: "FireFox",
                                     isWebTest: true,
                                     projectMetadata: projectMetadata[1]
                                 )
                             };
    var resultTestMatrix = createTestConfigurations.CreateTestMatrix();
    Assert.That(resultTestMatrix, Is.EqualTo(expectedTestMatrix));
  }
}