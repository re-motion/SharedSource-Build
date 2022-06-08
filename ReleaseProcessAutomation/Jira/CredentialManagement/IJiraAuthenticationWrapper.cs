using System;

namespace ReleaseProcessAutomation.Jira.CredentialManagement;

public interface IJiraAuthenticationWrapper
{
  public void CheckAuthentication (Credentials credential,string projectKey, string jiraURL);
}