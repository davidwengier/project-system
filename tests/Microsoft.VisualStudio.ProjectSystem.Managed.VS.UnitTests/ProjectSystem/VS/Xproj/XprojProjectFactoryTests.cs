﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;

using Microsoft.VisualStudio.Shell.Interop;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Xproj
{
    public sealed class XprojProjectFactoryTests
    {
        [Fact]
        public void UpgradeCheck()
        {
            string projectPath = "foo\\bar.xproj";

            var factory = new XprojProjectFactory();

            var loggedMessages = new List<LogMessage>();
            var logger = IVsUpgradeLoggerFactory.CreateLogger(loggedMessages);

            factory.UpgradeProject_CheckOnly(
                fileName: projectPath,
                logger,
                out uint upgradeRequired,
                out Guid migratedProjectFactor,
                out uint upgradeProjectCapabilityFlags);

            Assert.Equal((uint)__VSPPROJECTUPGRADEVIAFACTORYREPAIRFLAGS.VSPUVF_PROJECT_DEPRECATED, upgradeRequired);
            Assert.Equal(typeof(XprojProjectFactory).GUID, migratedProjectFactor);
            Assert.Equal(default, upgradeProjectCapabilityFlags);

            LogMessage message = Assert.Single(loggedMessages);
            Assert.Equal(projectPath, message.File);
            Assert.Equal(Path.GetFileNameWithoutExtension(projectPath), message.Project);
            Assert.Equal((uint)__VSUL_ERRORLEVEL.VSUL_ERROR, message.Level);
            Assert.Equal(VSResources.XprojNotSupported, message.Message);
        }
    }
}
