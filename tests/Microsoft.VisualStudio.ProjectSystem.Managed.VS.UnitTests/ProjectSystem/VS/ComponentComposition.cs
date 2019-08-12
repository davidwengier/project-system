﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem.VS.LanguageServices;
using Xunit;

#nullable disable

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal partial class ComponentComposition
    {
        /// <summary>
        /// The list of assemblies that may contain <see cref="ProjectSystemContractProvider.System"/> exports.
        /// </summary>
        private static readonly IReadOnlyList<Assembly> BuildInAssemblies = new Assembly[]
        {
            typeof(IConfiguredProjectImplicitActivationTracking).Assembly,  // Microsoft.VisualStudio.ProjectSystem.Managed
            typeof(VsContainedLanguageComponentsFactory).Assembly,          // Microsoft.VisualStudio.ProjectSystem.Managed.VS
        };

        /// <summary>
        /// The list of assemblies to scan for contracts.
        /// </summary>
        private static readonly IReadOnlyList<Assembly> ContractAssemblies = new Assembly[]
        {
            typeof(IProjectService).Assembly,                               // Microsoft.VisualStudio.ProjectSystem
            typeof(IVsProjectServices).Assembly,                            // Microsoft.VisualStudio.ProjectSystem.VS
            typeof(IConfiguredProjectImplicitActivationTracking).Assembly,  // Microsoft.VisualStudio.ProjectSystem.Managed
            typeof(VsContainedLanguageComponentsFactory).Assembly,          // Microsoft.VisualStudio.ProjectSystem.Managed.VS
        };

        public static readonly ComponentComposition Instance = new ComponentComposition();

        public ComponentComposition()
        {
            var discovery = PartDiscovery.Combine(new AttributedPartDiscoveryV1(Resolver.DefaultInstance),
                                                  new AttributedPartDiscovery(Resolver.DefaultInstance, isNonPublicSupported: true));

            var parts = discovery.CreatePartsAsync(BuildInAssemblies).GetAwaiter().GetResult();
            var scopeParts = discovery.CreatePartsAsync(typeof(UnconfiguredProjectScope), typeof(ConfiguredProjectScope), typeof(ProjectServiceScope), typeof(GlobalScope)).GetAwaiter().GetResult();

            ComposableCatalog catalog = ComposableCatalog.Create(Resolver.DefaultInstance)
                .AddParts(parts)
                .AddParts(scopeParts)
                .WithCompositionService();

            // Prepare the self-host service and composition
            Catalog = catalog;
            Configuration = CompositionConfiguration.Create(catalog);
            Contracts = CollectContractMetadata(ContractAssemblies.Union(BuildInAssemblies));
            ContractsRequiringAppliesTo = CollectContractsRequiringAppliesTo(catalog);
            InterfaceNames = CollectIntefaceNames(ContractAssemblies);
        }

        public ComposableCatalog Catalog
        {
            get;
        }

        public CompositionConfiguration Configuration
        {
            get;
        }

        public IDictionary<string, ContractMetadata> Contracts
        {
            get;
        }

        public IDictionary<string, ISet<Type>> ContractsRequiringAppliesTo
        {
            get;
        }

        public ISet<string> InterfaceNames
        {
            get;
        }

        public ComposedPart FindComposedPart(Type type)
        {
            foreach (ComposedPart part in Configuration.Parts)
            {
                if (type == part.Definition.Type)
                {
                    return part;
                }
            }

            return null;
        }

        public ComposablePartDefinition FindComposablePartDefinition(Type type)
        {
            foreach (ComposablePartDefinition part in Catalog.Parts)
            {
                if (type == part.Type)
                {
                    return part;
                }
            }

            return null;
        }

        private IDictionary<string, ISet<Type>> CollectContractsRequiringAppliesTo(ComposableCatalog catalog)
        {
            var contractsRequiringAppliesTo = new Dictionary<string, ISet<Type>>();

            // First step, we scan all imports, and gather all places requiring "AppliesTo" metadata.
            foreach (ComposablePartDefinition part in catalog.Parts)
            {
                foreach (ImportDefinitionBinding import in part.ImportingMembers)
                {
                    if (IsAppliesToRequired(import))
                    {
                        if (!contractsRequiringAppliesTo.TryGetValue(import.ImportDefinition.ContractName, out ISet<Type> contractTypes))
                        {
                            contractTypes = new HashSet<Type>();
                            contractsRequiringAppliesTo.Add(import.ImportDefinition.ContractName, contractTypes);
                        }

                        contractTypes.Add(import.ImportingSiteElementType);
                    }
                }
            }

            return contractsRequiringAppliesTo;
        }

        private ISet<string> CollectIntefaceNames(IEnumerable<Assembly> assemblies)
        {
            var interfaceNames = new HashSet<string>();
            foreach (Assembly assembly in assemblies)
            {
                foreach (Type type in GetTypes(assembly))
                {
                    if (type.IsPublic && type.IsInterface)
                    {
                        interfaceNames.Add(type.FullName);
                    }
                }
            }

            return interfaceNames;
        }

        private Dictionary<string, ContractMetadata> CollectContractMetadata(IEnumerable<Assembly> assemblies)
        {
            Requires.NotNull(assemblies, nameof(assemblies));
            var contracts = new Dictionary<string, ContractMetadata>(StringComparer.Ordinal);
            foreach (Assembly contractAssembly in assemblies)
            {
                ReadContractMetadata(contracts, contractAssembly);
            }

            return contracts;
        }

        private void ReadContractMetadata(Dictionary<string, ContractMetadata> contracts, Assembly contractAssembly)
        {
            Requires.NotNull(contracts, nameof(contracts));
            Requires.NotNull(contractAssembly, nameof(contractAssembly));
            foreach (ProjectSystemContractAttribute assemblyAttribute in contractAssembly.GetCustomAttributes<ProjectSystemContractAttribute>())
            {
                if (assemblyAttribute.ContractName != null || assemblyAttribute.ContractType != null)
                {
                    AddContractMetadata(contracts, assemblyAttribute.ContractName ?? assemblyAttribute.ContractType.FullName, assemblyAttribute.Scope, assemblyAttribute.Provider, assemblyAttribute.Cardinality);
                }
            }

            foreach (Type definedType in GetTypes(contractAssembly))
            {
                if (definedType.IsInterface || definedType.IsClass)
                {
                    foreach (ProjectSystemContractAttribute attribute in definedType.GetCustomAttributes<ProjectSystemContractAttribute>())
                    {
                        string name = attribute.ContractName ?? definedType.FullName;
                        AddContractMetadata(contracts, name, attribute.Scope, attribute.Provider, attribute.Cardinality);
                    }
                }
            }
        }

        private Type[] GetTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                var exceptions = ex.LoaderExceptions.Where(e => !IsIgnorable(e))
                                                    .ToArray();

                if (exceptions.Length == 0)
                    return ex.Types.Where(t => t != null).ToArray();

                string message = ex.ToString();

                message += "\nLoaderExceptions:\n";

                for (int i = 0; i < ex.LoaderExceptions.Length; i++)
                {
                    message += ex.LoaderExceptions[i].ToString();
                    message += "\n";
                }

                Assert.False(true, message);
            }

            return null;
        }

        private bool IsIgnorable(Exception exception)
        {
            if (exception is FileNotFoundException fileNotFound)
            {
                return fileNotFound.FileName == "Microsoft.VisualStudio.ProjectServices, Version=15.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            }

            return false;
        }

        private void AddContractMetadata(Dictionary<string, ContractMetadata> contracts, string name, ProjectSystemContractScope scope, ProjectSystemContractProvider provider, ImportCardinality cardinality)
        {
            Requires.NotNull(contracts, nameof(contracts));
            Requires.NotNullOrEmpty(name, nameof(name));

            if (!contracts.TryGetValue(name, out ContractMetadata metadata))
            {
                metadata = new ContractMetadata
                {
                    Provider = provider,
                    Scope = scope,
                    Cardinality = cardinality
                };

                contracts.Add(name, metadata);
            }
            else
            {
                // We don't support using the contract name with different interfaces, so we don't verify those contracts.
                if (metadata.Scope != scope)
                {
                    metadata.Scope = null;
                }

                if (metadata.Provider != provider)
                {
                    metadata.Provider = null;
                }

                if (metadata.Cardinality != cardinality)
                {
                    metadata.Cardinality = null;
                }
            }
        }

        /// <summary>
        /// Check whether the import requiring a component to have "AppliesTo" metadata.
        /// If the imports ask metadata from the exports, and the metadata based on IAppliesToMetadataView,
        /// the "AppliesTo" metadata is required.
        /// </summary>
        private static bool IsAppliesToRequired(ImportDefinitionBinding import)
        {
            Type metadataType = import.MetadataType;
            Type appliesToView = typeof(IAppliesToMetadataView);
            return metadataType != null && appliesToView.IsAssignableFrom(appliesToView);
        }
    }
}
