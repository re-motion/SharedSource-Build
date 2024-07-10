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
using NUnit.Framework;
using Remotion.BuildScript;

namespace BuildScript.Nuke.UnitTests;

[TestFixture]
public class RemotionBuildVersionFormatterTest
{
  static readonly Func<DateTime> _getDateTime = () => new DateTime(2022, 10, 03, 13, 01, 20);

  [Test]
  [TestCase("asd")]
  [TestCase("1.2.3.4")]
  [TestCase("1.2.3-alpha")]
  [TestCase("1.2.3-alpha.1000")]
  [TestCase("1.2.3-alpha.test")]
  [TestCase("1.2.3+test")]
  public void FormatRemotionBuildVersion_FailsOnInvalidVersion (string version)
  {
    var buildVersionFormatter = new RemotionBuildVersionFormatter(_getDateTime);

    Assert.That(
        () => buildVersionFormatter.FormatRemotionBuildVersion(version, false, false, "1"),
        Throws.TypeOf<FormatException>()
            .With.Message.EqualTo(
                $"The specified version '{version}' is invalid. "
                + "Only versions according to SemVer 2.0 without build metadata are allowed. "
                + "In case of a pre-release version, the release-counter may only contain up to 3 digits (e.g. '-alpha.123')."));
  }

  [Test]
  [TestCase(
      "3.0.1", false, true,
      "3.0.1-x.9.221003-130120", "3.0.0.0", "3.0.1.24009")]
  [TestCase(
      "3.0.1", true, true,
      "3.0.1", "3.0.0.0", "3.0.1.30000")]
  [TestCase(
      "3.0.1", false, false,
      "3.0.1-x.9", "3.0.0.0", "3.0.1.24009")]
  [TestCase(
      "3.0.1", true, false,
      "3.0.1-x.1.13", "3.0.0.0", "3.0.1.24001")]
  [TestCase(
      "3.0.1-alpha.3", false, true,
      "3.0.1-x.9.221003-130120", "3.0.0.0", "3.0.1.24009")]
  [TestCase(
      "3.0.1-alpha.3", true, true,
      "3.0.1-alpha.3", "3.0.0.0", "3.0.1.01003")]
  [TestCase(
      "3.0.1-alpha.3", false, false,
      "3.0.1-x.9", "3.0.0.0", "3.0.1.24009")]
  [TestCase(
      "3.0.1-alpha.3", true, false,
      "3.0.1-x.1.13", "3.0.0.0", "3.0.1.24001")]
  public void FormatRemotionBuildVersion_ValidVersions (
      string versionString,
      bool isServerBuild,
      bool useReleaseVersioning,
      string resultVersion,
      string resultAssemblyVersion,
      string resultAssemblyFileVersion)
  {
    var buildVersionFormatter = new RemotionBuildVersionFormatter(_getDateTime);
    var remotionBuildVersion = buildVersionFormatter.FormatRemotionBuildVersion(versionString, isServerBuild, useReleaseVersioning, "13");

    var expectedRemotionBuildVersion = new RemotionBuildVersion(
        resultVersion,
        resultAssemblyVersion,
        resultAssemblyFileVersion,
        resultVersion);

    Assert.That(remotionBuildVersion, Is.EqualTo(expectedRemotionBuildVersion));
  }

  [Test]
  [TestCase("1.2.3", "Debug", null, "1.2.3+Debug")]
  [TestCase("1.2.3", "Debug", "Meta", "1.2.3+Debug.Meta")]
  public void FormatAssemblyInformationalVersion (string version, string configuration, string? metadata, string expectedVersion)
  {
    var buildVersionFormatter = new RemotionBuildVersionFormatter(_getDateTime);

    Assert.That(
            buildVersionFormatter.FormatAssemblyInformationalVersion(version, configuration, metadata),
            Is.EqualTo(expectedVersion));
  }
}
