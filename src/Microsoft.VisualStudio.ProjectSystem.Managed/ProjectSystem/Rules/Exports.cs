using System;

using Microsoft.VisualStudio.ProjectSystem.Properties;

#pragma warning disable CA1822  // Mark members as static
#pragma warning disable IDE0051 // Remove unused private members

namespace Microsoft.VisualStudio.ProjectSystem.Rules
{
    internal class Exports
    {
        private const string AssemblyName = "Microsoft.VisualStudio.ProjectSystem.Managed,version=16.0.0.0,publicKeyToken=31bf3856ad364e35,culture=neutral";

        [AppliesTo(ProjectCapability.DotNet)]
        [ExportPropertyXamlRuleDefinition(AssemblyName, "XamlRuleToCode:AdditionalDesignTimeBuildInput.xaml", "ProjectSubscriptionService", UseLocalizedResources = true)]
        private object AdditionalDesignTimeBuildInput { get { throw new NotImplementedException(); } }

        [AppliesTo(ProjectCapability.DotNet)]
        [ExportPropertyXamlRuleDefinition(AssemblyName, "XamlRuleToCode:AppDesigner.xaml", "ProjectSubscriptionService", UseLocalizedResources = true)]
        private object AppDesigner { get { throw new NotImplementedException(); } }

        [AppliesTo(ProjectCapability.DotNet)]
        [ExportPropertyXamlRuleDefinition(AssemblyName, "XamlRuleToCode:CollectedFrameworkReference.xaml", "ProjectSubscriptionService", UseLocalizedResources = true)]
        private object CollectedFrameworkReference { get { throw new NotImplementedException(); } }

        [AppliesTo(ProjectCapability.DotNet)]
        [ExportPropertyXamlRuleDefinition(AssemblyName, "XamlRuleToCode:CollectedPackageDownload.xaml", "ProjectSubscriptionService", UseLocalizedResources = true)]
        private object CollectedPackageDownload { get { throw new NotImplementedException(); } }

        [AppliesTo(ProjectCapability.DotNet)]
        [ExportPropertyXamlRuleDefinition(AssemblyName, "XamlRuleToCode:CompilerCommandLineArgs.xaml", "ProjectSubscriptionService", UseLocalizedResources = true)]
        private object CompilerCommandLineArgs { get { throw new NotImplementedException(); } }

        [AppliesTo(ProjectCapability.DotNet)]
        [ExportPropertyXamlRuleDefinition(AssemblyName, "XamlRuleToCode:ConfigurationGeneral.xaml", "Project;ProjectSubscriptionService", UseLocalizedResources = true)]
        private object ConfigurationGeneral { get { throw new NotImplementedException(); } }

        [AppliesTo(ProjectCapability.DotNet)]
        [ExportPropertyXamlRuleDefinition(AssemblyName, "XamlRuleToCode:CopyUpToDateMarker.xaml", "ProjectSubscriptionService", UseLocalizedResources = true)]
        private object CopyUpToDateMarker { get { throw new NotImplementedException(); } }

        [AppliesTo(ProjectCapability.DotNet)]
        [ExportPropertyXamlRuleDefinition(AssemblyName, "XamlRuleToCode:DebuggerGeneral.xaml", "Project", UseLocalizedResources = true)]
        private object DebuggerGeneral { get { throw new NotImplementedException(); } }

        [AppliesTo(ProjectCapability.DotNet)]
        [ExportPropertyXamlRuleDefinition(AssemblyName, "XamlRuleToCode:DotNetCliToolReference.xaml", "ProjectSubscriptionService", UseLocalizedResources = true)]
        private object DotNetCliToolReference { get { throw new NotImplementedException(); } }

        [AppliesTo(ProjectCapability.DotNet)]
        [ExportPropertyXamlRuleDefinition(AssemblyName, "XamlRuleToCode:GeneralBrowseObject.xaml", "BrowseObject", UseLocalizedResources = true)]
        private object GeneralBrowseObject { get { throw new NotImplementedException(); } }

