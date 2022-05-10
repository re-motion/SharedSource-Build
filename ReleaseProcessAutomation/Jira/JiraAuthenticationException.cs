using System;

namespace ReleaseProcessAutomation.Jira;

public class JiraAuthenticationException : Exception
{
  public JiraAuthenticationException (string message) : base(message)
  {
  }
}