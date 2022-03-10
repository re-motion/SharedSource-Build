using System;
using System.Linq;
using Microsoft.Build.Evaluation;
using Nuke.Common.IO;

public class PropertiesPropsReader : BasePropsReader
{
  private const string c_propertiesFileName = "Properties.props";
  private const string c_assemblyInfoFileProperty = "AssemblyInfoFile";
  private const string c_companyNameProperty = "CompanyName";
  private const string c_companyUrlProperty = "CompanyUrl";
  private const string c_copyrightProperty = "Copyright";
  private const string c_productNameProperty = "ProductName";

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
    var assemblyInfoFile = _xmlProperties.Properties.Single(prop => prop.Name == c_assemblyInfoFileProperty);
    var companyName = _xmlProperties.Properties.Single(prop => prop.Name == c_companyNameProperty);
    var companyUrl = _xmlProperties.Properties.Single(prop => prop.Name == c_companyUrlProperty);
    var copyright = _xmlProperties.Properties.Single(prop => prop.Name == c_copyrightProperty);
    var productName = _xmlProperties.Properties.Single(prop => prop.Name == c_productNameProperty);
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