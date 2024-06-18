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

using System.Linq;
using JetBrains.Annotations;
using Nuke.Common;
using Remotion.BuildScript.Test;
using Remotion.BuildScript.Test.Dimensions;
using Serilog;

namespace Remotion.BuildScript.Components;

public interface ITestSettings : IBaseBuild
{
  public TestSettings TestSettings { get; set; }

  [PublicAPI]
  public Target VerifyTestParameters => _ => _
      .TryDependsOn<ITestMatrix>()
      .Executes(() =>
      {
        var parameters = new TestParameterBuilder();

        // If this build supports test matrix builds we can check the calculated matrix for test parameter requirements
        if (this is ITestMatrix testMatrix)
        {
          Log.Information("Verifying implicit test parameter dependencies of test matrix.");

          // Check the execution runtimes for test parameter requirements
          var requiredExecutionRuntimes = testMatrix.TestMatrices
              .SelectMany(e => e.TestConfigurations)
              .SelectMany(e => e.Elements)
              .OfType<ExecutionRuntimes>();

          foreach (var executionRuntime in requiredExecutionRuntimes)
          {
            var testExecutionRuntime = TestSettings.ExecutionRuntimeFactory.CreateTestExecutionRuntime(executionRuntime);
            if (testExecutionRuntime is IRequiresTestParameters verifyTestSettings)
              verifyTestSettings.ConfigureTestParameters(parameters);
          }

          // Check the test dimensions for test parameter requirements
          var testDimensionsWithParameters = testMatrix.TestMatrices
              .SelectMany(e => e.TestConfigurations)
              .SelectMany(e => e.Elements)
              .OfType<IRequiresTestParameters>()
              .Distinct();

          foreach (var testDimensionsWithParameter in testDimensionsWithParameters)
            testDimensionsWithParameter.ConfigureTestParameters(parameters);
        }

        // Extension point for custom test parameter requirements
        ConfigureTestParameters(parameters);

        TestSettings = TestSettings with
                       {
                           TestParameters = parameters.Build(TestSettings.TestParameters)
                       };
        Log.Information("Verified test settings.");
      });

  void ConfigureTestParameters (TestParameterBuilder parameters)
  {
  }
}