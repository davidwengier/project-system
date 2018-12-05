﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.ProjectSystem.VS.ConnectionPoint;
using Microsoft.VisualStudio.ProjectSystem.VS.TempPE;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;

using VSLangProj;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Automation
{
    /// <summary>
    /// Manages the portable executable (PE) files produced by running custom tools.
    /// </summary>
    [Export(typeof(BuildManager))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    [Order(Order.Default)]
    internal class VSBuildManager : ConnectionPointContainer,
                                    IEventSource<_dispBuildManagerEvents>,
                                    BuildManager,
                                    BuildManagerEvents
    {
        private readonly IProjectThreadingService _threadingService;
        private readonly IUnconfiguredProjectCommonServices _unconfiguredProjectServices;
        private readonly Lazy<ITempPEBuildManager> _tempPEManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="VSBuildManager"/> class.
        /// </summary>
        [ImportingConstructor]
        internal VSBuildManager(IProjectThreadingService threadingService, IUnconfiguredProjectCommonServices unconfiguredProjectServices,
            Lazy<ITempPEBuildManager> tempPEManager)
        {
            AddEventSource(this);
            _threadingService = threadingService;
            _unconfiguredProjectServices = unconfiguredProjectServices;
            _tempPEManager = tempPEManager;

            Project = new OrderPrecedenceImportCollection<VSLangProj.VSProject>(projectCapabilityCheckProvider: _unconfiguredProjectServices.Project);
        }

        [ImportMany(ExportContractNames.VsTypes.VSProject)]
        internal OrderPrecedenceImportCollection<VSLangProj.VSProject> Project { get; }

        /// <summary>
        /// Occurs when a design time output moniker is deleted.
        /// </summary>
        public event _dispBuildManagerEvents_DesignTimeOutputDeletedEventHandler DesignTimeOutputDeleted;

        /// <summary>
        /// Occurs when a design time output moniker is dirty
        /// </summary>
        public event _dispBuildManagerEvents_DesignTimeOutputDirtyEventHandler DesignTimeOutputDirty;

        /// <summary>
        /// Gets the project of which the selected item is a part.
        /// </summary>
        public EnvDTE.Project ContainingProject => Project.FirstOrDefault()?.Value.Project;

        /// <summary>
        /// Gets the top-level extensibility object.
        /// </summary>
        public EnvDTE.DTE DTE => Project.FirstOrDefault()?.Value.DTE;

        /// <summary>
        /// Gets the immediate parent object of a given object.
        /// </summary>
        public object Parent => Project.FirstOrDefault()?.Value;

        /// <summary>
        /// We don't do anything with TempPE until someone asks us for a list of inputs, or a TempPE library. This property lets other
        /// classes that need to interact with TempPE know whether they should do any work without kicking of initialization
        /// </summary>
        internal bool ProjectNeedsTempPE => _tempPEManager.IsValueCreated;

        /// <summary>
        /// Gets the temporary portable executable (PE) monikers for a project.
        /// </summary>
        public object DesignTimeOutputMonikers
        {
            get
            {
                return _tempPEManager.Value.GetTempPESourceFileNames();
            }
        }

        /// <summary>
        /// Builds a temporary portable executable (PE) and returns its description in an XML string.
        /// </summary>
        public string BuildDesignTimeOutput(string bstrOutputMoniker)
        {
            if (bstrOutputMoniker == null)
                throw new ArgumentException("Must supply a moniker to build", nameof(bstrOutputMoniker));

            return _threadingService.ExecuteSynchronously(() =>
            {
                return _tempPEManager.Value.GetTempPEDescriptionXmlAsync(bstrOutputMoniker);
            });
        }

        void IEventSource<_dispBuildManagerEvents>.OnSinkAdded(_dispBuildManagerEvents sink)
        {
            DesignTimeOutputDeleted += new _dispBuildManagerEvents_DesignTimeOutputDeletedEventHandler(sink.DesignTimeOutputDeleted);
            DesignTimeOutputDirty += new _dispBuildManagerEvents_DesignTimeOutputDirtyEventHandler(sink.DesignTimeOutputDirty);
        }

        void IEventSource<_dispBuildManagerEvents>.OnSinkRemoved(_dispBuildManagerEvents sink)
        {
            DesignTimeOutputDeleted -= new _dispBuildManagerEvents_DesignTimeOutputDeletedEventHandler(sink.DesignTimeOutputDeleted);
            DesignTimeOutputDirty -= new _dispBuildManagerEvents_DesignTimeOutputDirtyEventHandler(sink.DesignTimeOutputDirty);
        }

        /// <summary>
        /// Occurs when a design time output moniker is deleted.
        /// </summary>
        internal virtual void OnDesignTimeOutputDeleted(string outputMoniker)
        {
            UIThreadHelper.VerifyOnUIThread();

            DesignTimeOutputDeleted?.Invoke(outputMoniker);
        }

        /// <summary>
        /// Occurs when a design time output moniker is dirty.
        /// </summary>
        internal virtual void OnDesignTimeOutputDirty(string outputMoniker)
        {
            UIThreadHelper.VerifyOnUIThread();

            DesignTimeOutputDirty?.Invoke(outputMoniker);
        }
    }
}