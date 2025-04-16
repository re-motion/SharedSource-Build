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

using System.Collections.Immutable;
using Remotion.BuildScript.Components;
using Remotion.BuildScript.TestPlan;

namespace Remotion.BuildScript.Test;

public class DefaultTestContext : ITestContext
{
  public ITest Build { get; }

  public ImmutableDictionary<string, string> TestParameters { get; }

  public ImmutableArray<ITestResource> TestResources { get; }

  public bool FatalFailure { get; set; }

  public int PassedTestCount { get; set; }

  public int FailedTestCount { get; set; }

  public int TotalTestCount { get; set; }

  public DefaultTestContext (
      ITest build,
      ImmutableDictionary<string, string> testParameters,
      ImmutableArray<ITestResource> testResources)
  {
    Build = build;
    TestParameters = testParameters;
    TestResources = testResources;
  }

  public void ExecuteTestItem (ITestItem testItem)
  {
    testItem.Execute(this);
  }
}