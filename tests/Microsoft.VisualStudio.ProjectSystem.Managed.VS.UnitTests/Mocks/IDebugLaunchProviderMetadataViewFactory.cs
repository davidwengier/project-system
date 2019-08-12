﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;

#nullable disable

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    public class IDebugLaunchProviderMetadataViewFactory
    {
        public static IDebugLaunchProviderMetadataView CreateInstance()
        {
            return Mock.Of<IDebugLaunchProviderMetadataView>();
        }
    }
}
