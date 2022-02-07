using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Nuke.Common;

/// <summary>
///   Creates all needed versions for the build from the base version
/// </summary>
public class VersionHelper
{
  //language=regex
  private const string c_versionNumberPattern = @"(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)";

  //language=regex
  private const string c_versionPreReleaseSuffixPattern =
      @"(?:\-(?<prereleaseSuffix>(?<prereleaseName>[a-zA-Z]+)\.(?<prereleaseCounter>[1-9][0-9]{0,2})(?<prereleaseRemainder>(?:\.[a-zA-Z0-9-]+)*)))?";

  private const string c_versionPattern = c_versionNumberPattern + c_versionPreReleaseSuffixPattern;

  private string _major;
  private string _minor;
  private string _patch;
  private string _preReleaseCounter;
  private string _preReleaseName;

  /// <remarks>
  ///   e.g. 3.1.4-alpha.30
  /// </remarks>
  public string Version { get; }

  /// <remarks>
  ///   e.g. 3.1.0.0
  /// </remarks>
  public string AssemblyVersion { get; }

  /// <remarks>
  ///   e.g. 3.1.4.01030
  /// </remarks>
  public string AssemblyFileVersion { get; }

  /// <remarks>
  ///   e.g. 3.1.4-alpha.30
  /// </remarks>
  public string AssemblyNuGetVersion => Version;

  /// <remarks>
  ///   e.g. 3.1.0.0
  /// </remarks>
  public string DependDBProjectVersion => AssemblyVersion;

  /// <remarks>
  ///   e.g. 3.1.4
  /// </remarks>
  public string VersionWithoutPreRelease { get; }

  public VersionHelper (string version)
  {
    Version = version;
    var versionMatch = Regex.Match(Version, c_versionPattern);
    if (!versionMatch.Success)
      Assert.Fail($"Version semantic doesn't match: {Version}");

    SetVersionComponents(versionMatch);
    AssemblyFileVersion = CreateAssemblyFileVersion();
    AssemblyVersion = $"{_major}.{_minor}.0.0";
    VersionWithoutPreRelease = $"{_major}.{_minor}.{_patch}";
  }

  public string GetAssemblyInformationVersion (string configurationId, string additionalBuildMetadata) =>
      $"{Version}+{configurationId}{(additionalBuildMetadata != "" ? $".{additionalBuildMetadata}" : "")}";

  private string CreateAssemblyFileVersion ()
  {
    var revisionPrefix = !string.IsNullOrEmpty(_preReleaseName) ? (Convert.ToByte(_preReleaseName.ToLower()[0]) - 96).ToString() : "30";
    var revisionSuffix = string.IsNullOrEmpty(_preReleaseCounter) ? "000" : _preReleaseCounter;
    revisionPrefix = revisionPrefix.PadLeft(2, '0');
    revisionSuffix = revisionSuffix.PadLeft(3, '0');
    return $"{_major}.{_minor}.{_patch}.{revisionPrefix}{revisionSuffix}";
  }

  [MemberNotNull(nameof(_major))]
  [MemberNotNull(nameof(_minor))]
  [MemberNotNull(nameof(_patch))]
  [MemberNotNull(nameof(_preReleaseName))]
  [MemberNotNull(nameof(_preReleaseCounter))]
  private void SetVersionComponents (Match versionMatch)
  {
    _major = versionMatch.Groups["major"].Value;
    _minor = versionMatch.Groups["minor"].Value;
    _patch = versionMatch.Groups["patch"].Value;
    _preReleaseName = versionMatch.Groups["prereleaseName"].Value;
    _preReleaseCounter = versionMatch.Groups["prereleaseCounter"].Value;
  }
}