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

using System.Text.RegularExpressions;
using NUnit.Framework;

namespace BuildScript.Nuke.IntegrationTests.Components;

[TestFixture]
public class RestoreTest : IntegrationTestBase
{
  [Test]
  public void RestoreTarget ()
  {
    var result = RunBuildCmd("Restore");

    var sectionOutput = result.GetSectionOutput("Restore");
    Assert.That(sectionOutput, Is.Not.Null);

    foreach (var project in TestSolutionModel.Projects)
    {
      Assert.That(
          sectionOutput, Does.Contain($"Restored $SOLUTIONDIR$\\{project.RelativePath}"));
    }
  }
}