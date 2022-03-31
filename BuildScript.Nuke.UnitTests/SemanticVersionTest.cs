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
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Remotion.BuildScript;

namespace BuildScript.Nuke.UnitTests;

[TestFixture]
public class SemanticVersionTest
{
  [Test]
  [TestCase("3.0.0")]
  [TestCase("3.1.1")]
  [TestCase("3.0.0-alpha.30")]
  [TestCase("1.21.8-beta.102")]
  public void Version_WithValidVersion_ReturnsOriginalVersion (string version)
  {
    var semanticVersion = new SemanticVersion(version, false);
    var actualVersion = semanticVersion.Version;
    Assert.That(actualVersion, Is.EqualTo(version));
  }

  [Test]
  [TestCase("3.0.0", "3.0.0.0")]
  [TestCase("3.1.1", "3.1.0.0")]
  [TestCase("3.0.0-alpha.30", "3.0.0.0")]
  [TestCase("1.21.8-beta.102", "1.21.0.0")]
  public void AssemblyVersion_WithValidVersion_ReturnsAssemblyVersion (string version, string expectedAssemblyVersion)
  {
    var semanticVersion = new SemanticVersion(version, false);
    var assemblyVersion = semanticVersion.AssemblyVersion;
    Assert.That(assemblyVersion, Is.EqualTo(expectedAssemblyVersion));
  }

  [Test]
  [TestCase("3.0.0", "3.0.0.30000")]
  [TestCase("3.1.0", "3.1.0.30000")]
  [TestCase("3.0.0-alpha.30", "3.0.0.01030")]
  [TestCase("1.21.8-beta.102", "1.21.8.02102")]
  public void AssemblyFileVersion_WithValidVersion_ReturnsAssemblyFileVersion (string version, string expectedAssemblyFileVersion)
  {
    var semanticVersion = new SemanticVersion(version, false);
    var assemblyFileVersion = semanticVersion.AssemblyFileVersion;
    Assert.That(assemblyFileVersion, Is.EqualTo(expectedAssemblyFileVersion));
  }

  [Test]
  [TestCase("3.0.0", "3.0.0+Debug", "Debug", "")]
  [TestCase("3.1.0", "3.1.0+Release", "Release", "")]
  [TestCase("3.0.0-alpha.30", "3.0.0-alpha.30+Release", "Release", "")]
  [TestCase("1.21.8-beta.102", "1.21.8-beta.102+Release.buildMetaData", "Release", "buildMetaData")]
  [TestCase("1.21.8-beta.102", "1.21.8-beta.102+Release.buildMetaData3422", "Release", "buildMetaData3422")]
  public void GetAssemblyInformationVersion_WithValidVersion_ReturnsAssemblyInformationVersion (
      string version,
      string expectedAssemblyInformationVersion,
      string configurationId,
      string additionalBuildMetadata)
  {
    var semanticVersion = new SemanticVersion(version, false);
    var assemblyInformationVersion = semanticVersion.GetAssemblyInformationalVersion(configurationId, additionalBuildMetadata);
    Assert.That(assemblyInformationVersion, Is.EqualTo(expectedAssemblyInformationVersion));
  }

  [Test]
  [TestCase("3.0.0", "3.0.0")]
  [TestCase("3.1.0", "3.1.0")]
  [TestCase("3.0.0-alpha.30", "3.0.0-alpha.30")]
  [TestCase("1.21.8-beta.102", "1.21.8-beta.102")]
  public void AssemblyNuGetVersion_WithValidVersion_ReturnsAssemblyNugetVersion (string version, string expectedAssemblyNuGetVersion)
  {
    var semanticVersion = new SemanticVersion(version, false);
    var assemblyNuGetVersion = semanticVersion.AssemblyNuGetVersion;
    Assert.That(assemblyNuGetVersion, Is.EqualTo(expectedAssemblyNuGetVersion));
  }

