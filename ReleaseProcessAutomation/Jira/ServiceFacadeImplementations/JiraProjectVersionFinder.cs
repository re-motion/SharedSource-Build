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
using System.Text.RegularExpressions;
using ReleaseProcessAutomation.Jira.ServiceFacadeInterfaces;
using RestSharp;

namespace ReleaseProcessAutomation.Jira.ServiceFacadeImplementations
{
  public class JiraProjectVersionFinder : IJiraProjectVersionFinder
  {
    private readonly JiraRestClient _jiraClient;

    public JiraProjectVersionFinder(JiraRestClient restClient)
    {
      _jiraClient = restClient;
    }

    public IEnumerable<JiraProjectVersion> FindVersions (string projectKey, string versionPattern)
    {
      var versions = GetVersions (projectKey);
      return versions.Where (v =>
      {
        if (string.IsNullOrEmpty(v.name))
        {
          throw new InvalidOperationException($"Could not get name from jira project version with id '{v.id}', maybe it was not initialized?");
        }
        return Regex.IsMatch(v.name!, versionPattern);
      });
    }
    
    public IEnumerable<JiraProjectVersion> FindUnreleasedVersions (string projectKey, string versionPattern)
    {
      return FindVersions (projectKey, versionPattern).Where (v => v.released != true);
    }

    public IEnumerable<JiraProjectVersion> GetVersions (string projectKey)
    {
      var resource = $"project/{projectKey}/versions";
      var request = _jiraClient.CreateRestRequest (resource, Method.GET);

      var response = _jiraClient.DoRequest<List<JiraProjectVersion>> (request, HttpStatusCode.OK);
      return response.Data;
    }

    public JiraProjectVersion GetVersionById (string versionId)
    {
      var resource = $"version/{versionId}";
      var request = _jiraClient.CreateRestRequest (resource, Method.GET);

      var response = _jiraClient.DoRequest<JiraProjectVersion> (request, HttpStatusCode.OK);
      return response.Data;
    }
  }
}