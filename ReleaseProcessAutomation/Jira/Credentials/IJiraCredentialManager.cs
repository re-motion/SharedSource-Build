using System;

namespace ReleaseProcessAutomation.Jira;

public interface IJiraCredentialManager
{
  Credentials GetCredential (string target);
}