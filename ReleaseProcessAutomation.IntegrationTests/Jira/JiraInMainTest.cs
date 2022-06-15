using System;
using System.IO;
using Moq;
using NUnit.Framework;
using ReleaseProcessAutomation.Configuration;
using ReleaseProcessAutomation.Configuration.Data;
using ReleaseProcessAutomation.Jira.CredentialManagement;
using ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;
using ReleaseProcessAutomation.Jira.Utility;
using RestSharp.Authenticators;

namespace ReleaseProcessAutomation.IntegrationTests.Jira;
[TestFixture]
public class JiraInMainTest : IntegrationTestSetup
{
  private const string c_usernameEnvironmentVariableName = "JiraUsername";
  private const string c_passwordEnvironmentVariableName = "JiraPassword";

  private string _jiraUsername;
  private string _jiraPassword;
  private JiraProjectVersionService _service;
  private Config _config;
  
  [SetUp]
  public void Setup ()
  {
    _jiraUsername = Environment.GetEnvironmentVariable(c_usernameEnvironmentVariableName);
    _jiraPassword = Environment.GetEnvironmentVariable(c_passwordEnvironmentVariableName);

    if (string.IsNullOrEmpty(_jiraUsername))
      throw new InvalidOperationException($"Could not load credentials from environment variable '{c_usernameEnvironmentVariableName}'");

    if (string.IsNullOrEmpty(_jiraPassword))
      throw new InvalidOperationException($"Could not load credentials from environment variable '{c_passwordEnvironmentVariableName}'");

    var configURI = Path.Combine(Environment.CurrentDirectory, "Build", "Customizations", c_testConfigName);
    _config = new ConfigReader().LoadConfig(configURI);
    var testRestClient = JiraRestClient.CreateWithBasicAuthentication(_config.Jira.JiraURL, new Credentials(_jiraUsername, _jiraPassword));
    var restClientMock = new Mock<IJiraRestClientProvider>();
    restClientMock.Setup( _=>_.GetJiraRestClient()).Returns(testRestClient);
    
    var versionFinder = new JiraProjectVersionFinder(restClientMock.Object);
    var issueService = new JiraIssueService(restClientMock.Object);
    _service = new JiraProjectVersionService(restClientMock.Object, issueService, versionFinder);
  }
  
  [Test]
  public void TestFromDevelop_WithJira_CreatesAndMovesJiraVersions ()
  {
    
    JiraTestUtility.DeleteVersionsIfExistent(_config.Jira.JiraProjectKey, _service, "1.0.0", "1.1.0");
    
    ExecuteGitCommand("commit -m feature --allow-empty");
    ExecuteGitCommand("checkout -b develop");

    ExecuteGitCommand("commit -m feature2 --allow-empty");
    ExecuteGitCommand("commit -m feature3 --allow-empty");
    ExecuteGitCommand("commit -m feature4 --allow-empty");

    //Get release version from user
    TestConsole.Input.PushTextWithEnter("1.0.0");
    //Get next release version from user for jira
    TestConsole.Input.PushTextWithEnter("1.1.0");
    
    TestConsole.Input.PushTextWithEnter(_jiraUsername);
    TestConsole.Input.PushTextWithEnter(_jiraPassword);
    
    TestConsole.Input.PushTextWithEnter("n");
    

    var act = Program.Main(new[] { "Release-Version" });

    Assert.That(act, Is.EqualTo(0));
  }
}