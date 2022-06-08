using System;
using ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;
using RestSharp.Authenticators;

namespace ReleaseProcessAutomation.Jira.CredentialManagement;

public class JiraAuthenticationWrapper
    : IJiraAuthenticationWrapper
{
    public void CheckAuthentication (Credentials credentials, string projectKey, string jiraURL)
    {
        var jiraRestClient = new JiraRestClient(jiraURL, new HttpBasicAuthenticator(credentials.Username, credentials.Password));
        var finder = new JiraProjectVersionFinder(jiraRestClient);
        finder.FindUnreleasedVersions(projectKey, "(?s).*");
    }
}