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
using Serilog;

namespace Remotion.BuildScript.Test;

public class TestMatricesBuilder
{
  private readonly SupportedTestDimensions _supportedTestDimensions;
  private readonly EnabledTestDimensions _enabledTestDimensions;

  private readonly Dictionary<string, TestMatrix> _testMatrices = new();

  public TestMatricesBuilder (SupportedTestDimensions supportedTestDimensions, EnabledTestDimensions enabledTestDimensions)
  {
    _supportedTestDimensions = supportedTestDimensions;
    _enabledTestDimensions = enabledTestDimensions;
  }

  public TestMatrix AddTestMatrix (string name, TestDimension[,] matrix, bool allowEmpty = false)
  {
    ArgumentNullException.ThrowIfNull(name);
    ArgumentNullException.ThrowIfNull(matrix);
    if (matrix.GetLength(0) == 0 || matrix.GetLength(1) == 0)
      throw new ArgumentException("Test configuration matrix must not be empty.", nameof(matrix));

    if (_testMatrices.ContainsKey(name))
      throw new InvalidOperationException($"A test matrix with the name '{name}' has already been added.");

    var rowBuilder = ImmutableArray.CreateBuilder<TestMatrixRow>();
    for (var x = 0; x < matrix.GetLength(0); x++)
    {
      var addedTestDimensionNames = new HashSet<string>();
      var testDimensionBuilder = ImmutableArray.CreateBuilder<TestDimension>();

      for (var y = 0; y < matrix.GetLength(1); y++)
      {
        var value = matrix[x, y];
        if (!addedTestDimensionNames.Add(value.Name))
          Assert.Fail($"Test matrix '{name}' contains a duplicate test dimension '{value.Name}' in row {x}.");

        if (!_supportedTestDimensions.IsSupported(value))
          Assert.Fail($"The value '{value}' is not a supported value.");

        testDimensionBuilder.Add(value);
      }

      // Check if all the values are supported dimensions values
      if (!addedTestDimensionNames.SetEquals(_supportedTestDimensions.Names))
      {
        var missing = _supportedTestDimensions.Names.Except(addedTestDimensionNames);
        var added = addedTestDimensionNames.Except(_supportedTestDimensions.Names);

        throw new InvalidOperationException(
            $"Test matrix '{name}' contains invalid dimension values in row {x}. "
            + $"Missing: [{string.Join(", ", missing)}]. "
            + $"Additional: [{string.Join(", ", added)}]");
      }

      // Check if all the values are actually enabled
      if (testDimensionBuilder.All(_enabledTestDimensions.Contains))
      {
        rowBuilder.Add(new TestMatrixRow(testDimensionBuilder.ToImmutable()));
      }
      else
      {
        var disabledTestDimensions = string.Join(", ", testDimensionBuilder.Where(e => !_enabledTestDimensions.Contains(e)));
        Log.Verbose($"Test configuration '{string.Join(", ", testDimensionBuilder)}' was ignored due to disabled test dimensions '{disabledTestDimensions}'.");
      }
    }

    var testMatrix = new TestMatrix(name, rowBuilder.ToImmutable());
    if (testMatrix.IsEmpty && !allowEmpty)
    {
      Log.Warning($"The test matrix '{name}' is empty.");

      Assert.Fail("Test matrix cannot be empty.");
    }

    _testMatrices.Add(name, testMatrix);

    return testMatrix;
  }

  public ImmutableArray<TestMatrix> Build ()
  {
    return _testMatrices.Values.ToImmutableArray();
  }
}