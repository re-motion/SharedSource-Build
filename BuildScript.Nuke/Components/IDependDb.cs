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
// 

using System;
using System.Text;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Utilities;
using Nuke.Common.Utilities.Collections;
using Remotion.BuildScript.Components.Tasks;

namespace Remotion.BuildScript.Components;

public interface IDependDb : IBaseBuild
{
  [Parameter("Override for the notification mail address in the dependDb project import")]
  public string DependDbProjectImportNotificationMailAddress =>
      TryGetValue(() => DependDbProjectImportNotificationMailAddress) ?? "";

  [Parameter("Override for the notification cc mail address in the dependDb project import")]
  public string DependDbProjectImportNotificationMailAddressCc =>
      TryGetValue(() => DependDbProjectImportNotificationMailAddressCc) ?? "";

  [Parameter("Template for the version control system url")]
  public string VcsUrlTemplate => TryGetValue(() => VcsUrlTemplate) ?? "";

  Target DependDb => _ => _
      .DependsOn<INuget>(nuget => nuget.GenerateNuGetPackagesWithDebugSymbols)
      .Executes(() =>
      {
        var dataAssemblyMetadata = ConfigurationData.AssemblyMetadata;

        var projectImportNotificationMailAddress = DependDbProjectImportNotificationMailAddress.IsNullOrEmpty()
            ? dataAssemblyMetadata.DependDBProjectImportNotificationMailAddress
            : DependDbProjectImportNotificationMailAddress;
        var vcsUrlTemplate = VcsUrlTemplate;
        if (IsLocalBuild)
        {
          projectImportNotificationMailAddress = "noreply@localhost.net";
          vcsUrlTemplate = "http://localhost/{0}";
        }

        if (projectImportNotificationMailAddress.IsNullOrEmpty() ||
            vcsUrlTemplate.IsNullOrEmpty())
            Assert.Fail("Not all required dependDb parameters and properties are set");

        FileSystemTasks.EnsureExistingDirectory(Directories.Temp);
        var dependDbBuildProcessFile = Directories.Temp / "DependDb.BuildProcessor.config.xml";

        var msBuildToolPath = BaseTask.GetMsBuildToolPath(
            MSBuildTasks.MSBuildPath,
            MsBuildPath,
            VisualStudioVersion);

        foreach (var config in Configuration)
        {
          var nuGetWithDebugSymbolsOutputDirectory = Directories.NuGetWithDebugSymbols / config;
          var preProcessParameter = new StringBuilder()
              .Append(" --msbuild-path=").Append($"\"{msBuildToolPath}\"")
              .Append(" --project-name=").Append(dataAssemblyMetadata.DependDBProjectName)
              .Append(" --project-branch=").Append(dataAssemblyMetadata.DependDBProjectBranch)
              .Append(" --project-version=").Append(ConfigurationData.SemanticVersion.DependDBProjectVersion)
              .Append(" --retention-time=").Append(dataAssemblyMetadata.DependDBRetentionTime)
              .Append(" --temp-directory=").Append(Directories.Temp.ToString().TrimEnd('/'))
              .Append(" --notification-address-to=").Append(projectImportNotificationMailAddress)
              .Append(" --processor-output-directory=").Append(Directories.DependDBOutputDirectory.ToString().TrimEnd('/'))
              .Append(" --preprocessor-output-file=").Append(dependDbBuildProcessFile)
              .Append(" --source-directory-root=").Append(Directories.Solution.ToString().TrimEnd('/'))
              .Append(" --source-control-template=").Append(vcsUrlTemplate)
              .Append(" --target-framework=").Append(dataAssemblyMetadata.DependDBTargetFramework)
              .Append(" --own-packages-directory=").Append($"\"{nuGetWithDebugSymbolsOutputDirectory}\"");
          dataAssemblyMetadata.DependDBTrackedReferences.ForEach(trackedReference =>
              preProcessParameter
                  .Append(" --analzyer-tracked-reference=")
                  .Append(trackedReference));
          dataAssemblyMetadata.DependDBNuGetSources.ForEach(nugetSources =>
              preProcessParameter
                  .Append(" --additional-nuget-source=")
                  .Append(nugetSources));
          if (!DependDbProjectImportNotificationMailAddressCc.IsNullOrEmpty())
          {
            preProcessParameter
                .Append(" --notification-address-cc=")
                .Append(DependDbProjectImportNotificationMailAddressCc);
          }

          var nugetPreProcessorPackage = NuGetPackageResolver
              .GetLocalInstalledPackage("DependDB.BuildProcessor.NuGetPreProcessor", ToolPathResolver.NuGetAssetsConfigFile)
              .NotNull("nugetPreProcessorPackage != null");
          var buildProcessorPackage = NuGetPackageResolver
              .GetLocalInstalledPackage("DependDB.BuildProcessor", ToolPathResolver.NuGetAssetsConfigFile)
              .NotNull("buildProcessorPackage != null");
          var nuGetPreProcessorResult = ProcessTasks.StartProcess(
              $"{nugetPreProcessorPackage.Directory}/tools/DependDB.BuildProcessor.NuGetPreProcessor.exe",
              preProcessParameter.ToString());
          nuGetPreProcessorResult.WaitForExit();
          if (nuGetPreProcessorResult.ExitCode != 0)
          {
            Assert.Fail(
                $"DependDB.BuildProcessor.NuGetPreProcessor.exe exited with error code {nuGetPreProcessorResult.ExitCode}");
          }

          var buildProcessorResult = ProcessTasks.StartProcess(
              $"{buildProcessorPackage.Directory}/tools/DependDB.BuildProcessor.exe",
              dependDbBuildProcessFile);
          buildProcessorResult.WaitForExit();
          if (buildProcessorResult.ExitCode != 0)
            Assert.Fail($"DependDB.BuildProcessor.exe exited with error code {buildProcessorResult.ExitCode}");
        }
      });
}
