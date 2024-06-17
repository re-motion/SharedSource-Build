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
using JetBrains.Annotations;

namespace Remotion.BuildScript.TestMatrix.Dimensions;

// ReSharper disable InconsistentNaming

[PublicAPI]
public sealed class ExecutionRuntimes : TestDimension
{
  public static ExecutionRuntimes LocalMachine = new(nameof(LocalMachine));
  public static ExecutionRuntimes EnforcedLocalMachine = new(nameof(EnforcedLocalMachine));

  public static readonly ExecutionRuntimes Docker_Win_NET462 = new(nameof(Docker_Win_NET462));
  public static readonly ExecutionRuntimes Docker_Win_NET472 = new(nameof(Docker_Win_NET472));
  public static readonly ExecutionRuntimes Docker_Win_NET48 = new(nameof(Docker_Win_NET48));
  public static readonly ExecutionRuntimes Docker_Win_NET481 = new(nameof(Docker_Win_NET481));

  public ExecutionRuntimes (string value)
      : base(value)
  {
  }
}