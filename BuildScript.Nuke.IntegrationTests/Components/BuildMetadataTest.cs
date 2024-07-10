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

using NUnit.Framework;

namespace BuildScript.Nuke.IntegrationTests.Components;

[TestFixture]
public class BuildMetadataTest : IntegrationTestBase
{
  [Test]
  public void DetermineBuildMetadataTarget ()
  {
    var result = RunBuildCmd("DetermineBuildMetadata");

    var sectionOutput = result.GetSectionOutput("DetermineBuildMetadata");
    Assert.That(sectionOutput, Is.Not.Null);

    Assert.That(
        sectionOutput,
        Is.EqualTo(
            """
            xx:xx:xx [INF] Determined build metadata (UseReleaseVersioning = False).
            xx:xx:xx [INF] Build metadata for configuration 'Debug':
            xx:xx:xx [INF]   Version: 3.0.0-x.1.0
            xx:xx:xx [INF]   AssemblyVersion: 3.0.0.0
            xx:xx:xx [INF]   AssemblyFileVersion: 3.0.0.24001
            xx:xx:xx [INF]   AssemblyNuGetVersion: 3.0.0-x.1.0
            xx:xx:xx [INF]   AssemblyInformationalVersion: 3.0.0-x.1.0+Debug.Commit-eaeaeaeaeaeaeaeaeaeaeaeaeaeaeaeaeaeaeaea
            xx:xx:xx [INF]   AdditionalBuildMetadata: Commit-eaeaeaeaeaeaeaeaeaeaeaeaeaeaeaeaeaeaeaea
            xx:xx:xx [INF] Build metadata for configuration 'Release':
            xx:xx:xx [INF]   Version: 3.0.0-x.1.0
            xx:xx:xx [INF]   AssemblyVersion: 3.0.0.0
            xx:xx:xx [INF]   AssemblyFileVersion: 3.0.0.24001
            xx:xx:xx [INF]   AssemblyNuGetVersion: 3.0.0-x.1.0
            xx:xx:xx [INF]   AssemblyInformationalVersion: 3.0.0-x.1.0+Release.Commit-eaeaeaeaeaeaeaeaeaeaeaeaeaeaeaeaeaeaeaea
            xx:xx:xx [INF]   AdditionalBuildMetadata: Commit-eaeaeaeaeaeaeaeaeaeaeaeaeaeaeaeaeaeaeaea
            """
        ));
  }
  [Test]
  public void DetermineBuildMetadataTarget_WithReleaseVersioning ()
  {
    var result = RunBuildCmd("DetermineBuildMetadata -UseReleaseVersioning");

    var sectionOutput = result.GetSectionOutput("DetermineBuildMetadata");
    Assert.That(sectionOutput, Is.Not.Null);

    Assert.That(
        sectionOutput,
        Is.EqualTo(
            """
            xx:xx:xx [INF] Determined build metadata (UseReleaseVersioning = True).
            xx:xx:xx [INF] Build metadata for configuration 'Debug':
            xx:xx:xx [INF]   Version: 3.0.0
            xx:xx:xx [INF]   AssemblyVersion: 3.0.0.0
            xx:xx:xx [INF]   AssemblyFileVersion: 3.0.0.30000
            xx:xx:xx [INF]   AssemblyNuGetVersion: 3.0.0
            xx:xx:xx [INF]   AssemblyInformationalVersion: 3.0.0+Debug.Commit-eaeaeaeaeaeaeaeaeaeaeaeaeaeaeaeaeaeaeaea
            xx:xx:xx [INF]   AdditionalBuildMetadata: Commit-eaeaeaeaeaeaeaeaeaeaeaeaeaeaeaeaeaeaeaea
            xx:xx:xx [INF] Build metadata for configuration 'Release':
            xx:xx:xx [INF]   Version: 3.0.0
            xx:xx:xx [INF]   AssemblyVersion: 3.0.0.0
            xx:xx:xx [INF]   AssemblyFileVersion: 3.0.0.30000
            xx:xx:xx [INF]   AssemblyNuGetVersion: 3.0.0
            xx:xx:xx [INF]   AssemblyInformationalVersion: 3.0.0+Release.Commit-eaeaeaeaeaeaeaeaeaeaeaeaeaeaeaeaeaeaeaea
            xx:xx:xx [INF]   AdditionalBuildMetadata: Commit-eaeaeaeaeaeaeaeaeaeaeaeaeaeaeaeaeaeaeaea
            """
        ));
  }

  [Test]
  public void DetermineBuildMetadataTarget_WithExplicitAdditionalBuildMetadata ()
  {
    var result = RunBuildCmd("DetermineBuildMetadata --AdditionalBuildMetadata MyMetadata");

    var sectionOutput = result.GetSectionOutput("DetermineBuildMetadata");
    Assert.That(sectionOutput, Is.Not.Null);

    Assert.That(
        sectionOutput,
        Is.EqualTo(
            """
            xx:xx:xx [INF] Determined build metadata (UseReleaseVersioning = False).
            xx:xx:xx [INF] Build metadata for configuration 'Debug':
            xx:xx:xx [INF]   Version: 3.0.0-x.1.0
            xx:xx:xx [INF]   AssemblyVersion: 3.0.0.0
            xx:xx:xx [INF]   AssemblyFileVersion: 3.0.0.24001
            xx:xx:xx [INF]   AssemblyNuGetVersion: 3.0.0-x.1.0
            xx:xx:xx [INF]   AssemblyInformationalVersion: 3.0.0-x.1.0+Debug.MyMetadata
            xx:xx:xx [INF]   AdditionalBuildMetadata: MyMetadata
            xx:xx:xx [INF] Build metadata for configuration 'Release':
            xx:xx:xx [INF]   Version: 3.0.0-x.1.0
            xx:xx:xx [INF]   AssemblyVersion: 3.0.0.0
            xx:xx:xx [INF]   AssemblyFileVersion: 3.0.0.24001
            xx:xx:xx [INF]   AssemblyNuGetVersion: 3.0.0-x.1.0
            xx:xx:xx [INF]   AssemblyInformationalVersion: 3.0.0-x.1.0+Release.MyMetadata
            xx:xx:xx [INF]   AdditionalBuildMetadata: MyMetadata
            """
        ));
  }
}
