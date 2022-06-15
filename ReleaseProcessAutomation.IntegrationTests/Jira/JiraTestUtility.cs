using System.Linq;
using System.Net;
using ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;
using RestSharp;

namespace ReleaseProcessAutomation.IntegrationTests.Jira;

public class JiraTestUtility
{
  public static JiraIssue AddTestIssueToVersion (string summaryOfIssue, bool closed, string jiraProjectKey, JiraRestClient restClient, params JiraProjectVersion[] toRelease)
  {
    // Create new issue
    var resource = "issue";
    var request = new RestRequest { Method = Method.POST, RequestFormat = DataFormat.Json, Resource = resource };

    var body = new
               {
                   fields = new
                            {
                                project = new { key = jiraProjectKey }, issuetype = new { name = "Task" }, summary = summaryOfIssue,
                                description = "testDescription", fixVersions = toRelease.Select(v => new { v.id })
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

  public static void CloseIssue (string issueID, JiraRestClient restClient)
  {
    var resource = "issue/" + issueID + "/transitions";
    var request = new RestRequest { Method = Method.POST, RequestFormat = DataFormat.Json, Resource = resource };

    var body = new { transition = new { id = 2 } };
    request.AddBody(body);

    restClient.DoRequest(request, HttpStatusCode.NoContent);
  }

  public static void DeleteVersionsIfExistent (string projectName, JiraProjectVersionService service, params string[] versionNames)
  {
    foreach (var versionName in versionNames)
      try
      {
        service.DeleteVersion(projectName, versionName);
      }
      catch
      {
        // ignore
      }
  }
}