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
using System.Linq;
using System.Linq.Expressions;
using Remotion.BuildScript.Test;

namespace Remotion.BuildScript;

public static class TestResourceExtensions
{
  public static T GetRequiredTestResource<T> (this IEnumerable<ITestResource> enumerable, Expression<Func<T, bool>>? filter = null)
  {
    var predicate = filter?.Compile() ?? (static _ => true);
    IReadOnlyList<T> items = enumerable
        .OfType<T>()
        .Where(predicate)
        .ToList();

    if (items.Count != 1)
    {
      var testResourceString = filter != null
          ? $"'{typeof(T)}' with the specified filter '{filter}'"
          : $"'{typeof(T)}'";

      if (items.Count == 0)
        throw new InvalidOperationException($"Cannot find the required test resource {testResourceString}.");

      throw new InvalidOperationException($"Found multiple test resources for the test resource {testResourceString}");
    }

    return items[0];
  }
}