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
using Remotion.BuildScript.Util;

namespace Remotion.BuildScript.Test;

public class TestCase : ITestItem
{
  public string Name { get; }

  public ProjectMetadata Project { get; }

  public TestMatrixRow Row { get; }

  public ITestExecutionRuntimeFactory TestExecutionRuntimeFactory { get; }

  public ImmutableArray<ITestExecutionWrapper> TestExecutionWrappers { get; }

  public TestCase (
      string name,
      ProjectMetadata project,
      TestMatrixRow row,
      ITestExecutionRuntimeFactory testExecutionRuntimeFactory,
      ImmutableArray<ITestExecutionWrapper> testExecutionWrappers)
  {
    ArgumentNullException.ThrowIfNull(name);
    ArgumentNullException.ThrowIfNull(project);
    ArgumentNullException.ThrowIfNull(row);
    ArgumentNullException.ThrowIfNull(testExecutionRuntimeFactory);

    Name = name;
    Project = project;
    Row = row;
    TestExecutionRuntimeFactory = testExecutionRuntimeFactory;
    TestExecutionWrappers = testExecutionWrappers;
  }

  public void Execute (ITestContext context)
  {
    using var __ = GroupingBlock.Start($"TestCase '{Name}'");
    context.ExecuteTestCase(this);
  }
}