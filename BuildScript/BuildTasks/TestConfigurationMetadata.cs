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

namespace Remotion.BuildScript.BuildTasks
{
  /// <summary>
  /// The names of the metadata the resulting TestConfiguration msbuild items have.
  /// </summary>
  public static class TestConfigurationMetadata
  {
    public const string Browser = "Browser";
    public const string DatabaseSystem = "DatabaseSystem";
    public const string ConfigurationID = "ConfigurationID";
    public const string Platform = "Platform";
    public const string ExecutionRuntimeValue = "ExecutionRuntimeValue";
    public const string ExecutionRuntimeKey = "ExecutionRuntimeKey";
    public const string DockerImage = "DockerImage";
    public const string DockerIsolationMode = "DockerIsolationMode";
    public const string IsWebTest = "IsWebTest";
    public const string IsDatabaseTest = "IsDatabaseTest";
    public const string Use32Bit = "Use32Bit";
    public const string TestAssemblyFileName = "TestAssemblyFileName";
    public const string TestAssemblyFullPath = "TestAssemblyFullPath";
    public const string TestAssemblyDirectoryName = "TestAssemblyDirectoryName";
    public const string TestingSetupBuildFile = "TestingSetupBuildFile";
    public const string UseDocker = "UseDocker";
    public const string TargetRuntime = "TargetRuntime";
    public const string ExcludeCategories = "ExcludeCategories";
    public const string IncludeCategories = "IncludeCategories";
  }
}