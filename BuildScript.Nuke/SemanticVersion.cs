// Copyright (c) rubicon IT GmbH, www.rubicon.eu
//
// See the NOTICE file distributed with this work for additional information
// regarding copyright ownership.  rubicon licenses this file to you under 
// the Apache License, Version 2.0 (the "License"); you may not use this 
// file except in compliance with the License.  You may obtain a copy of the 
// License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software 
// distributed under the License is distributed on an "AS IS" BASIS, WITHOUT 
// WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.  See the 
// License for the specific language governing permissions and limitations
// under the License.
//

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Nuke.Common;

namespace Remotion.BuildScript;

/// <summary>
///   Creates all needed versions for the build from the base version
/// </summary>
public class SemanticVersion
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

  public SemanticVersion (string version, bool isLocalBuild)
  {
    Version = version;

    var versionMatch = Regex.Match(Version, c_versionPattern);
    if (!versionMatch.Success)
      Assert.Fail($"Version semantic doesn't match: {Version}");

    SetVersionComponents(versionMatch);

    if (isLocalBuild)
    {
      Version = $"{_major}.{_minor}.{_patch}-x.9.{DateTime.Now:yyMMdd-HHmmss}";
      _preReleaseName = "x";
      _preReleaseCounter = "9";
    }

    AssemblyFileVersion = CreateAssemblyFileVersion();
    AssemblyVersion = $"{_major}.{_minor}.0.0";
    VersionWithoutPreRelease = $"{_major}.{_minor}.{_patch}";
  }

  public string GetAssemblyInformationalVersion (string configurationId, string additionalBuildMetadata) =>
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