﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable disable

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore
{
    internal static class ProjectRestoreInfoFactory
    {
        public static ProjectRestoreInfo Create()
        {
            return new ProjectRestoreInfo(string.Empty, string.Empty, string.Empty, null, null);
        }
    }
}
