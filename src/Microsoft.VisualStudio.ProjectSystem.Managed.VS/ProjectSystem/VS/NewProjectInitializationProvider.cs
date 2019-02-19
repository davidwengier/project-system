﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    /// This class is here only to remember whether this is a newly created project or not. CPS will import INewProjectInitializationProvider and Call
    /// InitialiseNewProject for new projects. Just set a bool to remember this state.
    /// 
    /// </summary>
    [AppliesTo(ProjectCapability.DotNet)]
    [Export(typeof(INewProjectInitializationProvider))]
    [Export(typeof(IProjectCreationState))]
    internal sealed class NewProjectInitializationProvider : INewProjectInitializationProvider, IProjectCreationState
    {
        [ImportingConstructor]
        internal NewProjectInitializationProvider(UnconfiguredProject project)
        {
        }

        public bool WasNewlyCreated { get; private set; }
        public void InitialiseNewProject()
        {
            WasNewlyCreated = true;
        }
    }
}
