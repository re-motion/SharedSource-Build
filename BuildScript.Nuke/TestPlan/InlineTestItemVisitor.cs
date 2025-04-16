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
using System.Collections.Generic;

namespace Remotion.BuildScript.TestPlan;

public class InlineTestItemVisitor : ITestItemVisitor
{
  public static IEnumerable<ITestItem> CollectAll (IEnumerable<ITestItem> testItems)
  {
    var results = new List<ITestItem>();
    Visit(testItems, e => results.Add(e), e => results.Add(e));

    return results;
  }

  public static void Visit (
      IEnumerable<ITestItem> testItems,
      Action<ITestGroup> testGroupAction,
      Action<ITestCase> testCaseAction)
  {
    var visitor = new InlineTestItemVisitor(testGroupAction, testCaseAction);
    foreach (var testItem in testItems)
      testItem.Accept(visitor);
  }

  private readonly Action<ITestGroup> _testGroupAction;
  private readonly Action<ITestCase> _testCaseAction;

  public InlineTestItemVisitor (Action<ITestGroup> testGroupAction, Action<ITestCase> testCaseAction)
  {
    _testGroupAction = testGroupAction;
    _testCaseAction = testCaseAction;
  }

  public void VisitGroup (ITestGroup testGroup)
  {
    _testGroupAction(testGroup);
    foreach (var testItem in testGroup.Items)
      testItem.Accept(this);
  }

  public void VisitCase (ITestCase testCase)
  {
    _testCaseAction(testCase);
  }
}