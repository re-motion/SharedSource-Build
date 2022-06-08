using System;
using ReleaseProcessAutomation.Jira.CredentialManagement;

namespace ReleaseProcessAutomation.Jira.Authentication;

public interface IJiraAuthenticator
{
  public void CheckAuthentication (Credentials credential, string projectKey, string jiraURL);
}