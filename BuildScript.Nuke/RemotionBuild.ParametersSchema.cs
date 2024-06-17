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

using System.Collections.Generic;
using System.Linq;
using Remotion.BuildScript.TestMatrix;
using Remotion.BuildScript.TestMatrix.Dimensions;

namespace Remotion.BuildScript;

public partial class RemotionBuild
{
  protected IEnumerable<string> SupportedTestBrowsers => GetTestDimensionValueList<Browsers>();

  protected IEnumerable<string> SupportedTestConfigurations => GetTestDimensionValueList<Configurations>();

  protected IEnumerable<string> SupportedTestExecutionRuntimes => GetTestDimensionValueList<ExecutionRuntimes>();

  protected IEnumerable<string> SupportedTestPlatforms => GetTestDimensionValueList<Platforms>();

  protected IEnumerable<string> SupportedTestTargetRuntimes => GetTestDimensionValueList<TargetRuntimes>();

  private IEnumerable<string> GetTestDimensionValueList<T> ()
  {
    var supportedTestDimensionsBuilder = new SupportedTestDimensionsBuilder();
    ConfigureSupportedTestDimensions(supportedTestDimensionsBuilder);

    var supportedTestDimensions = supportedTestDimensionsBuilder.Build();
    return supportedTestDimensions.ByName.Values.Select(e => e.ToString());
  }
}