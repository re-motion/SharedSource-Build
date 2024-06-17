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
using JetBrains.Annotations;
using Nuke.Common;
using Remotion.BuildScript.TestMatrix;
using Serilog;

namespace Remotion.BuildScript.Components;

public interface ITestMatrix : IBaseBuild
{
  public SupportedTestDimensions SupportedTestDimensions { get; set; }

  public EnabledTestDimensions EnabledTestDimensions { get; set; }

  public ImmutableArray<TestMatrix.TestMatrix> TestMatrices { get; set; }

  [PublicAPI]
  public Target CreateTestMatrix => _ => _
      .Description("Creates the test matrix")
      .Executes(() =>
      {
        var supportedTestDimensionsBuilder = new SupportedTestDimensionsBuilder();
        ConfigureSupportedTestDimensions(supportedTestDimensionsBuilder);
        SupportedTestDimensions = supportedTestDimensionsBuilder.Build();

        var enabledTestDimensionsBuilder = new EnabledTestDimensionsBuilder();
        ConfigureEnabledTestDimensions(enabledTestDimensionsBuilder);
        EnabledTestDimensions = enabledTestDimensionsBuilder.Build();

        var testMatricesBuilder = new TestMatricesBuilder(SupportedTestDimensions, EnabledTestDimensions);
        ConfigureTestMatrix(testMatricesBuilder);

        var testMatrices = testMatricesBuilder.Build();
        Log.Information($"Created {testMatrices.Length} test matrices:");
        foreach (var testMatrix in testMatrices)
          Log.Information(testMatrix.ToString());

        TestMatrices = testMatrices;
      });

  void ConfigureSupportedTestDimensions (SupportedTestDimensionsBuilder supportedTestDimensions);

  void ConfigureEnabledTestDimensions (EnabledTestDimensionsBuilder enabledTestDimensions);

  void ConfigureTestMatrix (TestMatricesBuilder builder);
}