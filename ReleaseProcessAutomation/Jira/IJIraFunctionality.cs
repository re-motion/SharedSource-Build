using System;
using ReleaseProcessAutomation.SemanticVersioning;

namespace ReleaseProcessAutomation.Jira;

public interface IJIraFunctionality
{
  void CreateAndReleaseJiraVersion (SemanticVersion currentVersion, SemanticVersion nextVersion, bool squashUnreleased = false);

}