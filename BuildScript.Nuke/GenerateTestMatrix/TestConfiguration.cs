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

namespace Remotion.BuildScript.GenerateTestMatrix;

public class TestConfiguration
{
  public string ID { get; }
  public string Browser { get; }
  public string DatabaseSystem { get; }
  public string ConfigurationID { get; }
  public string Platform { get; }
  public ExecutionRuntime ExecutionRuntime { get; }
  public bool IsWebTest { get; }
  public bool IsDatabaseTest { get; }
  public bool Use32Bit { get; }
  public string TestAssemblyFileName { get; }
  public string TestAssemblyFullPath { get; }
  public string TestAssemblyDirectoryPath { get; }
  public string TestSetupBuildFile { get; }
  public string TargetRuntime { get; }
  public string TargetRuntimeMoniker { get; }
  public IReadOnlyCollection<string> ExcludeCategories { get; }
  public IReadOnlyCollection<string> IncludeCategories { get; }
  public bool IsNetCoreFramework { get; }
  public bool IsNetFramework { get; }
  public bool IsNetStandardFramework { get; }
  public string TestAssemblyFrameworkVersion { get; }
  public TestProjectMetadata ProjectMetadata { get; }

  public TestConfiguration (
      string id,
      string browser,
      bool isWebTest,
      string databaseSystem,
      bool isDatabaseTest,
      string configurationID,
      bool use32Bit,
      string platform,
      ExecutionRuntime executionRuntime,
      string testAssemblyFileName,
      string testAssemblyFullPath,
      string testAssemblyDirectoryPath,
      string testSetupBuildFile,
      string targetRuntime,
      string targetRuntimeMoniker,
      string excludeCategories,
      string includeCategories,
      TestProjectMetadata projectMetadata)
  {
    ID = id;
    Browser = browser;
    IsWebTest = isWebTest;
    DatabaseSystem = databaseSystem;
    IsDatabaseTest = isDatabaseTest;
    ConfigurationID = configurationID;
    Use32Bit = use32Bit;
    Platform = platform;
    ExecutionRuntime = executionRuntime;
    TestAssemblyFileName = testAssemblyFileName;
    TestAssemblyFullPath = testAssemblyFullPath;
    TestAssemblyDirectoryPath = testAssemblyDirectoryPath;
    TestSetupBuildFile = testSetupBuildFile;
    TargetRuntime = targetRuntime;
    TargetRuntimeMoniker = targetRuntimeMoniker;
    ExcludeCategories = string.IsNullOrEmpty(excludeCategories) ? Array.Empty<string>() : excludeCategories.Split(",");
    IncludeCategories = string.IsNullOrEmpty(includeCategories) ? Array.Empty<string>() : includeCategories.Split(",");
    ProjectMetadata = projectMetadata;
    TestAssemblyFrameworkVersion = projectMetadata.TargetFrameworks.Single();

    var isTargetFrameworkCoreProject = TestAssemblyFrameworkVersion.StartsWith("net") &&
                                       TestAssemblyFrameworkVersion.Contains('.');
    var firstVersionNumber = int.Parse(TargetRuntimeMoniker.Where(char.IsDigit).ToArray()[0] + "");
    var isTargetRuntimeCoreProject = firstVersionNumber >= 5;
    var isNetStandardProject = TestAssemblyFrameworkVersion.StartsWith("netstandard");
    IsNetCoreFramework = isTargetFrameworkCoreProject &&
                          isTargetRuntimeCoreProject;
    IsNetFramework = !isTargetFrameworkCoreProject && !isTargetRuntimeCoreProject && !isNetStandardProject;
    IsNetStandardFramework = isNetStandardProject;
  }

