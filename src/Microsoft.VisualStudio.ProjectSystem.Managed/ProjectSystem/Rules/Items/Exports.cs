using System;

using Microsoft.VisualStudio.ProjectSystem.Properties;

#pragma warning disable CA1822  // Mark members as static
#pragma warning disable IDE0051 // Remove unused private members

namespace Microsoft.VisualStudio.ProjectSystem.Rules.Items
{
    internal class Exports
    {
        private const string AssemblyName = "Microsoft.VisualStudio.ProjectSystem.Managed,version=16.0.0.0,publicKeyToken=31bf3856ad364e35,culture=neutral";

        [AppliesTo(ProjectCapability.DotNet)]
        [ExportPropertyXamlRuleDefinition(AssemblyName, "XamlRuleToCode:Compile.xaml", "File")]
        private object Compile { get { throw new NotImplementedException(); } }

        [AppliesTo(ProjectCapability.DotNet)]
        [ExportPropertyXamlRuleDefinition(AssemblyName, "XamlRuleToCode:Compile.BrowseObject.xaml", "BrowseObject")]
        private object Compile_BrowseObject { get { throw new NotImplementedException(); } }

        [AppliesTo(ProjectCapability.DotNet)]
        [ExportPropertyXamlRuleDefinition(AssemblyName, "XamlRuleToCode:Content.xaml", "File")]
        private object Content { get { throw new NotImplementedException(); } }

        [AppliesTo(ProjectCapability.DotNet)]
        [ExportPropertyXamlRuleDefinition(AssemblyName, "XamlRuleToCode:Content.BrowseObject.xaml", "BrowseObject")]
        private object Content_BrowseObject { get { throw new NotImplementedException(); } }

        [AppliesTo(ProjectCapability.DotNet)]
        [ExportPropertyXamlRuleDefinition(AssemblyName, "XamlRuleToCode:EmbeddedResource.xaml", "File")]
        private object EmbeddedResource { get { throw new NotImplementedException(); } }

        [AppliesTo(ProjectCapability.DotNet)]
        [ExportPropertyXamlRuleDefinition(AssemblyName, "XamlRuleToCode:EmbeddedResource.BrowseObject.xaml", "BrowseObject")]
        private object EmbeddedResource_BrowseObject { get { throw new NotImplementedException(); } }

        [AppliesTo(ProjectCapability.DotNet)]
        [ExportPropertyXamlRuleDefinition(AssemblyName, "XamlRuleToCode:None.xaml", "File")]
        private object None { get { throw new NotImplementedException(); } }

        [AppliesTo(ProjectCapability.DotNet)]
        [ExportPropertyXamlRuleDefinition(AssemblyName, "XamlRuleToCode:None.BrowseObject.xaml", "BrowseObject")]
        private object None_BrowseObject { get { throw new NotImplementedException(); } }

        [AppliesTo(ProjectCapability.DotNet)]
        [ExportPropertyXamlRuleDefinition(AssemblyName, "XamlRuleToCode:Page.xaml", "File")]
        private object Page { get { throw new NotImplementedException(); } }

        [AppliesTo(ProjectCapability.DotNet)]
        [ExportPropertyXamlRuleDefinition(AssemblyName, "XamlRuleToCode:Page.BrowseObject.xaml", "BrowseObject")]
        private object Page_BrowseObject { get { throw new NotImplementedException(); } }

        [AppliesTo(ProjectCapability.DotNet)]
        [ExportPropertyXamlRuleDefinition(AssemblyName, "XamlRuleToCode:XamlAppDef.xaml", "File")]
        private object XamlAppDef { get { throw new NotImplementedException(); } }

        [AppliesTo(ProjectCapability.DotNet)]
        [ExportPropertyXamlRuleDefinition(AssemblyName, "XamlRuleToCode:XamlAppDef.BrowseObject.xaml", "BrowseObject")]
        private object XamlAppDef_BrowseObject { get { throw new NotImplementedException(); } }

        [AppliesTo(ProjectCapability.DotNet)]
        [ExportPropertyXamlRuleDefinition(AssemblyName, "XamlRuleToCode:Resource.xaml", "File")]
        private object Resource { get { throw new NotImplementedException(); } }

        [AppliesTo(ProjectCapability.DotNet)]
        [ExportPropertyXamlRuleDefinition(AssemblyName, "XamlRuleToCode:Resource.BrowseObject.xaml", "BrowseObject")]
        private object Resource_BrowseObject { get { throw new NotImplementedException(); } }

        [AppliesTo(ProjectCapability.DotNet)]
        [ExportPropertyXamlRuleDefinition(AssemblyName, "XamlRuleToCode:AdditionalFiles.xaml", "File")]
        private object AdditionalFiles { get { throw new NotImplementedException(); } }

        [AppliesTo(ProjectCapability.DotNet)]
        [ExportPropertyXamlRuleDefinition(AssemblyName, "XamlRuleToCode:AdditionalFiles.BrowseObject.xaml", "BrowseObject")]
        private object AdditionalFiles_BrowseObject { get { throw new NotImplementedException(); } }

        [AppliesTo(ProjectCapability.DotNet)]
        [ExportPropertyXamlRuleDefinition(AssemblyName, "XamlRuleToCode:ApplicationDefinition.xaml", "File")]
        private object ApplicationDefinition { get { throw new NotImplementedException(); } }

        [AppliesTo(ProjectCapability.DotNet)]
        [ExportPropertyXamlRuleDefinition(AssemblyName, "XamlRuleToCode:ApplicationDefinition.BrowseObject.xaml", "BrowseObject")]
        private object ApplicationDefinition_BrowseObject { get { throw new NotImplementedException(); } }
    }
}