        [AppliesTo(ProjectCapability.DotNet)]
        [ExportPropertyXamlRuleDefinition(AssemblyName, "XamlRuleToCode:GeneralConfiguredBrowseObject.xaml", "ConfiguredBrowseObject", UseLocalizedResources = true)]
        private object GeneralConfiguredBrowseObject { get { throw new NotImplementedException(); } }

        [AppliesTo(ProjectCapability.DotNet)]
        [ExportPropertyXamlRuleDefinition(AssemblyName, "XamlRuleToCode:LanguageService.xaml", "ProjectSubscriptionService", UseLocalizedResources = true)]
        private object LanguageService { get { throw new NotImplementedException(); } }

        [AppliesTo(ProjectCapability.DotNet)]
        [ExportPropertyXamlRuleDefinition(AssemblyName, "XamlRuleToCode:NuGetRestore.xaml", "ProjectSubscriptionService", UseLocalizedResources = true)]
        private object NuGetRestore { get { throw new NotImplementedException(); } }

        [AppliesTo(ProjectCapability.DotNet)]
        [ExportPropertyXamlRuleDefinition(AssemblyName, "XamlRuleToCode:PotentialEditorConfigFiles.xaml", "ProjectSubscriptionService", UseLocalizedResources = true)]
        private object PotentialEditorConfigFiles { get { throw new NotImplementedException(); } }

        [AppliesTo(ProjectCapability.DotNet)]
        [ExportPropertyXamlRuleDefinition(AssemblyName, "XamlRuleToCode:ProjectDebugger.xaml", "Project", UseLocalizedResources = true)]
        private object ProjectDebugger { get { throw new NotImplementedException(); } }

        [AppliesTo(ProjectCapability.DotNet)]
        [ExportPropertyXamlRuleDefinition(AssemblyName, "XamlRuleToCode:ResolvedCompilationReference.xaml", "ProjectSubscriptionService", UseLocalizedResources = true)]
        private object ResolvedCompilationReference { get { throw new NotImplementedException(); } }

        [AppliesTo(ProjectCapability.DotNet)]
        [ExportPropertyXamlRuleDefinition(AssemblyName, "XamlRuleToCode:SourceControl.xaml", "Invisible", UseLocalizedResources = true)]
        private object SourceControl { get { throw new NotImplementedException(); } }

        [AppliesTo(ProjectCapability.DotNet)]
        [ExportPropertyXamlRuleDefinition(AssemblyName, "XamlRuleToCode:SpecialFolder.xaml", "File;ProjectSubscriptionService", UseLocalizedResources = true)]
        private object SpecialFolder { get { throw new NotImplementedException(); } }

        [AppliesTo(ProjectCapability.DotNet)]
        [ExportPropertyXamlRuleDefinition(AssemblyName, "XamlRuleToCode:UpToDateCheckBuilt.xaml", "ProjectSubscriptionService", UseLocalizedResources = true)]
        private object UpToDateCheckBuilt { get { throw new NotImplementedException(); } }

        [AppliesTo(ProjectCapability.DotNet)]
        [ExportPropertyXamlRuleDefinition(AssemblyName, "XamlRuleToCode:UpToDateCheckInput.xaml", "ProjectSubscriptionService", UseLocalizedResources = true)]
        private object UpToDateCheckInput { get { throw new NotImplementedException(); } }

        [AppliesTo(ProjectCapability.DotNet)]
        [ExportPropertyXamlRuleDefinition(AssemblyName, "XamlRuleToCode:UpToDateCheckOutput.xaml", "ProjectSubscriptionService", UseLocalizedResources = true)]
        private object UpToDateCheckOutput { get { throw new NotImplementedException(); } }

        // NOTE: Visual Basic only
        [AppliesTo(ProjectCapability.VisualBasic)]
        [ExportPropertyXamlRuleDefinition(AssemblyName, "XamlRuleToCode:VisualBasic.NamespaceImport.xaml", "ProjectSubscriptionService", UseLocalizedResources = true)]
        private object VisualBasic_NamespaceImport { get { throw new NotImplementedException(); } }
    }
}