  public override int GetHashCode ()
  {
    var hashCode = new HashCode();
    hashCode.Add(ID);
    hashCode.Add(Browser);
    hashCode.Add(DatabaseSystem);
    hashCode.Add(ConfigurationID);
    hashCode.Add(Platform);
    hashCode.Add(ExecutionRuntime);
    hashCode.Add(IsWebTest);
    hashCode.Add(IsDatabaseTest);
    hashCode.Add(Use32Bit);
    hashCode.Add(TestAssemblyFileName);
    hashCode.Add(TestAssemblyFullPath);
    hashCode.Add(TestAssemblyDirectoryPath);
    hashCode.Add(TestSetupBuildFile);
    hashCode.Add(TargetRuntime);
    hashCode.Add(TargetRuntimeMoniker);
    hashCode.Add(ExcludeCategories);
    hashCode.Add(IncludeCategories);
    hashCode.Add(IsNetCoreFramework);
    hashCode.Add(IsNetFramework);
    hashCode.Add(TestAssemblyFrameworkVersion);
    hashCode.Add(ProjectMetadata);
    return hashCode.ToHashCode();
  }

  public override bool Equals (object? obj)
  {
    if (ReferenceEquals(null, obj))
      return false;
    if (ReferenceEquals(this, obj))
      return true;
    if (obj.GetType() != GetType())
      return false;
    return Equals((TestConfiguration) obj);
  }

  public override string ToString () =>
      $"{nameof(ID)}: {ID}, {nameof(Browser)}: {Browser}, {nameof(DatabaseSystem)}: {DatabaseSystem}, {nameof(ConfigurationID)}: {ConfigurationID}, {nameof(Platform)}: {Platform}, {nameof(ExecutionRuntime)}: {ExecutionRuntime}, {nameof(IsWebTest)}: {IsWebTest}, {nameof(IsDatabaseTest)}: {IsDatabaseTest}, {nameof(Use32Bit)}: {Use32Bit}, {nameof(TestAssemblyFileName)}: {TestAssemblyFileName}, {nameof(TestAssemblyFullPath)}: {TestAssemblyFullPath}, {nameof(TestAssemblyDirectoryPath)}: {TestAssemblyDirectoryPath}, {nameof(TestSetupBuildFile)}: {TestSetupBuildFile}, {nameof(TargetRuntime)}: {TargetRuntime}, {nameof(ExcludeCategories)}: {string.Join(",", ExcludeCategories)}, {nameof(IncludeCategories)}: {string.Join(",", IncludeCategories)}, {nameof(IsNetCoreFramework)}: {IsNetCoreFramework}, {nameof(TestAssemblyFrameworkVersion)}: {TestAssemblyFrameworkVersion}, {nameof(ProjectMetadata)}: {ProjectMetadata}";

  protected bool Equals (TestConfiguration other) => ID == other.ID && Browser == other.Browser && DatabaseSystem == other.DatabaseSystem
                                                     && ConfigurationID == other.ConfigurationID && Platform == other.Platform
                                                     && ExecutionRuntime.Equals(other.ExecutionRuntime) && IsWebTest == other.IsWebTest
                                                     && IsDatabaseTest == other.IsDatabaseTest && Use32Bit == other.Use32Bit
                                                     && TestAssemblyFileName == other.TestAssemblyFileName
                                                     && TestAssemblyFullPath == other.TestAssemblyFullPath
                                                     && TestAssemblyDirectoryPath == other.TestAssemblyDirectoryPath
                                                     && TestSetupBuildFile == other.TestSetupBuildFile && TargetRuntime == other.TargetRuntime
                                                     && TargetRuntimeMoniker == other.TargetRuntimeMoniker
                                                     && ExcludeCategories.Equals(other.ExcludeCategories)
                                                     && IncludeCategories.Equals(other.IncludeCategories)
                                                     && IsNetCoreFramework == other.IsNetCoreFramework && IsNetFramework == other.IsNetFramework
                                                     && TestAssemblyFrameworkVersion == other.TestAssemblyFrameworkVersion
                                                     && ProjectMetadata.Equals(other.ProjectMetadata);
}