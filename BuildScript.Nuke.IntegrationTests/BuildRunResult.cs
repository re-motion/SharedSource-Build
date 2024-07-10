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
using System.Linq;
using Nuke.Common.Utilities.Collections;

namespace BuildScript.Nuke.IntegrationTests;

public record BuildRunResult (int ExitCode, string Output)
{
  public string? GetSectionOutput (string name)
  {
    var lines = Output.Split(Environment.NewLine);
    var sectionLines = lines
        .SkipUntil(e => e.StartsWith($"║ {name}"))
        .Skip(3)
        .TakeUntil(e => e.StartsWith('═') || e.StartsWith('╬'))
        .ToArray();

    return sectionLines.Length == 0
        ? null
        : string.Join(Environment.NewLine, sectionLines.Take(sectionLines.Length - 1));
  }
}
