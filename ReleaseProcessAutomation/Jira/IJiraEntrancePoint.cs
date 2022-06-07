using System;
using ReleaseProcessAutomation.SemanticVersioning;

namespace ReleaseProcessAutomation.Jira;

public interface IJiraEntrancePoint
{
  void CreateAndReleaseJiraVersion (SemanticVersion currentVersion, SemanticVersion nextVersion, bool squashUnreleased = false);

}