  [Test]
  [TestCase("3.0.0", "3.0.0.0")]
  [TestCase("3.1.1", "3.1.0.0")]
  [TestCase("3.0.0-alpha.30", "3.0.0.0")]
  [TestCase("1.21.8-beta.102", "1.21.0.0")]
  public void DependDBProjectVersion_WithValidVersion_ReturnsDepenDBProjectVersion (string version, string expectedDefineDependDBProjectVersion)
  {
    var semanticVersion = new SemanticVersion(version, false);
    var dependDbProjectVersion = semanticVersion.DependDBProjectVersion;
    Assert.That(dependDbProjectVersion, Is.EqualTo(expectedDefineDependDBProjectVersion));
  }

  [Test]
  [TestCase("3.0")]
  [TestCase("3.b.0")]
  [TestCase("3.0.c-2alpha.30")]
  [TestCase("d.21.8-beta.alpha")]
  [TestCase("wrong")]
  public void Version_WithInvalidVersion_ThrowsNukeBuildException (string version)
  {
    Assert.That(() => new SemanticVersion(version, false), Throws.InstanceOf<ApplicationException>());
  }

  [Test]
  [TestCase("3.0.0", @"3\.0\.0")]
  [TestCase("3.1.1", @"3\.1\.1")]
  [TestCase("3.0.0-alpha.30", @"3\.0\.0")]
  [TestCase("1.21.8-beta.102", @"1\.21\.8")]
  public void Version_WithLocalBuildTrue_ReturnsValidVersionWithLocalBuildSuffix (string version, string expectedVersion)
  {
    var semanticVersion = new SemanticVersion(version, true);
    
    var actualVersion = semanticVersion.Version;

    //language=regex
    var regexPattern = expectedVersion + @"-x\.9\.\d{6}-\d{6}";
    var versionMatch = Regex.Match(actualVersion, regexPattern);
    Assert.That(versionMatch.Success, Is.True);
  }

  [Test]
  [TestCase("3.0.0", "Debug", "", @"3\.0\.0")]
  [TestCase("3.1.0", "Release", "", @"3\.1\.0")]
  [TestCase("3.0.0-alpha.30", "Release", "", @"3\.0\.0")]
  [TestCase("1.21.8-beta.102", "Release", "buildMetaData", @"1\.21\.8")]
  [TestCase("1.21.8-beta.102", "Release", "buildMetaData3422", @"1\.21\.8")]
  public void GetAssemblyInformationVersion_WithLocalBuildTrue_ReturnsAssemblyInformationVersionWithLocalBuildSuffix (
      string version,
      string configurationId,
      string additionalBuildMetadata,
      string expectedVersion)
  {
    var semanticVersion = new SemanticVersion(version, true);
    var assemblyInformationVersion = semanticVersion.GetAssemblyInformationalVersion(configurationId, additionalBuildMetadata);
    
    //language=regex
    var regexPattern = expectedVersion + @"-x\.9\.\d{6}-\d{6}\+" 
                                       + configurationId 
                                       + (string.IsNullOrEmpty(additionalBuildMetadata) ? "" : $".{additionalBuildMetadata}");
    var versionMatch = Regex.Match(assemblyInformationVersion, regexPattern);
    Assert.That(versionMatch.Success, Is.True);
  }

  [Test]
  [TestCase("3.0.0", @"3\.0\.0")]
  [TestCase("3.1.1", @"3\.1\.1")]
  [TestCase("3.0.0-alpha.30", @"3\.0\.0")]
  [TestCase("1.21.8-beta.102", @"1\.21\.8")]
  public void AssemblyNuGetVersion_WithLocalBuildTrue_ReturnsAssemblyNugetVersionWithLocalBuildSuffix (string version, string expectedVersion)
  {
    var semanticVersion = new SemanticVersion(version, true);
    var actualVersion = semanticVersion.AssemblyNuGetVersion;
    
    //language=regex
    var regexPattern = expectedVersion + @"-x\.9\.\d{6}-\d{6}";
    var versionMatch = Regex.Match(actualVersion, regexPattern);
    Assert.That(versionMatch.Success, Is.True);
  }
}