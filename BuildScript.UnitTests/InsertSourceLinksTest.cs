using System;
using System.IO;
using NUnit.Framework;
using Remotion.BuildScript.BuildTasks;

namespace Remotion.BuildScript.UnitTests
{
  [TestFixture]
  public class InsertSourceLinksTest
  {
    private class TestableInsertSourceLinks : InsertSourceLinks
    {
      public new string GenerateFullPathToTool ()
      {
        return base.GenerateFullPathToTool();
      }
    }

    [Test]
    public void GenerateFullPathToTool ()
    {
      var task = new TestableInsertSourceLinks();

      var path = task.GenerateFullPathToTool();

      Assert.That (path, Does.EndWith (task.ToolExe));
      Assert.That (File.Exists (path), Is.True, string.Format ("File '{0}' not found.", path));
    }
  }
}