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
using System.Linq;
using ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;
using ReleaseProcessAutomation.Jira.ServiceFacadeInterfaces;

namespace ReleaseProcessAutomation.Jira;

public class JiraVersionCreator
    : IJiraVersionCreator
{
  private readonly IJiraProjectVersionFinder _projectVersionFinder;
  private readonly IJiraProjectVersionService _jiraProjectVersionService;

  public JiraVersionCreator (IJiraProjectVersionFinder projectVersionFinder, IJiraProjectVersionService jiraProjectVersionService)
  {
    _projectVersionFinder = projectVersionFinder;
    _jiraProjectVersionService = jiraProjectVersionService;
  }

  public string CreateNewVersionWithVersionNumber (string jiraProject, string versionNumber)
  {
    if (string.IsNullOrEmpty(jiraProject))
      throw new InvalidOperationException("Jira project was not assigned.");

    var jiraProjectVersionRepairer = new JiraProjectVersionRepairer(_jiraProjectVersionService, _projectVersionFinder);

    var versions = _projectVersionFinder.FindVersions(jiraProject, "(?s).*").ToList();
    var jiraProjectVersion = versions.Where(x => x.name == versionNumber).DefaultIfEmpty().First();

    string createdVersionID;

    if (jiraProjectVersion != null)
    {
      if (jiraProjectVersion.released)
        throw new JiraException("The Version '" + versionNumber + "' got already released in Jira.");

      if (string.IsNullOrEmpty(jiraProjectVersion.id))
        throw new InvalidOperationException("Jira project id was not assigned.");
      createdVersionID = jiraProjectVersion.id;
    }
    else
    {
      if (string.IsNullOrEmpty(versionNumber))
        throw new InvalidOperationException("Version number was not assigned.");
      createdVersionID = _jiraProjectVersionService.CreateVersion(jiraProject, versionNumber, null);

      jiraProjectVersionRepairer.RepairVersionPosition(createdVersionID);
    }

    return createdVersionID;
  }
}