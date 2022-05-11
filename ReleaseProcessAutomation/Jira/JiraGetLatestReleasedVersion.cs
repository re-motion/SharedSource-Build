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
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;
using ReleaseProcessAutomation.Jira.ServiceFacadeInterfaces;

namespace ReleaseProcessAutomation.Jira
{
  public class JiraGetLatestReleasedVersion : JiraTask
  {
    
    public JiraGetLatestReleasedVersion ([CanBeNull] string? jiraUsername, [CanBeNull] string? jiraPassword)
        : base(jiraUsername, jiraPassword) { }
    public IReadOnlyList<JiraProjectVersion> Execute (string jiraUrl, string jiraProjectKey, string versionPattern)
    {
      JiraRestClient restClient = new JiraRestClient (jiraUrl, Authenticator);
      IJiraProjectVersionFinder finder = new JiraProjectVersionFinder (restClient);
      var versions = finder.FindVersions (jiraProjectKey, versionPattern).ToArray();

      var unreleasedVersions = versions.Where (v => v.released != true);
      var releasedVersions = versions.Where (v => v.released == true);

      var versionsList = new List<JiraProjectVersion>
                         {
                             releasedVersions.Last(),
                             unreleasedVersions.First()
                         };

      return versionsList;
    }

  }
}
