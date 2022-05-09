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

namespace Remotion.ReleaseProcessScript.Jira.ServiceFacadeImplementations
{
  public class JiraIssue
  {
    public string? id { get; set; }
    public string? summary { get; set; }
    public List<string>? fixVersions { get; set; }

    public string? issuetype { get; set; }
    public string? project { get; set; }

    public string? key { get; set; }
  }

  public class JiraNonClosedIssues
  {
    public List<JiraToBeMovedIssue>? issues { get; set; }
  }

  public class JiraToBeMovedIssue
  {
    public string? id { get; set; }
    
    public string? key { get; set; }

    public JiraNonClosedIssueFields? fields { get; set; }
  }

  public class JiraNonClosedIssueFields
  {
    public List<JiraVersion>? fixVersions { get; set; }
  }

  public class JiraVersion
  {
    public string? id { get; set; }
  }
}
