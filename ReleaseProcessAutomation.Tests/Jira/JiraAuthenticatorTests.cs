using System;
using NUnit.Framework;
using ReleaseProcessAutomation.Jira.Authentication;
using ReleaseProcessAutomation.Jira.CredentialManagement;

namespace ReleaseProcessAutomation.Tests.Jira;

[TestFixture]
[Explicit]
public class JiraAuthenticatorTests
{
  private const string c_jiraUrl = "https://re-motion.atlassian.net/rest/api/2/";
  private const string c_jiraProjectKey = "SRCBLDTEST";
  private const string c_usernameEnvironmentVariableName = "JiraUsername";
  private const string c_passwordEnvironmentVariableName = "JiraPassword";

  [Test]
  public void CheckAuthentication_WithCorrectCredentials_DoesNotThrow ()
  {
    var jiraUsername = Environment.GetEnvironmentVariable(c_usernameEnvironmentVariableName);
    var jiraPassword = Environment.GetEnvironmentVariable(c_passwordEnvironmentVariableName);

    if (string.IsNullOrEmpty(jiraUsername))
      throw new InvalidOperationException($"Could not load credentials from environment variable '{c_usernameEnvironmentVariableName}'");
    
    if (string.IsNullOrEmpty(jiraPassword))
      throw new InvalidOperationException($"Could not load credentials from environment variable '{c_passwordEnvironmentVariableName}'");

    var testCredentials = new Credentials { Username = jiraUsername, Password = jiraPassword };

    var authenticator = new JiraAuthenticator();

    Assert.That(() => authenticator.CheckAuthentication(testCredentials, c_jiraProjectKey, c_jiraUrl), Throws.Nothing);
  }

  [Test]
  public void CheckAuthentication_WithIncorrectCredentials_DoesThrow ()
  {
    var testCredentials = new Credentials { Username = "DefinetlyNotAUsername", Password = "DefinetlyNotAPassword" };

    var authenticator = new JiraAuthenticator();

    Assert.That(() => authenticator.CheckAuthentication(testCredentials, c_jiraProjectKey, c_jiraUrl), Throws.Exception);
  }
}