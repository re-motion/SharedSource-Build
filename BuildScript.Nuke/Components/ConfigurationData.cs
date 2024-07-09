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

namespace Remotion.BuildScript.Components;

public class ConfigurationData
{
  public string NormalTestConfiguration { get; init; } = "";
  public IReadOnlyCollection<string> SupportedTargetRuntimes { get; init; } = Array.Empty<string>();
  public IReadOnlyCollection<string> SupportedExecutionRuntimes { get; init; } = Array.Empty<string>();
  public IReadOnlyCollection<string> SupportedBrowsers { get; init; } = Array.Empty<string>();
  public IReadOnlyCollection<string> SupportedDatabaseSystems { get; init; } = Array.Empty<string>();
  public IReadOnlyCollection<string> SupportedPlatforms { get; init; } = Array.Empty<string>();
  public IReadOnlyCollection<ProjectMetadata> ReleaseProjectFiles { get; init; } = Array.Empty<ProjectMetadata>();
  public IReadOnlyCollection<TestProjectMetadata> UnitTestProjectFiles { get; init; } = Array.Empty<TestProjectMetadata>();
  public IReadOnlyCollection<TestProjectMetadata> IntegrationTestProjectFiles { get; init; } = Array.Empty<TestProjectMetadata>();
  public IReadOnlyCollection<TestProjectMetadata> TestProjectFiles { get; init; } = Array.Empty<TestProjectMetadata>();
  public AssemblyMetadata AssemblyMetadata { get; init; } = null!;
  public SemanticVersion SemanticVersion { get; init; } = null!;
}
