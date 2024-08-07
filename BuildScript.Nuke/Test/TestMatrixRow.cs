﻿// Copyright (c) rubicon IT GmbH, www.rubicon.eu
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

namespace Remotion.BuildScript.Test;

public class TestMatrixRow
{
  public ImmutableArray<TestDimension> Elements { get; }

  public TestMatrixRow (ImmutableArray<TestDimension> elements)
  {
    Elements = elements;
  }

  public T GetDimension<T> ()
    where T : TestDimension
  {
    return GetDimensionOrDefault<T>() ?? throw new InvalidOperationException($"No element for dimension '{typeof(T).Name}' was found");
  }

  public T? GetDimensionOrDefault<T> ()
    where T : TestDimension
  {
    return (T?)Elements.SingleOrDefault(e => e.GetType().IsAssignableTo(typeof(T)));
  }

  public override string ToString ()
  {
    return string.Join(" + ", Elements);
  }
}