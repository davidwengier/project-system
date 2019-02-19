﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    /// <summary>
    ///     Represents a marker interface for types that apply changes to <see cref="IWorkspaceProjectContext"/> instances.
    /// </summary>
    internal interface IWorkspaceContextHandler
    {
        /// <summary>
        ///     Initialises the handler with the specified <see cref="IWorkspaceProjectContext"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     <see cref="Initialise(IWorkspaceProjectContext)"/> has already been called.
        /// </exception>
        void Initialise(IWorkspaceProjectContext context);
    }
}
