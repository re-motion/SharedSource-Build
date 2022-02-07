using System;
using NUnit.Framework;

namespace BuildScript.Nuke.UnitTests;

[TestFixture]
public class VersionHelperTest
{
  [Test]
  [TestCase("3.0.0")]
  [TestCase("3.1.1")]
  [TestCase("3.0.0-alpha.30")]
  [TestCase("1.21.8-beta.102")]
  public void Version_WithValidVersion_ReturnsOriginalVersion (string version)
  {
    var versionHelper = new VersionHelper(version);
    var actualVersion = versionHelper.Version;
    Assert.That(actualVersion, Is.EqualTo(version));
  }

  [Test]
  [TestCase("3.0.0", "3.0.0.0")]
  [TestCase("3.1.1", "3.1.0.0")]
  [TestCase("3.0.0-alpha.30", "3.0.0.0")]
  [TestCase("1.21.8-beta.102", "1.21.0.0")]
  public void AssemblyVersion_WithValidVersion_ReturnsAssemblyVersion (string version, string expectedAssemblyVersion)
  {
    var versionHelper = new VersionHelper(version);
    var assemblyVersion = versionHelper.AssemblyVersion;
    Assert.That(assemblyVersion, Is.EqualTo(expectedAssemblyVersion));
  }

  [Test]
  [TestCase("3.0.0", "3.0.0.30000")]
  [TestCase("3.1.0", "3.1.0.30000")]
  [TestCase("3.0.0-alpha.30", "3.0.0.01030")]
  [TestCase("1.21.8-beta.102", "1.21.8.02102")]
  public void AssemblyFileVersion_WithValidVersion_ReturnsAssemblyFileVersion (string version, string expectedAssemblyFileVersion)
  {
    var versionHelper = new VersionHelper(version);
    var assemblyFileVersion = versionHelper.AssemblyFileVersion;
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
    var versionHelper = new VersionHelper(version);
    var assemblyInformationVersion = versionHelper.GetAssemblyInformationVersion(configurationId, additionalBuildMetadata);
    Assert.That(assemblyInformationVersion, Is.EqualTo(expectedAssemblyInformationVersion));
  }

  [Test]
  [TestCase("3.0.0", "3.0.0")]
  [TestCase("3.1.0", "3.1.0")]
  [TestCase("3.0.0-alpha.30", "3.0.0-alpha.30")]
  [TestCase("1.21.8-beta.102", "1.21.8-beta.102")]
  public void AssemblyNuGetVersion_WithValidVersion_ReturnsAssemblyNugetVersion (string version, string expectedAssemblyNuGetVersion)
  {
    var versionHelper = new VersionHelper(version);
    var assemblyNuGetVersion = versionHelper.AssemblyNuGetVersion;
    Assert.That(assemblyNuGetVersion, Is.EqualTo(expectedAssemblyNuGetVersion));
  }

  [Test]
  [TestCase("3.0.0", "3.0.0.0")]
  [TestCase("3.1.1", "3.1.0.0")]
  [TestCase("3.0.0-alpha.30", "3.0.0.0")]
  [TestCase("1.21.8-beta.102", "1.21.0.0")]
  public void DependDBProjectVersion_WithValidVersion_ReturnsDepenDBProjectVersion (string version, string expectedDefineDependDBProjectVersion)
  {
    var versionHelper = new VersionHelper(version);
    var dependDBProjectVersion = versionHelper.DependDBProjectVersion;
    Assert.That(dependDBProjectVersion, Is.EqualTo(expectedDefineDependDBProjectVersion));
  }

  [Test]
  [TestCase("3.0")]
  [TestCase("3.b.0")]
  [TestCase("3.0.c-2alpha.30")]
  [TestCase("d.21.8-beta.alpha")]
  [TestCase("wrong")]
  public void Version_WithInvalidVersion_ThrowsNukeBuildException (string version)
  {
    Assert.That(() => new VersionHelper(version), Throws.InstanceOf<ApplicationException>());
  }
}