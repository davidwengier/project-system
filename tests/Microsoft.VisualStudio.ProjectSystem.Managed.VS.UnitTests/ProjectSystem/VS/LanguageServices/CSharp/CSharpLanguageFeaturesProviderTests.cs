﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

using Xunit;

#nullable disable

namespace Microsoft.VisualStudio.ProjectSystem.VS.LanguageServices.CSharp
{
    public class CSharpLanguageFeaturesProviderTests
    {
        [Fact]
        public void Constructor_DoesNotThrow()
        {
            new CSharpLanguageFeaturesProvider();
        }

        [Fact]
        public void MakeProperIdentifier_NullAsName_ThrowsArgumentNull()
        {
            var provider = CreateInstance();

            Assert.Throws<ArgumentNullException>("name", () =>
            {
                provider.MakeProperIdentifier((string)null);
            });
        }

        [Fact]
        public void MakeProperIdentifier_EmptyAsName_ThrowsArgument()
        {
            var provider = CreateInstance();

            Assert.Throws<ArgumentException>("name", () =>
            {
                provider.MakeProperIdentifier("");
            });
        }

        [Fact]
        public void MakeProperNamespace_NullAsName_ThrowsArgumentNull()
        {
            var provider = CreateInstance();

            Assert.Throws<ArgumentNullException>("name", () =>
            {
                provider.MakeProperNamespace((string)null);
            });
        }

        [Fact]
        public void MakeProperNamespace_EmptyAsName_ThrowsArgument()
        {
            var provider = CreateInstance();

            Assert.Throws<ArgumentException>("name", () =>
            {
                provider.MakeProperNamespace("");
            });
        }

        [Theory] // Input                                           // Expected
        [InlineData("_" ,                                           "_")]
        [InlineData("_._",                                           "_._")]
        [InlineData("A" ,                                           "A")]
        [InlineData(" " ,                                           "_")]
        [InlineData("  " ,                                          "__")]
        [InlineData("A." ,                                          "A")]
        [InlineData("A123" ,                                        "A123")]
        [InlineData("A<T>" ,                                        "A_T_")]
        [InlineData("A_1" ,                                         "A_1")]
        [InlineData("A.B",                                          "A.B")]
        [InlineData("Microsoft.VisualStudio.ProjectSystem",         "Microsoft.VisualStudio.ProjectSystem")]
        [InlineData(".A",                                           "A")]
        [InlineData(".A.",                                          "A")]
        [InlineData("..A.",                                         "A")]
        [InlineData("A....A.",                                      "A.A")]
        [InlineData("A B",                                          "A_B")]
        [InlineData("1.2",                                          "_1._2")]
        [InlineData("A.B and C",                                    "A.B_and_C")]
        [InlineData("A,B and C",                                    "A_B_and_C")]
        [InlineData("\u1234",                                       "\u1234")]
        [InlineData("\u00C6sh",                                     "\u00C6sh")]
        [InlineData("my\u034Fvery\u034Flong\u034Fidentifier",       "my\u034Fvery\u034Flong\u034Fidentifier")]           // COMBINING GRAPHEME JOINERs, not actual spaces
        public void MakeProperNamespace_ValuesAsName_ReturnsProperNamespace(string name, string expected)
        {
            var provider = CreateInstance();

            string result = provider.MakeProperNamespace(name);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void ConcatNamespaces_NullAsNamespaceNames_ThrowsArgumentNull()
        {
            var provider = CreateInstance();

            Assert.Throws<ArgumentNullException>("namespaceNames", () =>
            {
                provider.ConcatNamespaces((string[])null);
            });
        }

        [Fact]
        public void ConcatNamespaces_EmptyArrayAsNamespaceNames_ThrowsArgument()
        {
            var provider = CreateInstance();

            Assert.Throws<ArgumentException>("namespaceNames", () =>
            {
                provider.ConcatNamespaces(new string[0]);
            });
        }

        [Fact]
        public void ConcatNamespaces_ArrayWithNullAsNamespaceNames_ThrowsArgument()
        {
            var provider = CreateInstance();

            Assert.Throws<ArgumentException>("namespaceNames", () =>
            {
                provider.ConcatNamespaces(new string[] { null });
            });
        }

        [Theory]         // Input                                                                    // Expected
        [InlineData(new[] { "" },                                                                       "")]
        [InlineData(new[] { "A" },                                                                      "A")]
        [InlineData(new[] { "A", "B"},                                                                  "A.B")]
        [InlineData(new[] { "A", "B", "C"},                                                             "A.B.C")]
        [InlineData(new[] { "Microsoft", "VisualStudio", "ProjectSystem"},                              "Microsoft.VisualStudio.ProjectSystem")]
        [InlineData(new[] { "Microsoft.VisualStudio", "ProjectSystem"},                                 "Microsoft.VisualStudio.ProjectSystem")]
        [InlineData(new[] { "", "Microsoft", "VisualStudio", "ProjectSystem"},                          "Microsoft.VisualStudio.ProjectSystem")]
        [InlineData(new[] { "Microsoft", "", "ProjectSystem"},                                          "Microsoft.ProjectSystem")]
        public void ConcatNamespaces_ValuesAsNamespacesName_ReturnsConcatenatedNamespaces(string[] namespaceNames, string expected)
        {
            var provider = CreateInstance();

            string result = provider.ConcatNamespaces(namespaceNames);

            Assert.Equal(expected, result);
        }

        private static CSharpLanguageFeaturesProvider CreateInstance()
        {
            return new CSharpLanguageFeaturesProvider();
        }
    }
}
