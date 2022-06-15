using System;
using System.Linq;
using System.Net;
using Moq;
using NUnit.Framework;
using ReleaseProcessAutomation.Jira.CredentialManagement;
using ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;
using ReleaseProcessAutomation.Jira.Utility;
using RestSharp;

namespace ReleaseProcessAutomation.IntegrationTests.Jira;

public abstract class JiraTestBase
{

  private const string c_usernameEnvironmentVariableName = "JiraUsername";
  private const string c_passwordEnvironmentVariableName = "JiraPassword";
  private const string c_jiraUrl = "https://re-motion.atlassian.net/";

  protected JiraProjectVersionService Service;
  protected Mock<IJiraRestClientProvider> RestClientMock;
  protected JiraRestClient RestClient;

  private string _jiraUsername;
  private string _jiraPassword;

  [SetUp]
  protected void Setup ()
  {
    
    _jiraUsername = Environment.GetEnvironmentVariable(c_usernameEnvironmentVariableName);
    _jiraPassword = Environment.GetEnvironmentVariable(c_passwordEnvironmentVariableName);

    if (string.IsNullOrEmpty(_jiraUsername))
      throw new InvalidOperationException($"Could not load credentials from environment variable '{c_usernameEnvironmentVariableName}'");

    if (string.IsNullOrEmpty(_jiraPassword))
      throw new InvalidOperationException($"Could not load credentials from environment variable '{c_passwordEnvironmentVariableName}'");

    RestClient = JiraRestClient.CreateWithBasicAuthentication(c_jiraUrl, new Credentials(_jiraUsername, _jiraPassword));

    RestClientMock = new Mock<IJiraRestClientProvider>();
    RestClientMock.Setup(_ => _.GetJiraRestClient()).Returns(RestClient);

  }
  
  protected JiraIssue AddTestIssueToVersion (string summaryOfIssue, bool closed, string jiraProjectKey, JiraRestClient restClient, params JiraProjectVersion[] toRelease)
  {
    return JiraTestUtility.AddTestIssueToVersion(summaryOfIssue, closed, jiraProjectKey, restClient, toRelease);
  }
  protected void DeleteVersionsIfExistent (string projectName, params string[] versionNames)
  {
    foreach (var versionName in versionNames)
    {
      JiraTestUtility.DeleteVersionIfExistent(projectName, versionName, RestClient);
    }
  }
}