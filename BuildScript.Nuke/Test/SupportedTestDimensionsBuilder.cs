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

namespace Remotion.BuildScript.Test;

public class SupportedTestDimensionsBuilder
{
  private readonly ImmutableHashSet<Type>.Builder _supportedTestDimensionTypes = ImmutableHashSet.CreateBuilder<Type>();
  private readonly ImmutableDictionary<string, TestDimension>.Builder _supportedTestDimensionsByName = ImmutableDictionary.CreateBuilder<string, TestDimension>();

  public void AddSupportedDimension<T>(params T[] supportedValues)
    where T : TestDimension
  {
    ArgumentNullException.ThrowIfNull(supportedValues);
    if (supportedValues.Length == 0)
      throw new ArgumentException("Array must not be empty.", nameof(supportedValues));

    foreach (var supportedValue in supportedValues)
    {
      var dimensionValueName = supportedValue.Value;
      if (_supportedTestDimensionsByName.TryGetValue(dimensionValueName, out var existingValue))
      {
        if (supportedValue != existingValue)
          throw new InvalidOperationException($"Duplicate dimension value '{dimensionValueName}'.");

        continue;
      }

      _supportedTestDimensionTypes.Add(supportedValue.GetType());
      _supportedTestDimensionsByName.Add(dimensionValueName, supportedValue);
    }
  }

  public SupportedTestDimensions Build ()
  {
    return new SupportedTestDimensions(
        _supportedTestDimensionTypes.ToImmutable(),
        _supportedTestDimensionsByName.ToImmutable());
  }
}