using System;

namespace Remotion.ReleaseProcessScript.Jira.SemanticVersioning
{
  public class SemanticVersion : IComparable<SemanticVersion>
  {
    public int Major { get; set; }
    public int Minor { get; set; }
    public int Patch { get; set; }
    public PreReleaseStage? Pre { get; set; }
    public int? PreReleaseCounter { get; set; }

    public int CompareTo(SemanticVersion other)
    {
      if (other == null) return 1;

      if (Major > other.Major) return 1;
      if (Major < other.Major) return -1;

      if (Minor > other.Minor) return 1;
      if (Minor < other.Minor) return -1;

      if (Patch > other.Patch) return 1;
      if (Patch < other.Patch) return -1;

      if (Pre != null && other.Pre == null) return -1;
      if (Pre == null && other.Pre != null) return 1;

      if (Pre > other.Pre) return 1;
      if (Pre < other.Pre) return -1;

      if (PreReleaseCounter > other.PreReleaseCounter) return 1;
      if (PreReleaseCounter < other.PreReleaseCounter) return -1;

      return 0;
    }

    public override bool Equals(object obj)
    {
      SemanticVersion other = obj as SemanticVersion;

      if (other == null) return false;

      return Major == other.Major &&
             Minor == other.Minor &&
             Patch == other.Patch &&
             Pre == other.Pre &&
             PreReleaseCounter == other.PreReleaseCounter;
    }

    public override int GetHashCode()
    {
      unchecked
      {
        var hashCode = Major;
        hashCode = (hashCode*397) ^ Minor;
        hashCode = (hashCode*397) ^ Patch;
        hashCode = (hashCode*397) ^ Pre.GetHashCode();
        hashCode = (hashCode*397) ^ PreReleaseCounter.GetHashCode();
        return hashCode;
      }
    }

    public override string ToString()
    {
      if (Pre == null)
        return string.Format("{0}.{1}.{2}", Major, Minor, Patch);

      return string.Format("{0}.{1}.{2}-{3}.{4}", Major, Minor, Patch, Pre, PreReleaseCounter);
    }
  }
}