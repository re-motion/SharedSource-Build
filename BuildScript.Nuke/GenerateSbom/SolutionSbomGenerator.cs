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
using System.IO;
using CycloneDX.Models;
using CycloneDX.Utils;
using CycloneDX.Xml;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.Npm;
using Nuke.Common.Tools.PowerShell;
using Octokit;
using SbomCleaner.Library;
using Serilog;
using Tool = Nuke.Common.Tooling.Tool;

namespace Remotion.BuildScript.GenerateSbom;

public class SolutionSbomGenerator: ISbomGenerator
{
  public SbomGeneration SbomGeneration => SbomGeneration.ForSolution;

  private readonly FilterList _packageBlackList;
  private readonly FilterList _projectBlacklist;

  private readonly string _githubUsername;
  private readonly string _githubAccessToken;

  private readonly AbsolutePath _outputFile;

  private readonly Solution _solution;
  private readonly string _version;
  private readonly AbsolutePath _workingDirectory;

  private readonly AbsolutePath? _pathToPackageJson;

  public SolutionSbomGenerator (
      Solution solution,
      string version,
      AbsolutePath workingDirectory,
      AbsolutePath outputFile,
      FilterList packageBlackList,
      FilterList projectBlacklist,
      AbsolutePath? pathToPackageJson,
      string githubUsername,
      string githubAccessToken)
  {
    _solution = solution;
    _version = version;
    _workingDirectory = workingDirectory;
    _outputFile = outputFile;
    _projectBlacklist = projectBlacklist;
    _packageBlackList = packageBlackList;
    _pathToPackageJson = pathToPackageJson;
    _githubUsername = githubUsername;
    _githubAccessToken = githubAccessToken;
  }

  public void Generate (Tool cycloneDxTool, IReadOnlyCollection<ProjectInfo> projects)
  {
    var uncleanedSbomFilename = "uncleaned-sbom.xml";

    Directory.CreateDirectory(_workingDirectory);

    cycloneDxTool.Invoke(
        arguments:
        $"-o . "
        + $"--disable-package-restore "
        + $"--exclude-dev "
        + $"--exclude-test-projects "
        + $"-gu {_githubUsername} "
        + $"-gt {_githubAccessToken} "
        + $"-f \"{uncleanedSbomFilename}\" "
        + $"-sv \"{_version}\" "
        + $"{_solution.Path}",
        workingDirectory: _workingDirectory
    );

    Log.Information("Cleaning sbom...");

    var cleanedSbomFileName = "cleaned-sbom.xml";
    var cleaner = new Cleaner();

    cleaner.CleanSolutionSbom(
        _workingDirectory / uncleanedSbomFilename,
        projects,
        _packageBlackList,
        _projectBlacklist,
        _workingDirectory / cleanedSbomFileName);

    Log.Information("Created cleaned sbom '{_outputFile}'.", _workingDirectory / cleanedSbomFileName);

    if (_pathToPackageJson != null)
    {
      var npmSbomFileName = CreateNPMSbom();

      MergeSboms(cleanedSbomFileName, npmSbomFileName, _outputFile);
    }
    else
    {
      Log.Information("No package json file specified, will therefore not create combined sbom.");

      Directory.CreateDirectory(_outputFile.Parent);

      File.Copy(_workingDirectory / cleanedSbomFileName, _outputFile, overwrite: true);

      Log.Information($"Cleaned sbom has been copied to '{_outputFile}'.");
    }
  }

  private void MergeSboms (string cleanedSbomFileName, string npmSbomFileName, AbsolutePath outputFile)
  {
    using var mainSbomFileStream = File.OpenRead(_workingDirectory / cleanedSbomFileName);
    using var npmSbomFileStream = File.OpenRead(_workingDirectory / npmSbomFileName);

    var mainSbom = Serializer.Deserialize(mainSbomFileStream);
    var npmSbom = Serializer.Deserialize(npmSbomFileStream);

    var combinedSbom = CycloneDXUtils.HierarchicalMerge([mainSbom, npmSbom], mainSbom.Metadata.Component);

    using var outputFileStream = File.Create(outputFile);

    Serializer.Serialize(combinedSbom, outputFileStream);
    outputFileStream.Close();

    Log.Information("Merged sbom created at {0}", outputFile);
  }

  private string CreateNPMSbom ()
  {
    Log.Information("Downloading npm sbom tool...");

    var packageJsonFolder = _pathToPackageJson;
    if (File.Exists(_pathToPackageJson))
      packageJsonFolder  = Path.GetDirectoryName(_pathToPackageJson);

    NpmTasks.Npm("install", packageJsonFolder);

    NpmTasks.NpmInstall(_ => _
        .SetPackages("@cyclonedx/cyclonedx-npm")
        .SetProcessWorkingDirectory(_workingDirectory));

    var npmSbomFileName = "npm-sbom.xml";

    Log.Information("Creating npm sbom...");

    PowerShellTasks.PowerShell(_ => _
        .SetCommand($".\\node_modules\\.bin\\cyclonedx-npm.ps1 {packageJsonFolder /"package.json"} --omit dev --output-format xml --output-file '{npmSbomFileName}'")
        .SetProcessWorkingDirectory(_workingDirectory));

    Log.Information("Created npm sbom at '{npmSbomLocation}'.", _workingDirectory / npmSbomFileName);
    return npmSbomFileName;
  }
}