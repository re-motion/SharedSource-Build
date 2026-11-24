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

namespace Remotion.BuildScript;

public class Arguments
{
  private ImmutableArray<string> _args = ImmutableArray<string>.Empty;

  public Arguments ()
  {
  }

  private Arguments (ImmutableArray<string> args)
  {
    _args = args;
  }

  public Arguments AddLiteral (params string[] values)
  {
    return new Arguments(_args.AddRange(values));
  }

  public Arguments AddValue (params string[] values)
  {
    return new Arguments(_args.AddRange(values));
  }

  public Arguments Add (string key, string value)
  {
    var parts = key.Split(' ', StringSplitOptions.RemoveEmptyEntries);

    return AddLiteral([.. parts.Select(e => e.Replace("{value}", value))]);
  }

  public Arguments InsertLiteral (int index, params string[] values)
  {
    return new Arguments(_args.InsertRange(index, values));
  }

  public string[] RenderAsArray ()
  {
    return [.. _args];
  }

  public string RenderForExecution ()
  {
    return string.Join(" ", _args.Select(Escape));
  }

  public override string ToString () => RenderForExecution();

  private static string Escape (string value)
  {
    var valueWithReplacedQuotes = value.Replace("\"", "\\\"");

    return value.Contains(' ')
        ? $"\"{valueWithReplacedQuotes}\""
        : valueWithReplacedQuotes;
  }
}