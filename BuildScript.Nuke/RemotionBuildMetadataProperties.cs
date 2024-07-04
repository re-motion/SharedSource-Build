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

using Remotion.BuildScript.Test;

namespace Remotion.BuildScript;

public static class RemotionBuildMetadataProperties
{
  // General
  public static readonly ProjectMetadataProperty<string> AssemblyName = ProjectMetadataProperty.Create<string>(nameof(AssemblyName));
  public static readonly ProjectMetadataProperty<TargetFrameworkSet> TargetFrameworks = ProjectMetadataProperty.Create<TargetFrameworkSet>(nameof(TargetFrameworks));

  // Testing
  public static readonly ProjectMetadataProperty<TestConfiguration> TestConfiguration = ProjectMetadataProperty.Create<TestConfiguration>(nameof(TestConfiguration));

  // Documentation
  public static readonly ProjectMetadataProperty<bool> CreateDocumentationFile = ProjectMetadataProperty.Create<bool>(nameof(CreateDocumentationFile));
  public static readonly ProjectMetadataProperty<bool> ExcludeFromDocumentation = ProjectMetadataProperty.Create<bool>(nameof(ExcludeFromDocumentation));

  // Packaging
  public static readonly ProjectMetadataProperty<bool> CreateNugetPackage = ProjectMetadataProperty.CreateWithDefault<bool>(nameof(CreateNugetPackage), false);
  public static readonly ProjectMetadataProperty<bool> CreateNugetPackageWithSymbolServerSupport = ProjectMetadataProperty.Create<bool>(nameof(CreateNugetPackageWithSymbolServerSupport));
}