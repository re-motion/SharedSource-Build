using System;
using System.Collections.Generic;

#pragma warning disable CS8618

public class TestProjectMetadata : ProjectMetadata
{
  public string TestConfiguration { get; init; }
  public string TestSetupBuildFile { get; init; }
  
  public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), TestConfiguration, TestSetupBuildFile);
  public override bool Equals (object? obj)
  {
    if (ReferenceEquals(null, obj))
      return false;
    if (ReferenceEquals(this, obj))
      return true;
    if (obj.GetType() != GetType())
      return false;
    return Equals((TestProjectMetadata) obj);
  }

  public override string ToString () => $"{base.ToString()}, {nameof(TestSetupBuildFile)}: {TestSetupBuildFile}";

  protected bool Equals (TestProjectMetadata other) =>
      base.Equals(other) && TestConfiguration == other.TestConfiguration && TestSetupBuildFile == other.TestSetupBuildFile;
}

#pragma warning restore CS8618