using ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;

namespace ReleaseProcessAutomation.Jira;

public interface IJiraVersionCreator
{
  string CreateNewVersionWithVersionNumber (string jiraProject, string versionNumber, JiraRestClient jiraRestClient);
}