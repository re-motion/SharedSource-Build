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
using System.ComponentModel;
using Nuke.Common.Tooling;

namespace Remotion.BuildScript;

[TypeConverter(typeof(TypeConverter<VisualStudioVersion>))]
public class VisualStudioVersion : Enumeration
{
  public static VisualStudioVersion VS2015 = new("2015", "14.0");
  public static VisualStudioVersion VS2017 = new("2017", "15.0");
  public static VisualStudioVersion VS2019 = new("2019", "Current");
  public static VisualStudioVersion VS2022 = new("2022", "Current");

  public string VsVersion => Value;
  public string MsBuildVersion { get; }

  private VisualStudioVersion (string vsVersion, string msBuildVersion)
  {
    Value = vsVersion;
    MsBuildVersion = msBuildVersion;
  }
}
