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
//

using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using ReleaseProcessScript.New.Steps;
using Serilog;
using Spectre.Console;
using Spectre.Console.Cli;
using NotNull = System.Diagnostics.CodeAnalysis.NotNullAttribute;

namespace ReleaseProcessScript.New.Commands;

[UsedImplicitly]
[SuppressMessage("ReSharper", "RedundantNullableFlowAttribute")]
public class ReleaseVersionCommand : Command<ReleaseVersionSettings>
{
  private readonly IStartReleaseStep _startReleaseStep;
  private readonly IAnsiConsole _console;
  private readonly ILogger _log = Log.ForContext<ReleaseVersionCommand>();

  public ReleaseVersionCommand (IStartReleaseStep startReleaseStep, IAnsiConsole console)
  {
    _startReleaseStep = startReleaseStep;
    _console = console;
  }

  public override int Execute ([NotNull] CommandContext context, [NotNull] ReleaseVersionSettings settings)
  {
    const string message = "Starting a new release";
    _log.Information(message);
    _console.WriteLine(message);
    
    _startReleaseStep.Execute(settings.CommitHash, settings.PauseForCommit, settings.DoNotPush);

    return 0;
  }
}
