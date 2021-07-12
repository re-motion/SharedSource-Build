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
namespace Remotion.BuildScript.BuildTasks
{
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
    public string TestAssemblyDirectoryName { get; }
    public string TestSetupBuildFile { get; }
    public string TargetRuntime { get; }
    public string ExcludeCategories { get; }
    public string IncludeCategories { get; }

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
        string testAssemblyDirectoryName,
        string testSetupBuildFile,
        string targetRuntime,
        string excludeCategories,
        string includeCategories)
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
      TestAssemblyDirectoryName = testAssemblyDirectoryName;
      TestSetupBuildFile = testSetupBuildFile;
      TargetRuntime = targetRuntime;
      ExcludeCategories = excludeCategories;
      IncludeCategories = includeCategories;
    }
  }
}