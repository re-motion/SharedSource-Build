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
using System.Linq;
using System.Text;
using Nuke.Common.Utilities;
using Remotion.BuildScript.Nuke.GenerateTestMatrix;

public class TestArgumentHelper
{
  /// <remarks>
  ///   e.g. testAssemblyFileName.debug.x64.NET45.NET45.SQL.Exclude=exclude1-exclude2.Include=include1-include2
  /// </remarks>
  public string CreateTestName (
      TestConfiguration testConfig,
      IReadOnlyCollection<string> mergedTestCategoriesToExclude,
      IReadOnlyCollection<string> mergedTestCategoriesToInclude)
  {
    var testName = new StringBuilder().Append(
        $"{testConfig.TestAssemblyFileName}.{testConfig.ConfigurationID}.{testConfig.Platform}.{testConfig.ExecutionRuntime.Key}.{testConfig.TargetRuntime}"
    );
    if (testConfig.DatabaseSystem != MetadataValueConstants.NoDB) testName.Append('.').Append(testConfig.DatabaseSystem);
    if (mergedTestCategoriesToExclude.Any())
      testName.Append(".Exclude-").AppendJoin('-', mergedTestCategoriesToExclude);
    if (mergedTestCategoriesToInclude.Any())
      testName.Append(".Include-").AppendJoin('-', mergedTestCategoriesToInclude);
    return testName.ToString();
  }

  /// <remarks>
  ///   e.g. --filter (TestCategory!=exclude1&amp;TestCategory!=exclude2)&amp;(TestCategory=include1|TestCategory=include2)
  /// </remarks>
  public string CreateDotNetTestFilter (
      IReadOnlyCollection<string> mergedTestCategoriesToExclude,
      IReadOnlyCollection<string> mergedTestCategoriesToInclude)
  {
    var excludeFilters = CreateDotNetFilter(mergedTestCategoriesToExclude, "&", false);
    var includeFilters = CreateDotNetFilter(mergedTestCategoriesToInclude, "|");
    var testFilter = "";
    if (!excludeFilters.IsNullOrEmpty() && !includeFilters.IsNullOrEmpty())
      testFilter = $"({excludeFilters})&({includeFilters})";
    else if (excludeFilters.IsNullOrEmpty() && !includeFilters.IsNullOrEmpty())
      testFilter = $"{includeFilters}";
    else if (!excludeFilters.IsNullOrEmpty() && includeFilters.IsNullOrEmpty())
      testFilter = $"{excludeFilters}";
    return testFilter;
  }

  /// <remarks>
  ///   e.g. --where cat == include1 &amp;&amp; cat == include2 &amp;&amp; cat == include3
  /// </remarks>
  public string CreateDotNetFilter (IReadOnlyCollection<string> mergedTestCategories, string joinSymbol, bool equal = true)
  {
    var equalSymbol = equal ? "=" : "!=";
    return string.Join(
        joinSymbol,
        mergedTestCategories
            .Where(x => !x.IsNullOrEmpty())
            .Select(x => $"TestCategory{equalSymbol}{x}")
    );
  }
}