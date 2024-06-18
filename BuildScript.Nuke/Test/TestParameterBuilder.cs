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
using Nuke.Common;

namespace Remotion.BuildScript.Test;

public class TestParameterBuilder
{
  private readonly Dictionary<string, string?> _requiredParametersWithDefaultValues = new();

  public void AddRequiredParameter (TestDimension testDimension, string name)
  {
    ArgumentNullException.ThrowIfNull(testDimension);
    ArgumentNullException.ThrowIfNull(name);

    AddRequiredParameter($"{testDimension}_{name}");
  }

  public void AddRequiredParameter (string name)
  {
    ArgumentNullException.ThrowIfNull(name);

    _requiredParametersWithDefaultValues.TryAdd(name, null);
  }

  public void AddOptionalParameter (TestDimension testDimension, string name, string @default)
  {
    ArgumentNullException.ThrowIfNull(testDimension);
    ArgumentNullException.ThrowIfNull(name);
    ArgumentNullException.ThrowIfNull(@default);

    AddOptionalParameter($"{testDimension}_{name}", @default);
  }

  public void AddOptionalParameter (string name, string @default)
  {
    ArgumentNullException.ThrowIfNull(name);
    ArgumentNullException.ThrowIfNull(@default);

    if (_requiredParametersWithDefaultValues.TryGetValue(name, out var existingDefault))
    {
      Assert.True(
          @default == existingDefault,
          $"Duplicate optional parameter definitions must have the same default value. Test parameter '{name}'.");
    }
    else
    {
      _requiredParametersWithDefaultValues.Add(name, @default);
    }
  }

  public ImmutableDictionary<string, string> Build (ImmutableDictionary<string, string> parameters)
  {
    var finalParameters = parameters.ToBuilder();

    var missingTestParameters = new List<string>();
    foreach (var (key, @default) in _requiredParametersWithDefaultValues)
    {
      // Parameter already set
      if (finalParameters.ContainsKey(key))
        continue;

      // Try to resolve from environment variables
      var parameterFromEnvironment = Environment.GetEnvironmentVariable($"REMOTION_{key}");
      if (parameterFromEnvironment != null)
      {
        finalParameters[key] = parameterFromEnvironment;
        continue;
      }

      // Try to fall back to default
      if (@default != null)
      {
        finalParameters[key] = @default;
        continue;
      }

      // Otherwise, the test parameter is missing
      missingTestParameters.Add(key);
    }

    if (missingTestParameters.Count > 0)
    {
      Assert.Fail(
          $"Missing {missingTestParameters.Count} test parameters: [{string.Join(", ", missingTestParameters.Select(e => $"'{e}'"))}]. "
          + $"To set test parameters, set the following environment variables: [{string.Join(", ", missingTestParameters.Select(e => $"'REMOTION_{e}'"))}]");
    }

    return finalParameters.ToImmutable();
  }
}