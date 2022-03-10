using System;
using System.Linq;
using Microsoft.Build.Evaluation;
using Nuke.Common.IO;

public class PropertiesPropsReader : BasePropsReader
{
  private const string c_propertiesFileName = "Properties.props";


  private readonly Project _xmlProperties;

  public static AssemblyMetadata Read (AbsolutePath solutionDirectoryPath, AbsolutePath customizationDirectoryPath)
  {
    var propertiesPropsReader = new PropertiesPropsReader(solutionDirectoryPath, customizationDirectoryPath);
    return propertiesPropsReader.ReadPropertiesDefinition();
  }

  private PropertiesPropsReader (AbsolutePath solutionDirectoryPath, AbsolutePath customizationDirectoryPath)
      : base(solutionDirectoryPath, customizationDirectoryPath)
  {
    _xmlProperties = LoadProjectWithSolutionDirectoryPropertySet(c_propertiesFileName);
  }

  private AssemblyMetadata ReadPropertiesDefinition ()
  {
    var assemblyInfoFile = _xmlProperties.Properties.Single(prop => prop.Name == MSBuildProperties.AssemblyInfoFile);
    var companyName = _xmlProperties.Properties.Single(prop => prop.Name == MSBuildProperties.CompanyName);
    var companyUrl = _xmlProperties.Properties.Single(prop => prop.Name == MSBuildProperties.CompanyUrl);
    var copyright = _xmlProperties.Properties.Single(prop => prop.Name == MSBuildProperties.Copyright);
    var productName = _xmlProperties.Properties.Single(prop => prop.Name == MSBuildProperties.ProductName);
    return new AssemblyMetadata
           {
               AssemblyInfoFile = assemblyInfoFile.EvaluatedValue,
               CompanyName = companyName.EvaluatedValue,
               CompanyUrl = companyUrl.EvaluatedValue,
               Copyright = copyright.EvaluatedValue,
               ProductName = productName.EvaluatedValue
           };
  }
}