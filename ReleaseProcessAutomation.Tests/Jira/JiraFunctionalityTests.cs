using System;
using Moq;
using NUnit.Framework;
using ReleaseProcessAutomation.Configuration.Data;
using ReleaseProcessAutomation.Jira;
using ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;
using ReleaseProcessAutomation.ReadInput;
using ReleaseProcessAutomation.SemanticVersioning;
using Spectre.Console.Testing;

namespace ReleaseProcessAutomation.Tests.Jira;

[TestFixture]
public class JiraFunctionalityTests
{
  
  private Mock<IJiraVersionReleaser> _jiraVersionReleaserMock;
  private Mock<IJiraVersionCreator> _jiraVersionCreatorMock;
  private Mock<IJiraCredentialManager> _jiraCredentialManagerStub;
  private TestConsole _console;
  private Configuration.Data.Config _config;

  private const string c_userName = "user";
  private const string c_password = "password";
  private const string c_target = "target";
  private const string c_postfix = "postfix";
  [SetUp]
  public void Setup ()
  {
    _jiraVersionReleaserMock = new Mock<IJiraVersionReleaser>();
    _jiraVersionCreatorMock = new Mock<IJiraVersionCreator>();
    _jiraCredentialManagerStub = new Mock<IJiraCredentialManager>();
    _console = new TestConsole();
    _config = new Configuration.Data.Config();
    _config.Jira = new JiraConfig();
    _config.Jira.StringUseNTLM = "true";
    _config.Jira.JiraURL = "https://www.JiraURL.com/url";
    _config.Jira.JiraProjectKey = "JiraProjectKey";
  }

  [Test]
  public void CreateAndReleaseJiraVersion_CreatesTwoVersionsCorrectly_ThrowsNothing ()
  {

    var currentVersion = new SemanticVersion();
    var nextVersion = new SemanticVersion { Patch = 1 };
    var jiraFunctionality = new JiraFunctionality(
        _config,
        _console,
        _jiraVersionReleaserMock.Object,
        _jiraVersionCreatorMock.Object,
        _jiraCredentialManagerStub.Object,
        c_postfix);
    
    Assert.That(() => jiraFunctionality.CreateAndReleaseJiraVersion(currentVersion, nextVersion, false), Throws.Nothing);
    
    _jiraVersionCreatorMock.Verify(_=>_.CreateNewVersionWithVersionNumber(_config.Jira.JiraProjectKey, currentVersion.ToString(), It.IsAny<JiraRestClient>()), Times.Once);
    _jiraVersionCreatorMock.Verify(_=>_.CreateNewVersionWithVersionNumber(_config.Jira.JiraProjectKey, nextVersion.ToString(), It.IsAny<JiraRestClient>()), Times.Once);
  }
  
  [Test]
  public void CreateAndReleaseJiraVersion_WithoutSquashing_ReleaseWithoutSquash ()
  {
    var versionID = "curr";
    var nextID = "next";
    
    var currentVersion = new SemanticVersion();
    var nextVersion = new SemanticVersion { Patch = 1 };
    _jiraVersionCreatorMock.Setup(_=>_.CreateNewVersionWithVersionNumber(_config.Jira.JiraProjectKey, currentVersion.ToString(), It.IsAny<JiraRestClient>())).Returns(versionID);
    _jiraVersionCreatorMock.Setup(_=>_.CreateNewVersionWithVersionNumber(_config.Jira.JiraProjectKey, nextVersion.ToString(), It.IsAny<JiraRestClient>())).Returns(nextID);

    var jiraFunctionality = new JiraFunctionality(
        _config,
        _console,
        _jiraVersionReleaserMock.Object,
        _jiraVersionCreatorMock.Object,
        _jiraCredentialManagerStub.Object,
        c_postfix);
    
    Assert.That(() => jiraFunctionality.CreateAndReleaseJiraVersion(currentVersion, nextVersion, false), Throws.Nothing);
    
    _jiraVersionReleaserMock.Verify(_=>_.ReleaseVersion(
          It.IsAny<string>(),
          versionID,
          nextID,
          false,
          It.IsAny<JiraRestClient>()),
        Times.Once);
    
    _jiraVersionReleaserMock.Verify(_=>_.ReleaseVersionAndSquashUnreleased(
            It.IsAny<string>(),
            It.IsAny<string>(),
            versionID,
            nextID,
            It.IsAny<JiraRestClient>()),
        Times.Never);
  }
  
  [Test]
  public void CreateAndReleaseJiraVersion_WithoutSquashing_ReleaseWithSquash ()
  {
    var versionID = "curr";
    var nextID = "next";
    
    var currentVersion = new SemanticVersion();
    var nextVersion = new SemanticVersion { Patch = 1 };
    _jiraVersionCreatorMock.Setup(_=>_.CreateNewVersionWithVersionNumber(_config.Jira.JiraProjectKey, currentVersion.ToString(), It.IsAny<JiraRestClient>())).Returns(versionID);
    _jiraVersionCreatorMock.Setup(_=>_.CreateNewVersionWithVersionNumber(_config.Jira.JiraProjectKey, nextVersion.ToString(), It.IsAny<JiraRestClient>())).Returns(nextID);

    var jiraFunctionality = new JiraFunctionality(
        _config,
        _console,
        _jiraVersionReleaserMock.Object,
        _jiraVersionCreatorMock.Object,
        _jiraCredentialManagerStub.Object,
        c_postfix);
    
    Assert.That(() => jiraFunctionality.CreateAndReleaseJiraVersion(currentVersion, nextVersion, true), Throws.Nothing);
    
    _jiraVersionReleaserMock.Verify(_=>_.ReleaseVersion(
            It.IsAny<string>(),
            versionID,
            nextID,
            false,
            It.IsAny<JiraRestClient>()),
        Times.Never);
    
    _jiraVersionReleaserMock.Verify(_=>_.ReleaseVersionAndSquashUnreleased(
            It.IsAny<string>(),
            It.IsAny<string>(),
            versionID,
            nextID,
            It.IsAny<JiraRestClient>()),
        Times.Once);
  }
}