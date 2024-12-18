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
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using SbomCleaner.Library;

namespace Remotion.BuildScript.GenerateSbom;

public class SolutionSbomGeneratorBuilder : ISbomGeneratorBuilder
{
  private readonly Solution _solution;
  private readonly string _version;
  private readonly List<string> _packageBlackList = new();
  private readonly List<string> _projectBlackList = new();

  private readonly AbsolutePath _outputFile;
  private readonly AbsolutePath _tempDirectory;
  private AbsolutePath _pathToPackageJson;

  private readonly string _githubUsername;
  private readonly string _githubAccessToken;

  public SolutionSbomGeneratorBuilder (Solution solution, string version, AbsolutePath tempDirectory, AbsolutePath outputFile, string githubUsername, string githubAccessToken)
  {
    _solution = solution;
    _version = version;
    _tempDirectory = tempDirectory;
    _outputFile = outputFile;

    _githubAccessToken = githubAccessToken;
    _githubUsername = githubUsername;
  }

  public SolutionSbomGeneratorBuilder WithPackagesBlackListed (params string[] packagesToBlacklist)
  {
    _packageBlackList.AddRange(packagesToBlacklist);
    return this;
  }

  public SolutionSbomGeneratorBuilder WithProjectsBlackListed (params string[] projectsToBlacklist)
  {
    _projectBlackList.AddRange(projectsToBlacklist);
    return this;
  }

  public SolutionSbomGeneratorBuilder WithPackageJsonFile (AbsolutePath packageJsonFile)
  {
    _pathToPackageJson = packageJsonFile;
    return this;
  }

  public ISbomGenerator Build ()
  {
    return new SolutionSbomGenerator(
        _solution,
        _version,
        _tempDirectory,
        _outputFile,
        new FilterList(_packageBlackList),
        new FilterList(_projectBlackList),
        _pathToPackageJson,
        _githubUsername,
        _githubAccessToken);
  }
}