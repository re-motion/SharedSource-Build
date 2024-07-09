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
using System.Linq;
using Microsoft.Build.Evaluation;
using Nuke.Common.IO;

namespace Remotion.BuildScript;

public class PropertiesPropsReader : BasePropsReader
{
  private const string c_propertiesFileName = "Properties.props";


  private readonly Project _xmlProperties;

  private PropertiesPropsReader (AbsolutePath solutionDirectoryPath, AbsolutePath customizationDirectoryPath)
      : base(solutionDirectoryPath, customizationDirectoryPath)
  {
    _xmlProperties = LoadProjectWithSolutionDirectoryPropertySet(c_propertiesFileName);
  }

  public static AssemblyMetadata Read (AbsolutePath solutionDirectoryPath, AbsolutePath customizationDirectoryPath)
  {
    var propertiesPropsReader = new PropertiesPropsReader(solutionDirectoryPath, customizationDirectoryPath);
    return propertiesPropsReader.ReadPropertiesDefinition();
  }

  private AssemblyMetadata ReadPropertiesDefinition ()
  {
    var assemblyInfoFile = _xmlProperties.Properties.Single(prop => prop.Name == MSBuildProperties.AssemblyInfoFile);
    var companyName = _xmlProperties.Properties.Single(prop => prop.Name == MSBuildProperties.CompanyName);
    var companyUrl = _xmlProperties.Properties.Single(prop => prop.Name == MSBuildProperties.CompanyUrl);
    var copyright = _xmlProperties.Properties.Single(prop => prop.Name == MSBuildProperties.Copyright);
    var productName = _xmlProperties.Properties.Single(prop => prop.Name == MSBuildProperties.ProductName);
    var documentationFileName = _xmlProperties.Properties.SingleOrDefault(prop => prop.Name == SandcastleProperties.DocumentationFileName);
    var documentationRootPage = _xmlProperties.Properties.SingleOrDefault(prop => prop.Name == SandcastleProperties.DocumentationRootPage);
    var documentationNamespaceSummaryFiles =
        _xmlProperties.Properties.SingleOrDefault(prop => prop.Name == SandcastleProperties.DocumentationNamespaceSummaryFiles);
    return new AssemblyMetadata
           {
               AssemblyInfoFile = assemblyInfoFile.EvaluatedValue,
               CompanyName = companyName.EvaluatedValue,
               CompanyUrl = companyUrl.EvaluatedValue,
               Copyright = copyright.EvaluatedValue,
               ProductName = productName.EvaluatedValue,
               DocumentationFileName = documentationFileName != null ? documentationFileName.EvaluatedValue : "",
               DocumentationRootPage = documentationRootPage != null ? documentationRootPage.EvaluatedValue : "",
               DocumentationNamespaceSummaryFiles = documentationNamespaceSummaryFiles != null ? documentationNamespaceSummaryFiles.EvaluatedValue : ""
           };
  }
}
