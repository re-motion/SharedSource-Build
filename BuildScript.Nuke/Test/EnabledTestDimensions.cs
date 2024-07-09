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
using System.Collections.Immutable;
using System.Linq;

namespace Remotion.BuildScript.Test;

public class EnabledTestDimensions
{
  public static readonly EnabledTestDimensions Empty = new(ImmutableHashSet<TestDimension>.Empty);

  private readonly ImmutableHashSet<TestDimension> _enabledTestDimensions;

  public EnabledTestDimensions (ImmutableHashSet<TestDimension> enabledTestDimensions)
  {
    ArgumentNullException.ThrowIfNull(enabledTestDimensions);

    _enabledTestDimensions = enabledTestDimensions;
  }

  public bool Contains (TestDimension testDimension) => _enabledTestDimensions.Contains(testDimension);

  public IEnumerable<T> OfType<T> ()
    where T : TestDimension
  {
    return _enabledTestDimensions.OfType<T>();
  }
}