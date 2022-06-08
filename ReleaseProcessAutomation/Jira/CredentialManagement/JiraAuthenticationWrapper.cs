using System;
using System.Collections.Generic;
using System.Net;
using ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;
using RestSharp;
using RestSharp.Authenticators;

namespace ReleaseProcessAutomation.Jira.CredentialManagement;

public class JiraAuthenticationWrapper
    : IJiraAuthenticationWrapper
{
    public void CheckAuthentication (Credentials credentials, string projectKey, string jiraURL)
    {
        var jiraRestClient = new JiraRestClient(jiraURL, new HttpBasicAuthenticator(credentials.Username, credentials.Password));
        var resource = $"project/{projectKey}/versions";
        var request = jiraRestClient.CreateRestRequest (resource, Method.GET);

        var response = jiraRestClient.DoRequest<List<JiraProjectVersion>> (request, HttpStatusCode.OK);
    }
}