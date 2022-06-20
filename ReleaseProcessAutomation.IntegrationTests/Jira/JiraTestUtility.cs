using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;
using RestSharp;

namespace ReleaseProcessAutomation.IntegrationTests.Jira;

public class JiraTestUtility
{
  public static JiraIssue AddTestIssueToVersion (
      string summaryOfIssue,
      bool closed,
      string jiraProjectKey,
      JiraRestClient restClient,
      params JiraProjectVersion[] toRelease)
  {
    // Create new issue
    var resource = "issue";
    var request = restClient.CreateRestRequest(resource, Method.POST);

    var body = new
               {
                   fields = new
                            {
                                project = new { key = jiraProjectKey }, issuetype = new { name = "Task" }, summary = summaryOfIssue,
                                description = "testDescription", fixVersions = toRelease.Select(v => new { v.id }), components = new []{ new {name = "APMTestComponent"}}
                            }
               };
    request.AddBody(body);

    var response = restClient.DoRequest<JiraIssue>(request, HttpStatusCode.Created);

    // Close issue if necessary
    if (closed)
    {
      var issue = response.Data;
      CloseIssue(issue.ID, restClient);
    }

    return response.Data;
  }

  public static string CreateVersion (JiraRestClient restClient, string versionName, string projectKey)
  {
    var request = restClient.CreateRestRequest("version", Method.POST);

    var projectVersion = new JiraProjectVersion { name = versionName, project = projectKey, releaseDate = null };
    request.AddBody(projectVersion);

    var newVersion = restClient.DoRequest<JiraProjectVersion>(request, HttpStatusCode.Created);

    return newVersion.Data.id;
  }

  public static void CloseIssue (string issueID, JiraRestClient restClient)
  {
    var resource = "issue/" + issueID + "/transitions";
    var request = restClient.CreateRestRequest(resource, Method.POST);

    var body = new { transition = new { id = 2 } };
    request.AddBody(body);

    restClient.DoRequest(request, HttpStatusCode.NoContent);
  }

  public static void DeleteVersionIfExistent (string projectName, string versionName, JiraRestClient restClient)
  {
    var versions = GetAllJiraVersions(projectName, restClient);
    var versionToDelete = versions.SingleOrDefault(v => v.name == versionName);
    if (versionToDelete == null)
      return;
    var resource = $"version/{versionToDelete.id}";
    
    var request = restClient.CreateRestRequest(resource, Method.DELETE);
    restClient.DoRequest(request, HttpStatusCode.NoContent);
  }

  public static IEnumerable<JiraProjectVersion> GetAllJiraVersions (string projectKey, JiraRestClient restClient)
  {
    var request = restClient.CreateRestRequest($"project/{projectKey}/versions", Method.GET);
    var response = restClient.DoRequest<List<JiraProjectVersion>>(request, HttpStatusCode.OK);
    return response.Data;
  }

  public static bool IsPartOfJiraVersions (string projectKey, string versionName, JiraRestClient restClient, out JiraProjectVersion foundVersion)
  {
    var allVersions = GetAllJiraVersions(projectKey, restClient);
    try
    {
      foundVersion = allVersions.First(v => v.name.Equals(versionName));
    }
    catch
    {
      foundVersion = null;
      return false;
    }

    return true;
  }


  public static JiraIssue GetIssue (string issueID, JiraRestClient restClient)
  {
    var request = restClient.CreateRestRequest($"/issue/{issueID}", Method.GET);
    var response = restClient.DoRequest<JiraIssue>(request, HttpStatusCode.OK);
    return response.Data;
  }

  public static void DeleteIssue (string issueID, JiraRestClient restClient)
  {
    var request = restClient.CreateRestRequest($"/issue/{issueID}", Method.DELETE);
    restClient.DoRequest(request, HttpStatusCode.NoContent);
  }
}