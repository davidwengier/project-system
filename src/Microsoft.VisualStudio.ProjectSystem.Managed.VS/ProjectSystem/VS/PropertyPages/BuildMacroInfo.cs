﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PropertyPages
{
    /// <summary>
    /// Implements the <see cref="IVsBuildMacroInfo"/> interface to be consumed by project properties.
    /// </summary>
    [ExportProjectNodeComService((typeof(IVsBuildMacroInfo)))]
    [AppliesTo(ProjectCapability.DotNet)]
    internal class BuildMacroInfo : IVsBuildMacroInfo, IDisposable
    {
        private IProjectThreadingService _threadingService;
        private ActiveConfiguredProject<ConfiguredProject> _configuredProject;

        /// <summary>
        /// Initialises a new instance of the <see cref="BuildMacroInfo"/> class.
        /// </summary>
        /// <param name="configuredProject">Project being evaluated.</param>
        /// <param name="threadingService">Project threading service.</param>
        [ImportingConstructor]
        public BuildMacroInfo(
            ActiveConfiguredProject<ConfiguredProject> configuredProject,
            IProjectThreadingService threadingService)
        {
            _threadingService = threadingService;
            _configuredProject = configuredProject;
        }

        /// <summary>
        /// Retrieves the value or body of a macro based on the macro's name.
        /// </summary>
        /// <param name="bstrBuildMacroName">String containing the name of the macro.</param>
        /// <param name="pbstrBuildMacroValue">String containing the value or body of the macro.</param>
        /// <returns>If the method succeeds, it returns S_OK. If it fails, it returns an error code.</returns>
        public int GetBuildMacroValue(string bstrBuildMacroName, out string pbstrBuildMacroValue)
        {
            if (_configuredProject == null)
            {
                pbstrBuildMacroValue = null;
                return HResult.Unexpected;
            }

            pbstrBuildMacroValue = null;
            ProjectSystem.Properties.IProjectProperties commonProperties = _configuredProject.Value.Services.ProjectPropertiesProvider.GetCommonProperties();
            pbstrBuildMacroValue = _threadingService.ExecuteSynchronously(() => commonProperties.GetEvaluatedPropertyValueAsync(bstrBuildMacroName));

            if (pbstrBuildMacroValue == null)
            {
                pbstrBuildMacroValue = string.Empty;
                return VSConstants.E_FAIL;
            }
            else
            {
                return VSConstants.S_OK;
            }
        }

        public void Dispose()
        {
            // Important for ProjectNodeComServices to null out fields to reduce the amount 
            // of data we leak when extensions incorrectly holds onto the IVsHierarchy.
            _threadingService = null;
            _configuredProject = null;
        }
    }
}
