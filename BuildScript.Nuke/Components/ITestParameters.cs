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
using System.Linq;
using JetBrains.Annotations;
using Nuke.Common;
using Remotion.BuildScript.Test;
using Serilog;

namespace Remotion.BuildScript.Components;

public interface ITestParameters : IBaseBuild, IProjectMetadata
{
  public ImmutableDictionary<string, string> TestParameters { get; set; }

  [PublicAPI]
  public Target VerifyTestParameters => _ => _
      .TryDependsOn<IProjectMetadata>()
      .Executes(() =>
      {
        var parameters = this is ITestMatrix testMatrix
            ? new TestParameterBuilder(testMatrix.SupportedTestDimensions, testMatrix.EnabledTestDimensions)
            : new TestParameterBuilder();

        var testConfigurations = ProjectMetadata.Select(e => e.GetMetadataOrDefault(RemotionBuildMetadataProperties.TestConfiguration))
            .Where(e => e != null && !e.TestMatrix.IsEmpty)
            .Distinct()
            .Select(e => e!)
            .ToArray();

        var relevantRequiresTestParameters = Array.Empty<IRequiresTestParameters>()
            .Concat(
                testConfigurations
                    .Select(e => e.TestExecutionRuntimeFactory)
                    .OfType<IRequiresTestParameters>()
            )
            .Concat(
                testConfigurations
                    .Select(e => e.TestMatrix)
                    .SelectMany(e => e.Rows)
                    .SelectMany(e => e.Elements)
                    // ReSharper disable once SuspiciousTypeConversion.Global
                    .OfType<IRequiresTestParameters>()
            )
            .Concat(
                testConfigurations
                    .SelectMany(e => e.TestExecutionWrappers)
                    // ReSharper disable once SuspiciousTypeConversion.Global
                    .OfType<IRequiresTestParameters>()
            )
            .Distinct();

        foreach (var requiresTestParameters in relevantRequiresTestParameters)
          requiresTestParameters.ConfigureTestParameters(parameters);

        // Extension point for custom test parameter requirements
        ConfigureTestParameters(parameters);

        TestParameters = parameters.Build();
        Log.Information("Verified test parameters:");
        foreach (var (key, value) in TestParameters.OrderBy(e => e.Key))
          Log.Information($" - ['{key}'] = '{value}'");
      });

  void ConfigureTestParameters (TestParameterBuilder parameters)
  {
  }
}