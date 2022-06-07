// Copyright (c) rubicon IT GmbH, www.rubicon.eu
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
using System.IO;
using JetBrains.Annotations;
using ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;
using ReleaseProcessAutomation.Jira.ServiceFacadeInterfaces;

namespace ReleaseProcessAutomation.Jira;

public class JiraVersionReleaser
{
  private readonly JiraRestClient _jiraRestClient;

  public JiraVersionReleaser (JiraRestClient jiraRestClient)
  {
    _jiraRestClient = jiraRestClient;
  }

  public void ReleaseVersion (string jiraUrl, string versionID, string nextVersionID, bool sortReleasedVersion)
  {
    IJiraProjectVersionService service = new JiraProjectVersionService (_jiraRestClient);
    var jiraProjectVersionFinder = new JiraProjectVersionFinder (_jiraRestClient);
    var jiraProjectVersionRepairer = new JiraProjectVersionRepairer (service, jiraProjectVersionFinder);

    service.ReleaseVersion (versionID, nextVersionID);

    if (sortReleasedVersion)
    {
      jiraProjectVersionRepairer.RepairVersionPosition (versionID);
    }
  }
  
  public void ReleaseVersionAndSquashUnreleased (string jiraUrl, string jiraProjectKey, string versionID, string nextVersionID)
  {
    IJiraProjectVersionService service = new JiraProjectVersionService(_jiraRestClient);
    service.ReleaseVersionAndSquashUnreleased(versionID, nextVersionID, jiraProjectKey);
  }
}