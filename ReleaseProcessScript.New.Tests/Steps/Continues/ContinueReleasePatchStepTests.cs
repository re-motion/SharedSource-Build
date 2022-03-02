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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using ReleaseProcessScript.New.Configuration;
using ReleaseProcessScript.New.Configuration.Data;
using ReleaseProcessScript.New.Extensions;
using ReleaseProcessScript.New.Git;
using ReleaseProcessScript.New.ReadInput;
using ReleaseProcessScript.New.Scripting;
using ReleaseProcessScript.New.SemanticVersioning;
using ReleaseProcessScript.New.Steps;
using ReleaseProcessScript.New.Steps.PipelineSteps;
using Spectre.Console;

namespace ReleaseProcessScript.New.Tests.Steps.Continues
{
  internal class ContinueReleasePatchStepTests
  {
    [SetUp]
    public void Setup()
    {
      _gitClientStub = new Mock<IGitClient>();
      _inputReaderStub = new Mock<IInputReader>();
      _msBuildExecutorMock = new Mock<IMSBuildCallAndCommit>();
      _pushReleasePatchMock = new Mock<IPushPatchReleaseStep>();
      _consoleStub = new Mock<IAnsiConsole>();

      var path = Path.Join(Environment.CurrentDirectory, c_configFileName);
      _config = new ConfigReader().LoadConfig(path);

    }

    private Mock<IAnsiConsole> _consoleStub;
    private Mock<IGitClient> _gitClientStub;
    private Mock<IInputReader> _inputReaderStub;
    private Configuration.Data.Config _config;
    private Mock<IMSBuildCallAndCommit> _msBuildExecutorMock;
    private Mock<IPushPatchReleaseStep> _pushReleasePatchMock;
    private const string c_configFileName = "ReleaseProcessScript.Test.Config";

    [Test]
    public void Execute_OnMasterWithoutErrors_CallsNextStep ()
    {
      _gitClientStub.Setup(_ => _.IsWorkingDirectoryClean()).Returns(true);
      _pushReleasePatchMock.Setup(_ => _.Execute("master", "v0.0.0", "release/v0.0.0")).Verifiable();

      var releasePatchStep = new ContinueReleasePatchStep(
          _gitClientStub.Object,
          _config,
          _inputReaderStub.Object,
          _msBuildExecutorMock.Object,
          _pushReleasePatchMock.Object,
          _consoleStub.Object);

      Assert.That(
          () => releasePatchStep.Execute(new SemanticVersion(), false, true),
          Throws.Nothing);
      _pushReleasePatchMock.Verify();
    }

    [Test]
    public void Execute_NotOnMasterWithoutErrors_CallsNextStep()
    {
      _gitClientStub.Setup(_ => _.IsWorkingDirectoryClean()).Returns(true);
      _pushReleasePatchMock.Setup(_ => _.Execute("support/v0.0", "v0.0.0", "release/v0.0.0")).Verifiable();

      var releasePatchStep = new ContinueReleasePatchStep(
          _gitClientStub.Object,
          _config,
          _inputReaderStub.Object,
          _msBuildExecutorMock.Object,
          _pushReleasePatchMock.Object,
          _consoleStub.Object);



      Assert.That(
          () => releasePatchStep.Execute(new SemanticVersion(), false, false),
          Throws.Nothing);
      _pushReleasePatchMock.Verify();
    }

    [Test]
    public void Execute_NotOnMasterWithoutErrorsButWithNoPush_DoesNotCallNextStepButCallsMBuild()
    {
      _gitClientStub.Setup(_ => _.IsWorkingDirectoryClean()).Returns(true);
      _msBuildExecutorMock.Setup(_ => _.CallMSBuildStepsAndCommit(MSBuildMode.DevelopmentForNextRelease, new SemanticVersion().GetNextPatchVersion())).Verifiable();

      var releasePatchStep = new ContinueReleasePatchStep(
          _gitClientStub.Object,
          _config,
          _inputReaderStub.Object,
          _msBuildExecutorMock.Object,
          _pushReleasePatchMock.Object,
          _consoleStub.Object);



      Assert.That(
          () => releasePatchStep.Execute(new SemanticVersion(), true, false),
          Throws.Nothing);
      _msBuildExecutorMock.Verify();
      _pushReleasePatchMock.Verify(_=>_.Execute(It.IsAny<string>(),It.IsAny<string>(),It.IsAny<string>()),Times.Never);
    }

    [Test]
    public void Execute_OnMasterWithoutErrorsButWithNoPush_DoesNotCallNextStepButCallsMBuild()
    {
      _gitClientStub.Setup(_ => _.IsWorkingDirectoryClean()).Returns(true);
      _msBuildExecutorMock.Setup(_ => _.CallMSBuildStepsAndCommit(MSBuildMode.DevelopmentForNextRelease, new SemanticVersion().GetNextPatchVersion())).Verifiable();

      var releasePatchStep = new ContinueReleasePatchStep(
          _gitClientStub.Object,
          _config,
          _inputReaderStub.Object,
          _msBuildExecutorMock.Object,
          _pushReleasePatchMock.Object,
          _consoleStub.Object);



      Assert.That(
          () => releasePatchStep.Execute(new SemanticVersion(), true, true),
          Throws.Nothing);
      _msBuildExecutorMock.Verify();
      _pushReleasePatchMock.Verify(_ => _.Execute(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
  }
}
