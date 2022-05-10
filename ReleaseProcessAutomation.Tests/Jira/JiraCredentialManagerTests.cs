using System.Net;
using AdysTech.CredentialManager;
using Moq;
using NUnit.Framework;
using ReleaseProcessAutomation.Jira;
using ReleaseProcessAutomation.ReadInput;
using Remotion.ReleaseProcessScript.Jira.ServiceFacadeImplementations;
using Spectre.Console.Testing;

namespace ReleaseProcessAutomation.Tests.Jira;

[TestFixture]
public class JiraCredentialManagerTests
{
  private Mock<IJira> _jiraMock;
  private Mock<IInputReader> _inputReaderMock;
  private TestConsole _console;

  private const string c_userName = "user";
  private const string c_password = "password";
  private const string c_target = "target";

  [SetUp]
  public void Setup ()
  {
    _jiraMock = new Mock<IJira>();
    _inputReaderMock = new Mock<IInputReader>();
    _console = new TestConsole();
  }

  [TearDown]
  public void TearDown ()
  {
    try
    {
      CredentialManager.RemoveCredentials(c_target);
    }
    catch
    {
      //ignore
    }
  }

  [Test]
  public void GetCredential_CredentialsInCredentialManager_ReturnsCredentials ()
  {
    var jiraCredentialManager = new JiraCredentialManager(_jiraMock.Object, _inputReaderMock.Object, _console);

    var cred = new NetworkCredential(c_userName, c_password);

    CredentialManager.SaveCredentials(c_target, cred);

    var output = jiraCredentialManager.GetCredential(c_target);

    Assert.That(output.Username, Is.EqualTo(c_userName));
    Assert.That(output.Password, Is.EqualTo(c_password));
  }

  [Test]
  public void GetCredential_NoCredentialsInCredentialManager_AsksUser ()
  {
    var jiraCredentialManager = new JiraCredentialManager(_jiraMock.Object, _inputReaderMock.Object, _console);

    _inputReaderMock.Setup(_ => _.ReadString(It.IsAny<string>())).Returns(c_userName);
    _inputReaderMock.Setup(_ => _.ReadHiddenString(It.IsAny<string>())).Returns(c_password);
    _inputReaderMock.Setup(_ => _.ReadConfirmation(It.IsAny<bool>())).Returns(false);

    jiraCredentialManager.GetCredential(c_target);

    _inputReaderMock.Verify(_ => _.ReadString(It.IsAny<string>()));
    _inputReaderMock.Verify(_ => _.ReadHiddenString(It.IsAny<string>()));
    _inputReaderMock.Verify(_ => _.ReadConfirmation(It.IsAny<bool>()));
  }

  [Test]
  public void GetCredential_AsksUser_SavesPassword ()
  {
    var jiraCredentialManager = new JiraCredentialManager(_jiraMock.Object, _inputReaderMock.Object, _console);

    _inputReaderMock.Setup(_ => _.ReadString(It.IsAny<string>())).Returns(c_userName);
    _inputReaderMock.Setup(_ => _.ReadHiddenString(It.IsAny<string>())).Returns(c_password);
    _inputReaderMock.Setup(_ => _.ReadConfirmation(It.IsAny<bool>())).Returns(true);

    jiraCredentialManager.GetCredential(c_target);

    var createdCredentials = CredentialManager.GetCredentials(c_target);

    Assert.That(createdCredentials.UserName, Is.EqualTo(c_userName));
    Assert.That(createdCredentials.Password, Is.EqualTo(c_password));
  }

  [Test]
  public void GetCredential_WithWrongCredentials_AsksUser ()
  {
    var networkCredential = new NetworkCredential("NotUserName", "NotPassword");
    CredentialManager.SaveCredentials(c_target, networkCredential);

    var jiraCredentialManager = new JiraCredentialManager(_jiraMock.Object, _inputReaderMock.Object, _console);

    _jiraMock.Setup(_ => _.CheckJiraCredentials(new Credentials { Username = "NotUserName", Password = "NotPassword" }))
        .Throws(new JiraException("error") { HttpStatusCode = HttpStatusCode.Unauthorized });

    _inputReaderMock.Setup(_ => _.ReadString(It.IsAny<string>())).Returns(c_userName);
    _inputReaderMock.Setup(_ => _.ReadHiddenString(It.IsAny<string>())).Returns(c_password);
    _inputReaderMock.Setup(_ => _.ReadConfirmation(It.IsAny<bool>())).Returns(false);

    jiraCredentialManager.GetCredential(c_target);

    _inputReaderMock.Verify(_ => _.ReadString(It.IsAny<string>()));
    _inputReaderMock.Verify(_ => _.ReadHiddenString(It.IsAny<string>()));
    _inputReaderMock.Verify(_ => _.ReadConfirmation(It.IsAny<bool>()));

    _jiraMock.Verify(_ => _.CheckJiraCredentials(new Credentials { Username = "NotUserName", Password = "NotPassword" }));
  }

  [Test]
  public void GetCredential_WithWrongCredentials_SavesNewCredentials ()
  {
    var networkCredential = new NetworkCredential("NotUserName", "NotPassword");
    CredentialManager.SaveCredentials(c_target, networkCredential);

    var jiraCredentialManager = new JiraCredentialManager(_jiraMock.Object, _inputReaderMock.Object, _console);

    _jiraMock.Setup(_ => _.CheckJiraCredentials(new Credentials { Username = "NotUserName", Password = "NotPassword" }))
        .Throws(new JiraException("error") { HttpStatusCode = HttpStatusCode.Unauthorized });

    _inputReaderMock.Setup(_ => _.ReadString(It.IsAny<string>())).Returns(c_userName);
    _inputReaderMock.Setup(_ => _.ReadHiddenString(It.IsAny<string>())).Returns(c_password);
    _inputReaderMock.Setup(_ => _.ReadConfirmation(It.IsAny<bool>())).Returns(true);

    jiraCredentialManager.GetCredential(c_target);

    _jiraMock.Verify(_ => _.CheckJiraCredentials(new Credentials { Username = "NotUserName", Password = "NotPassword" }));

    var output = CredentialManager.GetCredentials(c_target);

    Assert.That(output.UserName, Is.EqualTo(c_userName));
    Assert.That(output.Password, Is.EqualTo(c_password));
  }
}