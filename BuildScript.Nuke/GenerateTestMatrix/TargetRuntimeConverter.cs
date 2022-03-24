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
using System.Text.RegularExpressions;

namespace Remotion.BuildScript.GenerateTestMatrix;

/// <summary>
///   Converts target runtimes from NUnit format to target framework moniker format and vice-versa.
/// </summary>
public static class TargetRuntimeConverter
{
  public static string ToProjectFileFromNUnitFormat (string targetFrameworkProjectFile)
  {
    if (string.IsNullOrEmpty(targetFrameworkProjectFile))
      throw new ArgumentException("Parameter needs to not empty and not null.");
    var versionString = targetFrameworkProjectFile
        .Replace(".", "")
        .Replace("NET-", "", StringComparison.OrdinalIgnoreCase);
    var versionDigits = versionString.ToCharArray();

    return "net" + string.Join("", versionDigits);
  }

  public static string ToNUnitFormatFromProjectFile (string targetFrameworkProjectFile)
  {
    if (string.IsNullOrEmpty(targetFrameworkProjectFile))
      throw new ArgumentException("Parameter needs to not empty and not null.");
    var versionString = targetFrameworkProjectFile
        .Replace(".", "")
        .Replace("NET", "", StringComparison.OrdinalIgnoreCase);
    var versionDigits = versionString.ToCharArray();

    return "NET-" + string.Join(".", versionDigits);
  }

  public static string ToNUnitFormatFromMoniker (string targetFrameworkMoniker)
  {
    if (string.IsNullOrEmpty(targetFrameworkMoniker))
      throw new ArgumentException("Parameter needs to not empty and not null.");
    var versionString = Regex.Replace(targetFrameworkMoniker, "NET", "", RegexOptions.IgnoreCase);
    var versionDigits = versionString.ToCharArray();

    return "NET-" + string.Join(".", versionDigits);
  }

  public static string ToTargetFrameworkMonikerFromNUnitFormat (string nUnitFormat)
  {
    if (string.IsNullOrEmpty(nUnitFormat))
      throw new ArgumentException("Parameter needs to not empty and not null.");
    var versionString = Regex.Replace(nUnitFormat, "NET-", "", RegexOptions.IgnoreCase);
    var versionDigits = versionString.Split('.');

    return "NET" + string.Join("", versionDigits);
  }
}