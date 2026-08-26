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
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.Npm;
using Nuke.Common.Tools.PowerShell;
using SbomCleaner.Library;
using Serilog;
using XMLSerializer = CycloneDX.Xml.Serializer;
using JsonSerializer = CycloneDX.Json.Serializer;
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

  public ISbomCleaner? SbomCleaner { get; init; } = new SbomCleanerAdapter(new Cleaner());

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

    var processedFile = _workingDirectory / uncleanedSbomFilename;
    Assert.FileExists(processedFile, "CycloneDX did not produce an output.");

    if (SbomCleaner != null)
    {
      Log.Information("Cleaning sbom...");

      var cleanedSbomFile = _workingDirectory / "cleaned-sbom.xml";

      SbomCleaner.CleanSolution(
          processedFile,
          projects,
          _packageBlackList,
          _projectBlacklist,
          cleanedSbomFile);

      Log.Information("Created cleaned sbom '{_outputFile}'.", cleanedSbomFile);

      processedFile = cleanedSbomFile;
      Assert.FileExists(processedFile, "SBOM cleaning did not produce an output");
    }


    if (_pathToPackageJson != null)
    {
      var npmSbomFileName = CreateNPMSbom();

      MergeSboms(processedFile, npmSbomFileName, _outputFile);
    }
    else
    {
      Log.Information("No package json file specified, will therefore not create combined sbom.");

      _outputFile.Parent.CreateDirectory();

      //The sbom cleaner only works with xml files, so we need to convert the sbom if we want a different format.
      //Given a requested xml file, we can just copy
      if (_outputFile.Extension == ".xml")
      {
        File.Copy(processedFile, _outputFile, overwrite: true);
      }
      else if (_outputFile.Extension == ".json")
      {
        DuplicateAsJsonToOutputFile(processedFile, _outputFile);
      }

      Log.Information($"Cleaned sbom has been copied to '{_outputFile}'.");
    }
  }

  private void DuplicateAsJsonToOutputFile (AbsolutePath sbomFileToConvert, AbsolutePath finalJsonSbomLocation)
  {
    using var cleanedBomStream = File.OpenRead(sbomFileToConvert);
    var finalBom = XMLSerializer.Deserialize(cleanedBomStream);

    using var outputSbomJsonStream = File.Create(finalJsonSbomLocation);

    JsonSerializer.SerializeAsync(finalBom, outputSbomJsonStream).GetAwaiter().GetResult();
  }

  private void MergeSboms (AbsolutePath cleanedSbomFile, AbsolutePath npmSbomFile, AbsolutePath outputFile)
  {
    using var mainSbomFileStream = File.OpenRead(cleanedSbomFile);
    using var npmSbomFileStream = File.OpenRead(npmSbomFile);

    var mainSbom = XMLSerializer.Deserialize(mainSbomFileStream);
    var npmSbom = XMLSerializer.Deserialize(npmSbomFileStream);

    var component = new Component
    {
      Type = Component.Classification.Library,
      Name = _solution.Name + "-root-component",
      Version = _version
    };

    var combinedSbom = CycloneDXUtils.FlatMerge([mainSbom, npmSbom], component);

    using var outputFileStream = File.Create(outputFile);

    if (outputFile.Extension == ".xml")
    {
      XMLSerializer.Serialize(combinedSbom, outputFileStream);
    }
    if (outputFile.Extension == ".json")
    {
      JsonSerializer.SerializeAsync(combinedSbom, outputFileStream).GetAwaiter().GetResult();
    }

    outputFileStream.Close();

    Log.Information("Merged sbom created at {0}", outputFile);
  }

  private AbsolutePath CreateNPMSbom ()
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
    var npmSbomFile = _workingDirectory / npmSbomFileName;

    Log.Information("Creating npm sbom...");

    PowerShellTasks.PowerShell(_ => _
        .SetCommand($".\\node_modules\\.bin\\cyclonedx-npm.ps1 {packageJsonFolder /"package.json"} --omit dev --output-format xml --output-file '{npmSbomFile}'")
        .SetProcessWorkingDirectory(_workingDirectory));

    Log.Information("Created npm sbom at '{npmSbomLocation}'.", npmSbomFile);

    return npmSbomFile;
  }
}