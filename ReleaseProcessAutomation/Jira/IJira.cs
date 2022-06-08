using ReleaseProcessAutomation.Jira.CredentialManagement;
using ReleaseProcessAutomation.Jira.ServiceFacadeInterfaces;

namespace ReleaseProcessAutomation.Jira;

public interface IJira
{

  IJiraVersionCreator VersionCreator
  {
    get;
  }

  IJiraVersionReleaser VersionReleaser
  {
    get;
  }

  IJiraAuthenticationWrapper AuthenticationWrapper
  {
    get;
  }
}