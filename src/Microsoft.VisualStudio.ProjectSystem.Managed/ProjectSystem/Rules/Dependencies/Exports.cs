using System;

using Microsoft.VisualStudio.ProjectSystem.Properties;

#pragma warning disable CA1822  // Mark members as static
#pragma warning disable IDE0051 // Remove unused private members

namespace Microsoft.VisualStudio.ProjectSystem.Rules.Dependencies
{
    internal class Exports
    {
        private const string AssemblyName = "Microsoft.VisualStudio.ProjectSystem.Managed,version=16.0.0.0,publicKeyToken=31bf3856ad364e35,culture=neutral";

        [AppliesTo(ProjectCapability.DotNet)]
        [ExportPropertyXamlRuleDefinition(AssemblyName, "XamlRuleToCode:AnalyzerReference.xaml", "Project;ProjectSubscriptionService;BrowseObject", UseLocalizedResources = true)]
        private object AnalyzerReference { get { throw new NotImplementedException(); } }

        [AppliesTo(ProjectCapability.DotNet)]
        [ExportPropertyXamlRuleDefinition(AssemblyName, "XamlRuleToCode:AssemblyReference.xaml", "Project;BrowseObject", UseLocalizedResources = true)]
        private object AssemblyReference { get { throw new NotImplementedException(); } }

        [AppliesTo(ProjectCapability.DotNet)]
        [ExportPropertyXamlRuleDefinition(AssemblyName, "XamlRuleToCode:COMReference.xaml", "Project;BrowseObject", UseLocalizedResources = true)]
        private object COMReference { get { throw new NotImplementedException(); } }

        [AppliesTo(ProjectCapability.DotNet)]
        [ExportPropertyXamlRuleDefinition(AssemblyName, "XamlRuleToCode:FrameworkReference.xaml", "Project;BrowseObject", UseLocalizedResources = true)]
        private object FrameworkReference { get { throw new NotImplementedException(); } }

        [AppliesTo(ProjectCapability.DotNet)]
        [ExportPropertyXamlRuleDefinition(AssemblyName, "XamlRuleToCode:PackageReference.xaml", "ProjectSubscriptionService;BrowseObject", UseLocalizedResources = true)]
        private object PackageReference { get { throw new NotImplementedException(); } }

        [AppliesTo(ProjectCapability.DotNet)]
        [ExportPropertyXamlRuleDefinition(AssemblyName, "XamlRuleToCode:ProjectReference.xaml", "Project;BrowseObject", UseLocalizedResources = true)]
        private object ProjectReference { get { throw new NotImplementedException(); } }

        [AppliesTo(ProjectCapability.DotNet)]
        [ExportPropertyXamlRuleDefinition(AssemblyName, "XamlRuleToCode:ResolvedAnalyzerReference.xaml", "ProjectSubscriptionService;BrowseObject", UseLocalizedResources = true)]
        private object ResolvedAnalyzerReference { get { throw new NotImplementedException(); } }

        [AppliesTo(ProjectCapability.DotNet)]
        [ExportPropertyXamlRuleDefinition(AssemblyName, "XamlRuleToCode:ResolvedAssemblyReference.xaml", "ProjectSubscriptionService;BrowseObject", UseLocalizedResources = true)]
        private object ResolvedAssemblyReference { get { throw new NotImplementedException(); } }

        [AppliesTo(ProjectCapability.DotNet)]
        [ExportPropertyXamlRuleDefinition(AssemblyName, "XamlRuleToCode:ResolvedCOMReference.xaml", "ProjectSubscriptionService;BrowseObject", UseLocalizedResources = true)]
        private object ResolvedCOMReference { get { throw new NotImplementedException(); } }

        [AppliesTo(ProjectCapability.DotNet)]
        [ExportPropertyXamlRuleDefinition(AssemblyName, "XamlRuleToCode:ResolvedFrameworkReference.xaml", "ProjectSubscriptionService;BrowseObject", UseLocalizedResources = true)]
        private object ResolvedFrameworkReference { get { throw new NotImplementedException(); } }

        [AppliesTo(ProjectCapability.DotNet)]
        [ExportPropertyXamlRuleDefinition(AssemblyName, "XamlRuleToCode:ResolvedPackageReference.xaml", "ProjectSubscriptionService;BrowseObject", UseLocalizedResources = true)]
        private object ResolvedPackageReference { get { throw new NotImplementedException(); } }

        [AppliesTo(ProjectCapability.DotNet)]
        [ExportPropertyXamlRuleDefinition(AssemblyName, "XamlRuleToCode:ResolvedProjectReference.xaml", "ProjectSubscriptionService;BrowseObject", UseLocalizedResources = true)]
        private object ResolvedProjectReference { get { throw new NotImplementedException(); } }

        [AppliesTo(ProjectCapability.DotNet)]
        [ExportPropertyXamlRuleDefinition(AssemblyName, "XamlRuleToCode:ResolvedSdkReference.xaml", "ProjectSubscriptionService;BrowseObject", UseLocalizedResources = true)]
        private object ResolvedSdkReference { get { throw new NotImplementedException(); } }

        [AppliesTo(ProjectCapability.DotNet)]
        [ExportPropertyXamlRuleDefinition(AssemblyName, "XamlRuleToCode:SdkReference.xaml", "Project;ProjectSubscriptionService;BrowseObject", UseLocalizedResources = true)]
        private object SdkReference { get { throw new NotImplementedException(); } }
    }
}
