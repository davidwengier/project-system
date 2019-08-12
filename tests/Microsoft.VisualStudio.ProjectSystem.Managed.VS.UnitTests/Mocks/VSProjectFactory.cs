﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;

using EnvDTE;

using Moq;

#nullable disable

namespace VSLangProj
{
    internal static class VSProjectFactory
    {
        public static VSProject ImplementReferences(IEnumerable<Reference> references)
        {
            var vsProjectReferences = new VSProjectReferences(references);

            var mock = new Mock<VSProject>();
            mock.SetupGet(p => p.References)
                .Returns(vsProjectReferences);

            return mock.Object;
        }

        private class VSProjectReferences : References
        {
            private readonly IEnumerable<Reference> _references;

            public VSProjectReferences(IEnumerable<Reference> references)
            {
                _references = references;
            }

            public Reference Item(object index)
            {
                throw new NotImplementedException();
            }

            public IEnumerator GetEnumerator()
            {
                return _references.GetEnumerator();
            }

            public Reference Find(string bstrIdentity)
            {
                throw new NotImplementedException();
            }

            public Reference Add(string bstrPath)
            {
                throw new NotImplementedException();
            }

            public Reference AddActiveX(string bstrTypeLibGuid, int lMajorVer = 0, int lMinorVer = 0, int lLocaleId = 0, string bstrWrapperTool = "")
            {
                throw new NotImplementedException();
            }

            public Reference AddProject(Project pProject)
            {
                throw new NotImplementedException();
            }

            public DTE DTE
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public object Parent
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public Project ContainingProject
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public int Count
            {
                get
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}
