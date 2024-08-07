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
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Remotion.BuildScript.Test;

public class SupportedTestDimensions
{
  public static readonly SupportedTestDimensions Empty = new(ImmutableDictionary<string, TestDimension>.Empty);

  private static IEnumerable<Type> GetRelevantTestDimensionTypes (TestDimension testDimension)
  {
    ArgumentNullException.ThrowIfNull(testDimension);

    var current = testDimension.GetType();
    while (current != typeof(TestDimension))
    {
      yield return current;
      current = current.BaseType!;

      Debug.Assert(current != null, "current != null");
    }
  }

  public ImmutableDictionary<string, TestDimension> Values { get; }

  public ImmutableHashSet<Type> Types { get; }

  public ImmutableHashSet<string> Names { get; }

  public SupportedTestDimensions (ImmutableDictionary<string, TestDimension> values)
  {
    Values = values;

    Types = Values.Values
        .SelectMany(GetRelevantTestDimensionTypes)
        .ToImmutableHashSet();

    Names = Values.Values
        .Select(e => e.Name)
        .ToImmutableHashSet();
  }

  public bool IsSupported (TestDimension value)
  {
    return Values.TryGetValue(value.Value, out var existingValue) && value == existingValue;
  }

  public bool IsSupported<T> ()
      where T : TestDimension
  {
    return Types.Contains(typeof(T));
  }

  public IEnumerable<T> OfType<T> ()
      where T : TestDimension
  {
    return Values.Values.OfType<T>();
  }

  public T[]? ParseTestDimensionValuesOrDefault<T> (string[] values)
      where T : TestDimension
  {
    if (values.Length == 0)
      return null;

    var testDimensions = new T[values.Length];
    for (var i = 0; i < values.Length; i++)
    {
      var dimensionString = values[i].Trim();
      if (!Values.TryGetValue(dimensionString, out var testDimension))
        throw new InvalidOperationException($"The supplied test dimension '{dimensionString}' is not supported.");
      if (testDimension is not T concreteTestDimension)
        throw new InvalidOperationException($"The supplied test dimension '{dimensionString}' is not of the dimension type '{typeof(T).Name}'.");

      testDimensions[i] = concreteTestDimension;
    }

    return testDimensions;
  }
}