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
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Remotion.BuildScript;

public class TargetFrameworkSet : IEnumerable<string>
{
  public static TargetFrameworkSet Parse (string value)
  {
    var parts = value.Split(';');

    var builder = ImmutableArray.CreateBuilder<string>();
    foreach (var part in parts)
    {
      if (string.IsNullOrWhiteSpace(part))
        throw new InvalidOperationException($"The specified target framework list '{value}' is invalid.");

      builder.Add(part.Trim());
    }

    return new TargetFrameworkSet(builder.ToImmutable());
  }

  private readonly ImmutableArray<string> _targetFrameworks;

  public TargetFrameworkSet (ImmutableArray<string> targetFrameworks)
  {
    _targetFrameworks = targetFrameworks;
  }

  public bool Contains (string value)
  {
    return _targetFrameworks.Any(e => e == value);
  }

  public IEnumerator<string> GetEnumerator () => ((IEnumerable<string>)_targetFrameworks).GetEnumerator();

  IEnumerator IEnumerable.GetEnumerator () => GetEnumerator();

  public override string ToString () => string.Join(";", _targetFrameworks);
}