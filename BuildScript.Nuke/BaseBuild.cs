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
using Nuke.Common;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;

namespace Remotion.BuildScript;

[CheckBuildProjectConfigurations]
public partial class BaseBuild : NukeBuild
{
  [GitRepository]
  protected readonly GitRepository GitRepository = null!;
  [Solution]
  protected readonly Solution Solution = null!;

  private Directories Directories { get; } = new(RootDirectory, BuildProjectDirectory);
  private AssemblyMetadata AssemblyMetadata { get; set; } = null!;
  private SemanticVersion SemanticVersion { get; set; } = null!;

  private IReadOnlyCollection<ProjectMetadata> ReleaseProjectFiles { get; set; } = Array.Empty<ProjectMetadata>();
  private IReadOnlyCollection<TestProjectMetadata> UnitTestProjectFiles { get; set; } = Array.Empty<TestProjectMetadata>();
  private IReadOnlyCollection<TestProjectMetadata> IntegrationTestProjectFiles { get; set; } = Array.Empty<TestProjectMetadata>();
  private IReadOnlyCollection<TestProjectMetadata> TestProjectFiles { get; set; } = Array.Empty<TestProjectMetadata>();

  [Parameter("Skip generation of nuget package with debug symbols - true / false")]
  protected bool SkipNuGet { get; set; }

  [Parameter("Skip generation of nuget package with symbol server support - true / false")]
  protected bool SkipNuGetOrg { get; set; }

  [Parameter("Skip compiling and running of tests - true / false")]
  protected bool SkipTests { get; set; }
  
  [Parameter("Skip generating documentation - true / false")]
  protected bool SkipDocumentation { get; set; }

  [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
  protected string[] Configuration { get; set; } = { "Debug", "Release" };

  [Parameter("Browser available for the build to use for test running")]
  protected string[] Browsers { get; set; } = { "NoBrowser" };

  [Parameter("Target runtimes available for the build to use for test running")]
  protected string[] TargetRuntimes { get; set; } = { "NET48", "NET45", "NET50", "NET461" };

  [Parameter("Execution runtimes available for the build to use for test running")]
  protected string[] ExecutionRuntimes { get; set; } = { "LocalMachine" };

  [Parameter("Database runtimes available for the build to use for test running")]
  protected string[] DatabaseSystems { get; set; } = { "NoDB" };

  [Parameter("Platforms available for the build to use for test running")]
  protected string[] Platforms { get; set; } = { "x86", "x64" };

  [Parameter("Test Categories to exclude for test running")]
  protected string[] TestCategoriesToExclude { get; set; } = { };

  [Parameter("Test Categories to include for test running")]
  protected string[] TestCategoriesToInclude { get; set; } = { };
}