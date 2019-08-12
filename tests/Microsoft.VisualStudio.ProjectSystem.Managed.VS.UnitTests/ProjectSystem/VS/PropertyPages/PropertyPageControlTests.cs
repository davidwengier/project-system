﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

using Moq;
using Moq.Protected;

using Xunit;

#nullable disable

namespace Microsoft.VisualStudio.ProjectSystem.VS.PropertyPages
{
    public class PropertyPageControlTests
    {
        [Fact]
        public void Test()
        {

            var thread = new Thread(new ThreadStart(CallPropertyPageControl));

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();


        }

        private void CallPropertyPageControl()
        {
            var ppvm = new Mock<PropertyPageViewModel>();
            ppvm.Setup(m => m.Save()).ReturnsAsync(VSConstants.S_OK);
            ppvm.Setup(m => m.Initialize()).Returns(new Task(() => { }));
            ppvm.CallBase = true;

            var ppc = new Mock<PropertyPageControl>(MockBehavior.Loose)
            {
                CallBase = true
            };

            ppc.Object.InitializePropertyPage(ppvm.Object);
            ppc.Object.IsDirty = true;
            int result = ppc.Object.Apply().Result;

            Assert.Equal(VSConstants.S_OK, result);
            Assert.Equal(ppvm.Object, ppc.Object.ViewModel);
            Assert.Equal(ppvm.Object, ppc.Object.DataContext);
            ppc.Protected().Verify("OnApply", Times.Once());
            ppc.Protected().Verify("OnStatusChanged", Times.Exactly(2), ItExpr.IsAny<EventArgs>());
            ppvm.Verify(m => m.Save(), Times.Once());
        }
    }
}
