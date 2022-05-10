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
using System.IO;
using ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;
using ReleaseProcessAutomation.Jira.ServiceFacadeInterfaces;

namespace ReleaseProcessAutomation.Jira
{
  public class JiraReleaseVersionAndSquashUnreleased : JiraTask
  {
    public string? VersionID { get; init; }

    public string? NextVersionID { get; init; }

    public string? ProjectKey { get; init; }

    public void Execute ()
    {
      if (string.IsNullOrEmpty(JiraUrl))
      {
        throw new InvalidOperationException("Jira url was not assigned.");
      }
      if (string.IsNullOrEmpty(VersionID))
      {
        throw new InvalidOperationException("Version id was not assigned.");
      }
      if (string.IsNullOrEmpty(NextVersionID))
      {
        throw new InvalidOperationException("Next version id was not assigned");
      }

      if (string.IsNullOrEmpty(ProjectKey))
      {
        throw new InvalidDataException("Project key was not assigned");
      }
      JiraRestClient restClient = new JiraRestClient (JiraUrl, Authenticator);
      IJiraProjectVersionService service = new JiraProjectVersionService (restClient);
      service.ReleaseVersionAndSquashUnreleased (VersionID, NextVersionID, ProjectKey);
    }
  }
}