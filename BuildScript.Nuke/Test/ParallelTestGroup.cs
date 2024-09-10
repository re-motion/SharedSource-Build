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
using System.Threading.Tasks;
using Remotion.BuildScript.Util;

namespace Remotion.BuildScript.Test;

public class ParallelTestGroup (string name, ImmutableArray<ITestItem> testItems) : TestGroup(name, testItems)
{
  /// <inheritdoc />
  public override void Execute (ITestContext context)
  {
    // During parallel execution logging output will be mixed as all threads write
    // to output/log at the same time. To fix this, we buffer all console/log calls
    // and flush these calls once the parallel execution of an item finishes.
    // This makes everything appear as if the work was done sequentially.
    using var parallelLogger = ParallelLoggingContext.CreateAndAssign();

    // ReSharper disable AccessToDisposedClosure
    Parallel.ForEach(testItems, item =>
    {
      parallelLogger.StartParallelWork();
      try
      {
        item.Execute(context);
      }
      finally
      {
        parallelLogger.StopParallelWork();
      }
    });
    // ReSharper enable AccessToDisposedClosure
  }
}