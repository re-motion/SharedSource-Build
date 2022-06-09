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
using System.IO;
using Moq;
using NUnit.Framework;
using ReleaseProcessAutomation.Configuration;
using ReleaseProcessAutomation.Configuration.Data;
using ReleaseProcessAutomation.Extensions;
using ReleaseProcessAutomation.Git;
using ReleaseProcessAutomation.Jira;
using ReleaseProcessAutomation.ReadInput;
using ReleaseProcessAutomation.Scripting;
using ReleaseProcessAutomation.SemanticVersioning;
using ReleaseProcessAutomation.Steps.PipelineSteps;
using Spectre.Console;

namespace ReleaseProcessAutomation.Tests.Steps.Releases;

internal class ReleaseAlphaBetaStepTests
{
  private const string c_configFileName = "ReleaseProcessScript.Test.Config";
  private Configuration.Data.Config _config;

  private Mock<IAnsiConsole> _consoleMock;
  private Mock<IContinueAlphaBetaStep> _continueAlphaBetaMock;
  private Mock<IGitClient> _gitClientStub;
  private Mock<IInputReader> _inputReaderStub;
  private Mock<IJiraFunctionality> _jiraFunctionalityMock;
  private Mock<IMSBuildCallAndCommit> _msBuildInvokerMock;

  [SetUp]
  public void Setup ()
  {
    _gitClientStub = new Mock<IGitClient>();
    _inputReaderStub = new Mock<IInputReader>();
    _msBuildInvokerMock = new Mock<IMSBuildCallAndCommit>();
    _continueAlphaBetaMock = new Mock<IContinueAlphaBetaStep>();
    _consoleMock = new Mock<IAnsiConsole>();
    _jiraFunctionalityMock = new Mock<IJiraFunctionality>();

    var path = Path.Join(Environment.CurrentDirectory, c_configFileName);
    _config = new ConfigReader().LoadConfig(path);
  }

  [Test]
  public void Execute_OnMasterWithoutErrors_CallsNextStep ()
  {
    var nextVersion = new SemanticVersion
                      {
                          Major = 1,
                          Minor = 1,
                          Patch = 1
                      };
    var nextJiraVersion = new SemanticVersion();
    _gitClientStub.Setup(_ => _.IsWorkingDirectoryClean()).Returns(true);
    _gitClientStub.Setup(_ => _.IsCommitHash("")).Returns(true);
    _gitClientStub.Setup(_ => _.IsOnBranch("develop")).Returns(true);
    _gitClientStub.Setup(_ => _.GetCurrentBranchName()).Returns("develop");
    var nextPossibleVersions = nextVersion.GetNextPossibleVersionsDevelop();
    _inputReaderStub.Setup(_ => _.ReadVersionChoice(It.IsAny<string>(), nextPossibleVersions)).Returns(nextJiraVersion);

    var alphaBetaStep = new ReleaseAlphaBetaStep(
        _gitClientStub.Object,
        _config,
        _inputReaderStub.Object,
        _msBuildInvokerMock.Object,
        _continueAlphaBetaMock.Object,
        _consoleMock.Object,
        _jiraFunctionalityMock.Object);

    Assert.That(
        () => alphaBetaStep.Execute(nextVersion, "", false, false),
        Throws.Nothing);
    _continueAlphaBetaMock.Verify(_ => _.Execute(nextVersion, "develop", "develop", false));
    _jiraFunctionalityMock.Verify(_ => _.CreateAndReleaseJiraVersion(nextVersion, nextJiraVersion, false), Times.Exactly(1));
  }

  [Test]
  public void Execute_OnMasterWithoutErrorsButWithPauseForCommit_CallsInvokeMBuildAndCommitButNotNextStep ()
  {
    var nextVersion = new SemanticVersion
                      {
                          Major = 1,
                          Minor = 1,
                          Patch = 1
                      };
    var nextJiraVersion = new SemanticVersion();
    _gitClientStub.Setup(_ => _.IsWorkingDirectoryClean()).Returns(true);
    _gitClientStub.Setup(_ => _.IsCommitHash("")).Returns(true);
    _gitClientStub.Setup(_ => _.IsOnBranch("develop")).Returns(true);
    _gitClientStub.Setup(_ => _.GetCurrentBranchName()).Returns("develop");
    var nextPossibleVersions = nextVersion.GetNextPossibleVersionsDevelop();
    _inputReaderStub.Setup(_ => _.ReadVersionChoice(It.IsAny<string>(), nextPossibleVersions)).Returns(nextJiraVersion);

    var alphaBetaStep = new ReleaseAlphaBetaStep(
        _gitClientStub.Object,
        _config,
        _inputReaderStub.Object,
        _msBuildInvokerMock.Object,
        _continueAlphaBetaMock.Object,
        _consoleMock.Object,
        _jiraFunctionalityMock.Object);

    Assert.That(
        () => alphaBetaStep.Execute(nextVersion, "", true, false),
        Throws.Nothing);

    _msBuildInvokerMock.Verify(_ => _.CallMSBuildStepsAndCommit(It.IsAny<MSBuildMode>(), nextVersion));
    _jiraFunctionalityMock.Verify(_ => _.CreateAndReleaseJiraVersion(nextVersion, nextJiraVersion, false), Times.Exactly(1));
    _continueAlphaBetaMock.Verify(_ => _.Execute(nextVersion, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
  }
}