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