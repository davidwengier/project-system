﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.VS.UI;
using Moq;

#nullable disable

namespace Microsoft.VisualStudio
{
    internal class IDialogServicesFactory
    {
        public static IDialogServices Create()
        {
            var mock = new Mock<IDialogServices>();
            mock.Setup(s => s.DontShowAgainMessageBox(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(() => true);
            return mock.Object;
        }
    }
}
