using System;
using System.IO;
using System.Linq;
using Moq;
using NUnit.Framework;
using ReleaseProcessAutomation.Configuration;
using ReleaseProcessAutomation.Configuration.Data;
using ReleaseProcessAutomation.Jira.CredentialManagement;
using ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;
using ReleaseProcessAutomation.Jira.Utility;

namespace ReleaseProcessAutomation.IntegrationTests.Jira;
[TestFixture]
public class JiraIntegrationTests : IntegrationTestSetup
{
  private const string c_usernameEnvironmentVariableName = "JiraUsername";
  private const string c_passwordEnvironmentVariableName = "JiraPassword";

  private string _jiraUsername;
  private string _jiraPassword;
  private JiraProjectVersionService _service;
  private Config _config;
  private JiraRestClient _testRestClient;

  [SetUp]
  public void Setup ()
  {
    _jiraUsername = Environment.GetEnvironmentVariable(c_usernameEnvironmentVariableName);
    _jiraPassword = Environment.GetEnvironmentVariable(c_passwordEnvironmentVariableName);

    if (string.IsNullOrEmpty(_jiraUsername))
      throw new InvalidOperationException($"Could not load credentials from environment variable '{c_usernameEnvironmentVariableName}'");

    if (string.IsNullOrEmpty(_jiraPassword))
      throw new InvalidOperationException($"Could not load credentials from environment variable '{c_passwordEnvironmentVariableName}'");

    var configUri = Path.Combine(Environment.CurrentDirectory, "Build", "Customizations", c_testConfigName);
    _config = new ConfigReader().LoadConfig(configUri);
    _testRestClient = JiraRestClient.CreateWithBasicAuthentication(_config.Jira.JiraURL, new Credentials(_jiraUsername, _jiraPassword));
    var restClientMock = new Mock<IJiraRestClientProvider>();
    restClientMock.Setup( _=>_.GetJiraRestClient()).Returns(_testRestClient);
    
    var versionFinder = new JiraProjectVersionFinder(restClientMock.Object);
    var issueService = new JiraIssueService(restClientMock.Object);
    _service = new JiraProjectVersionService(restClientMock.Object, issueService, versionFinder);
  }
  
  [Test]
  public void TestJiraRunThrough_MovesOnlyOpenJiraVersions ()
  {
    
    JiraTestUtility.DeleteVersionIfExistent(_config.Jira.JiraProjectKey,  "1.0.0", _testRestClient);
    JiraTestUtility.DeleteVersionIfExistent(_config.Jira.JiraProjectKey,  "1.1.0", _testRestClient);
    
    var newVersionID = JiraTestUtility.CreateVersion(_testRestClient, "1.0.0", _config.Jira.JiraProjectKey);
    
    var openIssue = JiraTestUtility.AddTestIssueToVersion(
        "Some open issue", 
        false, 
        _config.Jira.JiraProjectKey, 
        _testRestClient, 
      new JiraProjectVersion(){name = "1.0.0", id = newVersionID});
    
    var closedIssue = JiraTestUtility.AddTestIssueToVersion(
      "Some closed issue",
      true,
      _config.Jira.JiraProjectKey,
      _testRestClient,
      new JiraProjectVersion() { name = "1.0.0", id = newVersionID});
    
    ExecuteGitCommand("commit -m feature --allow-empty");
    ExecuteGitCommand("checkout -b develop");

    //Get release version from user
    TestConsole.Input.PushTextWithEnter("1.0.0");
    //Get next release version from user for jira
    TestConsole.Input.PushTextWithEnter("1.1.0");
    
    TestConsole.Input.PushTextWithEnter(_jiraUsername);
    TestConsole.Input.PushTextWithEnter(_jiraPassword);
    
    TestConsole.Input.PushTextWithEnter("n");
    

    var act = Program.Main(new[] { "Release-Version" });

    Assert.That(act, Is.EqualTo(0));
    
    var is110JiraVersion = JiraTestUtility.IsPartOfJiraVersions(_config.Jira.JiraProjectKey, "1.1.0", _testRestClient, out var version110);
    Assert.That(is110JiraVersion, Is.True);
    Assert.That(version110.released, Is.False);

    openIssue = JiraTestUtility.GetIssue(openIssue.ID, _testRestClient);
    Assert.That(openIssue.fields.FixVersions.First().ID , Is.EqualTo(version110.id));
    
    var is100JiraVersion = JiraTestUtility.IsPartOfJiraVersions(_config.Jira.JiraProjectKey, "1.0.0", _testRestClient, out var version100);
    Assert.That(is100JiraVersion, Is.True);
    Assert.That(version100.released, Is.True);
    
    closedIssue = JiraTestUtility.GetIssue(closedIssue.ID, _testRestClient);
    Assert.That(closedIssue.fields.FixVersions.First().ID, Is.EqualTo(version100.id));
    
    JiraTestUtility.DeleteVersionIfExistent(_config.Jira.JiraProjectKey, "1.0.0", _testRestClient);
    JiraTestUtility.DeleteVersionIfExistent(_config.Jira.JiraProjectKey, "1.1.0", _testRestClient);
    
    JiraTestUtility.DeleteIssue(openIssue.ID, _testRestClient);
    JiraTestUtility.DeleteIssue(closedIssue.ID, _testRestClient);
  }
  
