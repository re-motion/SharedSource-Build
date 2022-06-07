using ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;

namespace ReleaseProcessAutomation.Jira;

public interface IJiraAuthenticationWrapper
{
  public void CheckAuthentication (JiraRestClient jiraRestClient, string projectKey);
}