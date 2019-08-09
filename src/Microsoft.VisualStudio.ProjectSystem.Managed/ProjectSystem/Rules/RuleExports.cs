using System;

using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.Rules
{
    internal class RuleExports
    {
        private const string AssemblyName = "Microsoft.VisualStudio.ProjectSystem.Managed,version=16.0.0.0,publicKeyToken=31bf3856ad364e35,culture=neutral";

        [ExportPropertyXamlRuleDefinition(AssemblyName, "XamlRuleToCode:Compile.xaml", "File")]
        [AppliesTo(ProjectCapability.DotNet)]
        private object Compile{ get { throw new NotImplementedException(); } }


        [ExportPropertyXamlRuleDefinition(AssemblyName, "XamlRuleToCode:Compile.BrowseObject.xaml", "BrowseObject")]
        [AppliesTo(ProjectCapability.DotNet)]
        private object Compile_BrowseObject { get { throw new NotImplementedException(); } }
    }
}
