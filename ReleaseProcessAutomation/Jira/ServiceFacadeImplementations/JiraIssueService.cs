﻿// Copyright (c) rubicon IT GmbH, www.rubicon.eu
//
// See the NOTICE file distributed with this work for additional information
// regarding copyright ownership.  rubicon licenses this file to you under 
// the Apache License, Version 2.0 (the "License"); you may not use this 
// file except in compliance with the License.  You may obtain a copy of the 
// License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software 
// distributed under the License is distributed on an "AS IS" BASIS, WITHOUT 
// WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.  See the 
// License for the specific language governing permissions and limitations
// under the License.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using RestSharp;

namespace ReleaseProcessAutomation.Jira.ServiceFacadeImplementations
{
  public class JiraIssueService
  {
    private readonly JiraRestClient jiraClient;

    public JiraIssueService (JiraRestClient restClient)
    {
      jiraClient = restClient;
    }

    public void MoveIssuesToVersion (IEnumerable<JiraToBeMovedIssue> issues, string oldVersionId, string newVersionId)
    {
      foreach (var issue in issues)
      {
        var resource = $"issue/{issue.ID}";
        var request = jiraClient.CreateRestRequest (resource, Method.PUT);

        if (issue.Fields == null)
        {
          throw new InvalidOperationException($"Could not get fields from issue '{issue}");
        }
        var newFixVersions = issue.Fields!.FixVersions;
        newFixVersions!.RemoveAll (v =>
        {
          if (string.IsNullOrEmpty(v.ID))
          {
            throw new InvalidOperationException($"Could not get id from jira version");
          }
          return v.ID == oldVersionId;
        });
        newFixVersions.Add (new JiraVersion { ID = newVersionId });

        var body = new { fields = new { fixVersions = newFixVersions.Select (v => new { id = v.ID }) } };
        request.AddBody (body);

        jiraClient.DoRequest<JiraIssue> (request, HttpStatusCode.NoContent);
      }
    }

    public IEnumerable<JiraToBeMovedIssue> FindAllNonClosedIssues (string versionId)
    {
      var jql = $"fixVersion={versionId} and resolution = \"unresolved\"";
      var resource = $"search?jql={jql}&fields=id,fixVersions";
      var request = jiraClient.CreateRestRequest (resource, Method.GET);

      var response = jiraClient.DoRequest<JiraNonClosedIssues> (request, HttpStatusCode.OK);
      return response.Data.Issues ?? throw new InvalidOperationException($"Could not get non closed issue data from jira with request '{resource}'");
    }

    public IEnumerable<JiraToBeMovedIssue> FindAllClosedIssues (string versionId)
    {
      var jql = $"fixVersion={versionId} and resolution != \"unresolved\"";
      var resource = $"search?jql={jql}&fields=id,fixVersions";
      var request = jiraClient.CreateRestRequest (resource, Method.GET);

      var response = jiraClient.DoRequest<JiraNonClosedIssues> (request, HttpStatusCode.OK);
      return response.Data.Issues ?? throw new InvalidOperationException($"Could not get closed issue data from jira with request '{resource}'");

    }
  }
}
