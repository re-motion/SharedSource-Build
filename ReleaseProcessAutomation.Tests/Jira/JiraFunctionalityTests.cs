using System;
using Moq;
using NUnit.Framework;
using ReleaseProcessAutomation.Configuration.Data;
using ReleaseProcessAutomation.Jira;
using ReleaseProcessAutomation.SemanticVersioning;
using Spectre.Console.Testing;

namespace ReleaseProcessAutomation.Tests.Jira;

[TestFixture]
public class JiraFunctionalityTests
{
  [SetUp]
  public void Setup ()
  {
    _jiraMock = new Mock<IJira>();
    _jiraMock.Setup(_ => _.VersionCreator.CreateNewVersionWithVersionNumber(It.IsAny<string>(), It.IsAny<string>()));
    _jiraMock.Setup(_ => _.VersionReleaser.ReleaseVersion(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), false));
    _jiraMock.Setup(
        _ => _.VersionReleaser.ReleaseVersionAndSquashUnreleased(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));

    _console = new TestConsole();
    _config = new Configuration.Data.Config();
    _config.Jira = new JiraConfig();
    _config.Jira.StringUseNTLM = "true";
    _config.Jira.JiraURL = "https://www.JiraURL.com/url";
    _config.Jira.JiraProjectKey = "JiraProjectKey";
  }

  private Mock<IJira> _jiraMock;
  private TestConsole _console;
  private Configuration.Data.Config _config;

  private const string c_userName = "user";
  private const string c_password = "password";
  private const string c_target = "target";
  private const string c_postfix = "postfix";

  [Test]
  public void CreateAndReleaseJiraVersion_CreatesTwoVersionsCorrectly_ThrowsNothing ()
  {
    var currentVersion = new SemanticVersion();
    var nextVersion = new SemanticVersion { Patch = 1 };
    var jiraFunctionality = new JiraFunctionality(
        _config,
        _console,
        _jiraMock.Object,
        c_postfix);

    _jiraMock.Setup(_ => _.VersionCreator.CreateNewVersionWithVersionNumber(_config.Jira.JiraProjectKey, currentVersion.ToString()))
        .Returns("current").Verifiable();
    _jiraMock.Setup(_ => _.VersionCreator.CreateNewVersionWithVersionNumber(_config.Jira.JiraProjectKey, nextVersion.ToString()))
        .Returns("next").Verifiable();

    Assert.That(() => jiraFunctionality.CreateAndReleaseJiraVersion(currentVersion, nextVersion), Throws.Nothing);

    _jiraMock.Verify();
  }

  [Test]
  public void CreateAndReleaseJiraVersion_WithoutSquashing_ReleaseWithoutSquash ()
  {
    var versionID = "curr";
    var nextID = "next";

    var currentVersion = new SemanticVersion();
    var nextVersion = new SemanticVersion { Patch = 1 };
    _jiraMock.Setup(_ => _.VersionCreator.CreateNewVersionWithVersionNumber(_config.Jira.JiraProjectKey, currentVersion.ToString()))
        .Returns(versionID);
    _jiraMock.Setup(_ => _.VersionCreator.CreateNewVersionWithVersionNumber(_config.Jira.JiraProjectKey, nextVersion.ToString())).Returns(nextID);

    var jiraFunctionality = new JiraFunctionality(
        _config,
        _console,
        _jiraMock.Object,
        c_postfix);

    Assert.That(() => jiraFunctionality.CreateAndReleaseJiraVersion(currentVersion, nextVersion), Throws.Nothing);

    _jiraMock.Verify(_ => _.VersionReleaser.ReleaseVersion(It.IsAny<string>(), versionID, nextID, false), Times.Once);
    _jiraMock.Verify(
        _ => _.VersionReleaser.ReleaseVersionAndSquashUnreleased(It.IsAny<string>(), It.IsAny<string>(), versionID, nextID),
        Times.Never);
  }

  [Test]
  public void CreateAndReleaseJiraVersion_WithoutSquashing_ReleaseWithSquash ()
  {
    var versionID = "curr";
    var nextID = "next";

    var currentVersion = new SemanticVersion();
    var nextVersion = new SemanticVersion { Patch = 1 };
    _jiraMock.Setup(_ => _.VersionCreator.CreateNewVersionWithVersionNumber(_config.Jira.JiraProjectKey, currentVersion.ToString()))
        .Returns(versionID);
    _jiraMock.Setup(_ => _.VersionCreator.CreateNewVersionWithVersionNumber(_config.Jira.JiraProjectKey, nextVersion.ToString())).Returns(nextID);

    var jiraFunctionality = new JiraFunctionality(
        _config,
        _console,
        _jiraMock.Object,
        c_postfix);

    Assert.That(() => jiraFunctionality.CreateAndReleaseJiraVersion(currentVersion, nextVersion, true), Throws.Nothing);

    _jiraMock.Verify(_ => _.VersionReleaser.ReleaseVersion(It.IsAny<string>(), versionID, nextID, false), Times.Never);
    _jiraMock.Verify(_ => _.VersionReleaser.ReleaseVersionAndSquashUnreleased(It.IsAny<string>(), It.IsAny<string>(), versionID, nextID), Times.Once);
  }
}