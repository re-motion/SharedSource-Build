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
using System.Collections.Immutable;
using Nuke.Common;
using Remotion.BuildScript.Test;
using Remotion.BuildScript.Test.Dimensions;

namespace Remotion.BuildScript;

public partial class RemotionBuild
{
  // todo parameter descriptions
  [Parameter(ValueProviderMember = nameof(SupportedTestConfigurations), Separator = "+")]
  public string[] TestConfigurations { get; set; } = Array.Empty<string>();

  [Parameter(ValueProviderMember = nameof(SupportedTestExecutionRuntimes), Separator = "+")]
  public string[] TestExecutionRuntimes { get; set; } = Array.Empty<string>();

  [Parameter(ValueProviderMember = nameof(SupportedTestPlatforms), Separator = "+")]
  public string[] TestPlatforms { get; set; } = Array.Empty<string>();

  [Parameter(ValueProviderMember = nameof(SupportedTestTargetRuntimes), Separator = "+")]
  public string[] TestTargetRuntimes { get; set; } = Array.Empty<string>();


  public TestSettings TestSettings { get; set; } = TestSettings.Default;

  public SupportedTestDimensions SupportedTestDimensions { get; set; } = default!;

  public EnabledTestDimensions EnabledTestDimensions { get; set; } = default!;

  public ImmutableArray<TestMatrix> TestMatrices { get; set; }


  public abstract void ConfigureProjects (ProjectsBuilder projects);

  public abstract void ConfigureSupportedTestDimensions (SupportedTestDimensionsBuilder supportedTestDimensions);

  public virtual void ConfigureEnabledTestDimensions (EnabledTestDimensionsBuilder enabledTestDimensions)
  {
    if (SupportedTestDimensions.IsSupported<Configurations>())
    {
      var testConfigurations = SupportedTestDimensions.ParseTestDimensionValuesOrDefault<Configurations>(TestConfigurations)
                               ?? throw CreateConfigurationException<Configurations>();

      enabledTestDimensions.AddEnabledDimension(testConfigurations);
    }

    if (SupportedTestDimensions.IsSupported<ExecutionRuntimes>())
    {
      var testExecutionRuntimes = SupportedTestDimensions.ParseTestDimensionValuesOrDefault<ExecutionRuntimes>(TestExecutionRuntimes)
                                  ?? throw CreateConfigurationException<ExecutionRuntimes>();

      enabledTestDimensions.AddEnabledDimension(testExecutionRuntimes);
    }

    if (SupportedTestDimensions.IsSupported<Platforms>())
    {
      var testPlatforms = SupportedTestDimensions.ParseTestDimensionValuesOrDefault<Platforms>(TestPlatforms)
                          ?? throw CreateConfigurationException<Platforms>();

      enabledTestDimensions.AddEnabledDimension(testPlatforms);
    }

    if (SupportedTestDimensions.IsSupported<TargetFrameworks>())
    {
      var testTargetRuntimes = SupportedTestDimensions.ParseTestDimensionValuesOrDefault<TargetFrameworks>(TestTargetRuntimes)
                               ?? throw CreateConfigurationException<TargetFrameworks>();

      enabledTestDimensions.AddEnabledDimension(testTargetRuntimes);
    }

    return;

    static InvalidOperationException CreateConfigurationException<T> ()
        where T : TestDimension
    {
      return new InvalidOperationException($"The configuration for test dimension '{typeof(T).Name}' cannot be empty.");
    }
  }

  public abstract void ConfigureTestMatrix (TestMatricesBuilder builder);
}