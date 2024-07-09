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

using System;
using System.Text.RegularExpressions;

namespace Remotion.BuildScript;

/// <summary>
/// Utility to format versions according to re-motion's version rules.
/// </summary>
public class RemotionBuildVersionFormatter
{
  private static readonly Regex s_versionPreReleaseSuffixPatternRegex = new(
      @"^(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)"
      + @"(?:\-(?<prereleaseSuffix>(?<prereleaseName>[a-zA-Z]+)\.(?<prereleaseCounter>[1-9][0-9]{0,2})(?<prereleaseRemainder>(?:\.[a-zA-Z0-9-]+)*)))?$");

  public static RemotionBuildVersionFormatter Instance { get; } = new();

  private readonly Func<DateTime> _getCurrentTime;

  private RemotionBuildVersionFormatter ()
      : this(static () => DateTime.Now)
  {
  }

  // Unit testing override for the current time
  internal RemotionBuildVersionFormatter (Func<DateTime> getCurrentTime)
  {
    _getCurrentTime = getCurrentTime;
  }

  public RemotionBuildVersion FormatRemotionBuildVersion (
      string versionString,
      bool isServerBuild,
      bool useReleaseVersioning,
      string buildNumber)
  {
    ArgumentNullException.ThrowIfNull(versionString);
    ArgumentNullException.ThrowIfNull(buildNumber);

    var match = s_versionPreReleaseSuffixPatternRegex.Match(versionString);
    if (!match.Success)
    {
      throw new FormatException(
          $"The specified version '{versionString}' is invalid. Only versions according to SemVer 2.0 without build metadata are allowed."
          + " In case of a pre-release version, the release-counter may only contain up to 3 digits (e.g. '-alpha.123').");
    }

    var major = match.Groups["major"].Value;
    var minor = match.Groups["minor"].Value;
    var patch = match.Groups["patch"].Value;
    var preReleaseName = match.Groups["prereleaseName"].Value;
    var preReleaseCounter = match.Groups["prereleaseCounter"].Value;

    string version;
    if (useReleaseVersioning)
    {
      if (isServerBuild)
      {
        version = versionString;
      }
      else
      {
        var formattedDate = _getCurrentTime().ToString("yyMMdd-HHmmss");
        version = $"{major}.{minor}.{patch}-x.9.{formattedDate}";
        preReleaseName = "x";
        preReleaseCounter = "9";
      }
    }
    else
    {
      if (isServerBuild)
      {
        version = $"{major}.{minor}.{patch}-x.1.{buildNumber}";
        preReleaseName = "x";
        preReleaseCounter = "1";
      }
      else
      {
        // For local test builds, we don't want unique version to prevent unnecessary re-compiles.
        version = $"{major}.{minor}.{patch}-x.9";
        preReleaseName = "x";
        preReleaseCounter = "9";
      }
    }

    var assemblyFileVersion = CreateAssemblyFileVersion(major, minor, patch, preReleaseName, preReleaseCounter);
    var assemblyVersion = $"{major}.{minor}.0.0";
    var assemblyNuGetVersion = version;

    return new RemotionBuildVersion(
        version,
        assemblyVersion,
        assemblyFileVersion,
        assemblyNuGetVersion);
  }

  public string FormatAssemblyInformationalVersion (string version, string configuration, string? additionalBuildMetadata)
  {
    return string.IsNullOrEmpty(additionalBuildMetadata)
        ? $"{version}+{configuration}"
        : $"{version}+{configuration}.{additionalBuildMetadata}";
  }

  private static string CreateAssemblyFileVersion (string major, string minor, string patch, string preReleaseName, string preReleaseCounter)
  {
    var revisionPrefix = !string.IsNullOrEmpty(preReleaseName)
        ? (Convert.ToByte(preReleaseName.ToLower()[0]) - 96).ToString()
        : "30";
    var revisionSuffix = string.IsNullOrEmpty(preReleaseCounter)
        ? "000"
        : preReleaseCounter;
    revisionPrefix = revisionPrefix.PadLeft(2, '0');
    revisionSuffix = revisionSuffix.PadLeft(3, '0');
    return $"{major}.{minor}.{patch}.{revisionPrefix}{revisionSuffix}";
  }
}