using System;
using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;
using ReleaseProcessAutomation.Jira.ServiceFacadeInterfaces;

namespace ReleaseProcessAutomation.Tests.Jira
{
  public class JiraProjectVersionRepairerTest
  {
    const string projectId = "exampleProjectId";

    [Test]
    public void TestAlreadySortedVersion ()
    {
      const string versionId = "exampleId";

      var createdVersion = CreateJiraProjectVersion ("1.0.1", versionId);


      var jiraProjectVersions = new List<JiraProjectVersion>();
      jiraProjectVersions.Add (CreateJiraProjectVersion ("1.0.0"));

      var jiraProjectVersionService = MockRepository.GenerateMock<IJiraProjectVersionService>();
      var jiraProjectVersionFinder = MockRepository.GenerateMock<IJiraProjectVersionFinder>();

      jiraProjectVersionFinder.Stub (x => x.GetVersionById (versionId)).Return (createdVersion);
      jiraProjectVersionFinder.Stub (x => x.FindVersions (projectId, "(?s).*")).Return (jiraProjectVersions);

      var jiraProjectVersionRepairer = new JiraProjectVersionRepairer (jiraProjectVersionService, jiraProjectVersionFinder);

      jiraProjectVersionRepairer.RepairVersionPosition (versionId);

      jiraProjectVersionService.AssertWasNotCalled (x => x.MoveVersionByPosition (Arg<string>.Is.Anything, Arg<string>.Is.Anything));
      jiraProjectVersionService.AssertWasNotCalled (x => x.MoveVersion (Arg<string>.Is.Anything, Arg<string>.Is.Anything));
    }

    [Test]
    public void TestMoveVersionToFirstPosition ()
    {
      const string versionId = "exampleId";

      var createdVersion = CreateJiraProjectVersion ("1.0.0", versionId);

      var jiraProjectVersions = new List<JiraProjectVersion>();
      jiraProjectVersions.Add (CreateJiraProjectVersion ("1.0.1"));

      var jiraProjectVersionService = MockRepository.GenerateMock<IJiraProjectVersionService>();
      var jiraProjectVersionFinder = MockRepository.GenerateMock<IJiraProjectVersionFinder>();

      jiraProjectVersionFinder.Stub (x => x.GetVersionById (versionId)).Return (createdVersion);
      jiraProjectVersionFinder.Stub (x => x.FindVersions (projectId, "(?s).*")).Return (jiraProjectVersions);

      var jiraProjectVersionRepairer = new JiraProjectVersionRepairer (jiraProjectVersionService, jiraProjectVersionFinder);

      jiraProjectVersionRepairer.RepairVersionPosition (versionId);

      jiraProjectVersionService.AssertWasCalled (x => x.MoveVersionByPosition (versionId, "First"));
    }

    [Test]
    public void TestMoveVersionToCorrectPosition ()
    {
      const string versionId = "exampleId";
      const string beforeUrl = "someBeforeUrl";
      var createdVersion = CreateJiraProjectVersion ("1.0.1", versionId);

      var jiraProjectVersions = new List<JiraProjectVersion>();
      var beforeCorrectPositionVersion = CreateJiraProjectVersion ("1.0.0");
      beforeCorrectPositionVersion.self = beforeUrl;
      jiraProjectVersions.Add (beforeCorrectPositionVersion);
      jiraProjectVersions.Add (CreateJiraProjectVersion ("1.0.2"));

      var jiraProjectVersionService = MockRepository.GenerateMock<IJiraProjectVersionService>();
      var jiraProjectVersionFinder = MockRepository.GenerateMock<IJiraProjectVersionFinder>();

      jiraProjectVersionFinder.Stub (x => x.GetVersionById (versionId)).Return (createdVersion);
      jiraProjectVersionFinder.Stub (x => x.FindVersions (projectId, "(?s).*")).Return (jiraProjectVersions);

      var jiraProjectVersionRepairer = new JiraProjectVersionRepairer (jiraProjectVersionService, jiraProjectVersionFinder);

      jiraProjectVersionRepairer.RepairVersionPosition (versionId);

      jiraProjectVersionService.AssertWasCalled (x => x.MoveVersion (versionId, beforeUrl));
    }

    [Test]
    public void TestMoveVersionToCorrectPositionWithReleasedVersionInBetween ()
    {
      const string versionId = "exampleId";
      const string beforeUrl = "someBeforeUrl";
      var createdVersion = CreateJiraProjectVersion ("1.0.2", versionId, released: false);

      var jiraProjectVersions = new List<JiraProjectVersion>
                                {
                                    CreateJiraProjectVersion ("1.0.0", released: false),
                                    CreateJiraProjectVersion ("1.0.1", released: true, self: beforeUrl),
                                    CreateJiraProjectVersion ("1.0.3", released: false)
                                };

      var jiraProjectVersionService = MockRepository.GenerateMock<IJiraProjectVersionService>();
      var jiraProjectVersionFinder = MockRepository.GenerateMock<IJiraProjectVersionFinder>();
      jiraProjectVersionFinder.Stub (x => x.GetVersionById (versionId)).Return (createdVersion);
      jiraProjectVersionFinder.Stub (x => x.FindVersions (projectId, "(?s).*")).Return (jiraProjectVersions);
      var jiraProjectVersionRepairer = new JiraProjectVersionRepairer (jiraProjectVersionService, jiraProjectVersionFinder);

      jiraProjectVersionRepairer.RepairVersionPosition (versionId);

      jiraProjectVersionService.AssertWasCalled (x => x.MoveVersion (versionId, beforeUrl));
    }

    private JiraProjectVersion CreateJiraProjectVersion (string name, string id = "", bool released = false, string self = "")
    {
      return new JiraProjectVersion() { name = name, projectId = projectId, id = id, released = released, self = self };
    }
  }
}