   [Test]
  public void TestJiraRunThrough_WithManyDifferentVersionsWithCloseNames_ReleasesCorrectJiraVersions ()
  {
    
    JiraTestUtility.DeleteVersionIfExistent(_config.Jira.JiraProjectKey,  "1.0.0", _testRestClient);
    JiraTestUtility.DeleteVersionIfExistent(_config.Jira.JiraProjectKey,  "1.1.0", _testRestClient);
    
    var newVersionID = JiraTestUtility.CreateVersion(_testRestClient, "1.0.0", _config.Jira.JiraProjectKey);
    JiraTestUtility.CreateVersion(_testRestClient, "1.0.1", _config.Jira.JiraProjectKey);
    JiraTestUtility.CreateVersion(_testRestClient, "1.0.2", _config.Jira.JiraProjectKey);
    
    var openIssue = JiraTestUtility.AddTestIssueToVersion(
        "Some open issue", 
        false, 
        _config.Jira.JiraProjectKey, 
        _testRestClient, 
      new JiraProjectVersion(){name = "1.0.0", id = newVersionID});
    
    var closedIssue = JiraTestUtility.AddTestIssueToVersion(
      "Some closed issue",
      true,
      _config.Jira.JiraProjectKey,
      _testRestClient,
      new JiraProjectVersion() { name = "1.0.0", id = newVersionID});
    
    ExecuteGitCommand("commit -m feature --allow-empty");
    ExecuteGitCommand("checkout -b develop");

    //Get release version from user
    TestConsole.Input.PushTextWithEnter("1.0.0");
    //Get next release version from user for jira
    TestConsole.Input.PushTextWithEnter("1.1.0");
    
    TestConsole.Input.PushTextWithEnter(_jiraUsername);
    TestConsole.Input.PushTextWithEnter(_jiraPassword);
    
    TestConsole.Input.PushTextWithEnter("n");
    

    var act = Program.Main(new[] { "Release-Version" });

    Assert.That(act, Is.EqualTo(0));
    
    var is110JiraVersion = JiraTestUtility.IsPartOfJiraVersions(_config.Jira.JiraProjectKey, "1.1.0", _testRestClient, out var version110);
    Assert.That(is110JiraVersion, Is.True);
    Assert.That(version110.released, Is.False);

    openIssue = JiraTestUtility.GetIssue(openIssue.ID, _testRestClient);
    Assert.That(openIssue.fields.FixVersions.First().ID , Is.EqualTo(version110.id));
    
    var is100JiraVersion = JiraTestUtility.IsPartOfJiraVersions(_config.Jira.JiraProjectKey, "1.0.0", _testRestClient, out var version100);
    Assert.That(is100JiraVersion, Is.True);
    Assert.That(version100.released, Is.True);
    
    closedIssue = JiraTestUtility.GetIssue(closedIssue.ID, _testRestClient);
    Assert.That(closedIssue.fields.FixVersions.First().ID, Is.EqualTo(version100.id));
    
    JiraTestUtility.DeleteVersionIfExistent(_config.Jira.JiraProjectKey, "1.0.0", _testRestClient);
    JiraTestUtility.DeleteVersionIfExistent(_config.Jira.JiraProjectKey, "1.1.0", _testRestClient);
    
    JiraTestUtility.DeleteIssue(openIssue.ID, _testRestClient);
    JiraTestUtility.DeleteIssue(closedIssue.ID, _testRestClient);
  }
  
}