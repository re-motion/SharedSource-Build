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

namespace Remotion.BuildScript.Test.Dimensions;

public class DockerExecutionRuntimes : ExecutionRuntimes, IRequiresTestParameters
{
  private const string c_imageParameterName = "Image";
  private const string c_isolationModeParameterName = "IsolationMode";

  public DockerExecutionRuntimes (string value)
      : base(value)
  {
  }

  public void ConfigureTestParameters (TestParameterBuilder builder)
  {
    builder.AddRequiredParameter(this, c_imageParameterName);
    builder.AddOptionalParameter(this, c_isolationModeParameterName, "");
  }

  public string GetImage (TestExecutionContext context)
  {
    return context.GetTestParameter(this, c_imageParameterName);
  }

  public string GetIsolationMode (TestExecutionContext context)
  {
    return context.GetTestParameter(this, c_isolationModeParameterName);
  }
}