using ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;

namespace ReleaseProcessAutomation.Jira;

public class JiraAuthenticationWrapper
    : IJiraAuthenticationWrapper
{
    public void CheckAuthentication (JiraRestClient jiraRestClient, string projectKey)
    {
        var finder = new JiraProjectVersionFinder(jiraRestClient);
        finder.FindUnreleasedVersions(projectKey, "(?s).*");
    }
}