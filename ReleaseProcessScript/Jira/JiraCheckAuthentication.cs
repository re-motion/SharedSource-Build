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
using Remotion.ReleaseProcessScript.Jira.ServiceFacadeImplementations;
using Remotion.ReleaseProcessScript.Jira.ServiceFacadeInterfaces;

namespace Remotion.ReleaseProcessScript.Jira
{
  public class JiraCheckAuthentication : JiraTask
  {
    public string JiraProject { get; set; }

    public void Execute ()
    {
      JiraRestClient restClient = new JiraRestClient (JiraUrl, Authenticator);

      IJiraProjectVersionFinder finder = new JiraProjectVersionFinder (restClient);

      //Just call any function to send a Request and test Authentication Details
      //Throws JiraException with HttpStatusCode.Forbidden if Authentication fails
      finder.FindUnreleasedVersions (JiraProject, "(?s).*");
    }
  }
}