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
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Serilog;

namespace Remotion.BuildScript.Components;

public interface IDependDB : IBaseBuild, IBuildMetadata, IPack
{
  [NuGetPackage(
      packageId: "DependDB.BuildProcessor.NuGetPreProcessor",
      packageExecutable: "DependDB.BuildProcessor.NuGetPreProcessor.exe")]
  Tool NugetPreProcessor => TryGetValue(() => NugetPreProcessor);

  [NuGetPackage(
      packageId: "DependDB.BuildProcessor",
      packageExecutable: "DependDB.BuildProcessor.exe")]
  Tool BuildProcessor => TryGetValue(() => BuildProcessor)!;

  [Parameter]
  string DependDBProjectName => TryGetValue(() => DependDBProjectName)!;

  [Parameter]
  string DependDBProjectBranch => TryGetValue(() => DependDBProjectBranch)!;

  [Parameter]
  string? DependDBProjectVersion => TryGetValue(() => DependDBProjectVersion);

  [Parameter]
  string DependDBRetentionTime => TryGetValue(() => DependDBRetentionTime)!;

  [Parameter]
  string DependDBProjectImportNotificationMailAddress => TryGetValue(() => DependDBProjectImportNotificationMailAddress)!;

  [Parameter]
  string DependDBProjectImportNotificationMailAddressCc => TryGetValue(() => DependDBProjectImportNotificationMailAddressCc)!;

  [Parameter]
  string VcsUrlTemplate => TryGetValue(() => VcsUrlTemplate)!;

  [Parameter]
  string DependDBTargetFramework => TryGetValue(() => DependDBTargetFramework)!;

  [Parameter]
  string? DependDBNugetConfig => TryGetValue(() => DependDBNugetConfig)!;

  [Parameter]
  string DependDBTrackedReferences => TryGetValue(() => DependDBTrackedReferences)!;

  [Parameter]
  string? DependDBNuGetSources => TryGetValue(() => DependDBNuGetSources);

  [PublicAPI]
  public Target GenerateDependDBOutput => _ => _
          .DependsOn<IBuildMetadata>()
          .DependsOn<IPack>()
          .Requires(() => DependDBProjectName)
          .Requires(() => DependDBProjectBranch)
          .Requires(() => DependDBRetentionTime)
          .Requires(() => DependDBProjectImportNotificationMailAddress != null)
          .Requires(() => VcsUrlTemplate)
          .Requires(() => DependDBTargetFramework)
          .Requires(() => DependDBTrackedReferences)
          .Executes(() =>
          {
              if (NugetPreProcessor == null)
                  throw new InvalidOperationException("Cannot find the DependDB Nuget pre processor. Please install the NuGet package 'DependDB.BuildProcessor.NuGetPreProcessor'.");
              if (BuildProcessor == null)
                  throw new InvalidOperationException("Cannot find the DependDB build processor. Please install the NuGet package 'DependDB.BuildProcessor'.");

              var outputFolder = OutputFolder / "DependDB";
              var buildProcessorFile = TempFolder / "DependDB.BuildProcessor.config.xml";

              var projectVersion = DependDBProjectVersion ?? BuildMetadataPerConfiguration["Debug"].AssemblyVersion;

              var trackedReferences = DependDBTrackedReferences.Split(';');
              var nugetSources = DependDBNuGetSources != null
                      ? DependDBNuGetSources.Split(';')
                      : [];

              if (!int.TryParse(DependDBRetentionTime, out var retentionTime))
                  throw new FormatException($"The specified parameter '{nameof(DependDBRetentionTime)}' is not an integer.");

              var ownPackagesDirectory = DetermineOwnPackagesDirectory();
              if (!ownPackagesDirectory.GetFiles("*.nupkg").Any())
                  throw new InvalidOperationException("Own packages directory does not contain any NuGet packages.");

              Log.Information("DependDB configuration:");
              Log.Information($"  OwnPackagesDirectory: '{ownPackagesDirectory}'");
              Log.Information($"  {nameof(DependDBProjectName)}: '{DependDBProjectName}'");
              Log.Information($"  {nameof(DependDBProjectBranch)}: '{DependDBProjectBranch}'");
              Log.Information($"  {nameof(DependDBProjectVersion)}: '{projectVersion}'");
              Log.Information($"  {nameof(DependDBRetentionTime)}: '{retentionTime}'");
              Log.Information($"  {nameof(DependDBProjectImportNotificationMailAddress)}: '{DependDBProjectImportNotificationMailAddress}'");
              Log.Information($"  {nameof(DependDBProjectImportNotificationMailAddressCc)}: '{DependDBProjectImportNotificationMailAddressCc}'");
              Log.Information($"  {nameof(VcsUrlTemplate)}: '{VcsUrlTemplate}'");
              Log.Information($"  {nameof(DependDBTargetFramework)}: '{DependDBTargetFramework}'");
              Log.Information($"  {nameof(DependDBTrackedReferences)}: [{string.Join(", ", trackedReferences.Select(e => $"'{e}'"))}]");
              Log.Information($"  {nameof(DependDBNuGetSources)}: [{string.Join(", ", nugetSources.Select(e => $"'{e}'"))}]");

              var arguments = new Arguments()
                      .Add("--project-name={value}", DependDBProjectName)
                      .Add("--project-branch={value}", DependDBProjectBranch)
                      .Add("--project-version={value}", projectVersion)
                      .Add("--retention-time={value}", retentionTime)
                      .Add("--notification-address-to={value}", DependDBProjectImportNotificationMailAddress)
                      .Add("--temp-directory={value}", TempFolder)
                      .Add("--processor-output-directory={value}", outputFolder)
                      .Add("--preprocessor-output-file={value}", buildProcessorFile)
                      .Add("--source-directory-root={value}", Solution.Directory)
                      .Add("--source-control-template={value}", VcsUrlTemplate)
                      .Add("--own-packages-directory={value}", ownPackagesDirectory)
                      .Add("--target-framework={value}", DependDBTargetFramework);

              if (DependDBProjectImportNotificationMailAddressCc != null)
                  arguments = arguments.Add("--notification-address-cc={value}", DependDBProjectImportNotificationMailAddressCc);

              if (DependDBNugetConfig != null)
                  arguments = arguments.Add("--nuget-config={value}", MakeAbsolutePath(DependDBNugetConfig));

              foreach (var trackedReference in trackedReferences)
                  arguments = arguments.Add("--analzyer-tracked-reference={value}", trackedReference);

              foreach (var nugetSource in nugetSources)
                  arguments = arguments.Add("--additional-nuget-source={value}", nugetSource);

              // Because of NUKE's special handling for the arguments using IFormattable, we can't pass
              // `arguments.RenderForOutput()` to the tool because the string would be escaped as a single argument.
              // As such, we need to manually create the interpolated string handler to use the literal string
              var argumentStringHandler = new ArgumentStringHandler(1, 0, out var _);
              argumentStringHandler.AppendLiteral(arguments.RenderForOutput());

              NugetPreProcessor(
                      argumentStringHandler,
                      workingDirectory: Solution.Directory);

              BuildProcessor($"{buildProcessorFile}");


              string MakeAbsolutePath (string value)
              {
                  if (value.StartsWith("~/"))
                  {
                      return Solution.Directory / value[2..];
                  }

                  return value;
              }
          });

  AbsolutePath DetermineOwnPackagesDirectory ()
  {
      var packProfile = Profiles.Single();
      return OutputFolder / packProfile.OutputFolderName / "Debug";
  }
}
