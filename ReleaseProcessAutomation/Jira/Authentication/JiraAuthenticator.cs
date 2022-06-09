using System;
using System.Collections.Generic;
using System.Net;
using ReleaseProcessAutomation.Jira.CredentialManagement;
using ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;
using RestSharp;
using RestSharp.Authenticators;

namespace ReleaseProcessAutomation.Jira.Authentication;

public class JiraAuthenticator
    : IJiraAuthenticator
{
  public void CheckAuthentication (Credentials credentials, string projectKey, string jiraURL)
  {
    //var jiraRestClient = new JiraRestClient(jiraURL, new HttpBasicAuthenticator(credentials.Username, credentials.Password));
    var jiraRestClient = JiraRestClient.CreateWithBasicAuthentication(jiraURL, credentials);
    var resource = $"project/{projectKey}/versions";
    var request = jiraRestClient.CreateRestRequest(resource, Method.GET);

    jiraRestClient.DoRequest<List<JiraProjectVersion>>(request, HttpStatusCode.OK);
  }
}