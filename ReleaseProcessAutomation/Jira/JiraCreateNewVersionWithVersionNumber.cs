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
using System.Linq;
using ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;
using ReleaseProcessAutomation.Jira.ServiceFacadeInterfaces;

namespace ReleaseProcessAutomation.Jira
{
  public class JiraCreateNewVersionWithVersionNumber : JiraTask
  {
    public string? JiraProject { get; set; }

    public string? VersionNumber { get; set; }

    public string? CreatedVersionID { get; private set; }
    

    public void Execute ()
    {
      if (string.IsNullOrEmpty(JiraUrl))
      {
        throw new InvalidOperationException("Jira url was not assigned.");
      }
      if (string.IsNullOrEmpty(JiraProject))
      {
        throw new InvalidOperationException("Jira project was not assigned.");
      }
      
      JiraRestClient restClient = new JiraRestClient (JiraUrl, Authenticator);
      IJiraProjectVersionService service = new JiraProjectVersionService (restClient);
      IJiraProjectVersionFinder finder = new JiraProjectVersionFinder (restClient);
      var _jiraProjectVersionRepairer = new JiraProjectVersionRepairer (service, finder);

      var versions = finder.FindVersions (JiraProject, "(?s).*").ToList();
      var jiraProject = versions.Where (x => x.name == VersionNumber).DefaultIfEmpty().First();

      if (jiraProject != null)
      {
        if (jiraProject.released != null)
        {
          if (jiraProject.released.Value)
            throw new JiraException ("The Version '" + VersionNumber + "' got already released in Jira.");
        }

        CreatedVersionID = jiraProject.id;
      }
      else
      {
        if (string.IsNullOrEmpty(VersionNumber))
        {
          throw new InvalidOperationException("Version number was not assigned.");
        }
        CreatedVersionID = service.CreateVersion (JiraProject, VersionNumber, null);

        _jiraProjectVersionRepairer.RepairVersionPosition (CreatedVersionID);
      }
    }
  }
}