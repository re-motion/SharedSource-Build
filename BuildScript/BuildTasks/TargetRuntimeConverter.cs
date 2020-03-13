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
using System.Text.RegularExpressions;

namespace Remotion.BuildScript.BuildTasks
{
  /// <summary>
  /// Converts target runtimes from NUnit format to target framework moniker format and vice-versa.
  /// </summary>
  public static class TargetRuntimeConverter
  {
    public static string ToNUnitFormat (string targetFrameworkMoniker)
    {
      return Regex.Replace (targetFrameworkMoniker, @"NET(\d)(\d)", "NET-$1.$2", RegexOptions.IgnoreCase);
    }

    public static string ToTargetFrameworkMoniker (string nUnitFormat)
    {
      return Regex.Replace (nUnitFormat, @"NET-(\d).(\d)", "NET$1$2", RegexOptions.IgnoreCase);
    }
  }
}