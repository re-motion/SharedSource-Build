using System.Xml.Serialization;

namespace ReleaseProcessAutomation.Tests.Jira;

[XmlRoot("credentials")]
public class JiraTestConfig
{
  [XmlElement("username")]
  public string Username { get; set; }
  
  [XmlElement("password")]
  public string Password { get; set; }
}