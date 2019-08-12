﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

using Microsoft.VisualStudio.ProjectSystem.Utilities;

using Moq;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{

    public class DebugTokenReplacerTests
    {
        private readonly Dictionary<string, string> _envVars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            { "%env1%","envVariable1" },
            { "%env2%","envVariable2" },
            { "%env3%","$(msbuildProperty6)" }
        };
        private readonly Mock<IEnvironmentHelper> _envHelper = new Mock<IEnvironmentHelper>();
        public DebugTokenReplacerTests()
        {
            _envHelper.Setup(x => x.ExpandEnvironmentVariables(It.IsAny<string>())).Returns<string>((str) =>
            {
                foreach (var kv in _envVars)
                {
                    str = str.Replace(kv.Key, kv.Value);
                }
                return str;
            });
        }

        [Fact]
        public async Task ReplaceTokensInProfileTests()
        {
            var replacer = CreateInstance();

            // Tests all the possible replacements. env3 tests that environment vars are resolved before msbuild tokens
            var launchProfile = new LaunchProfile()
            {
                Name = "$(msbuildProperty1)",
                CommandLineArgs = "%env1%",
                CommandName = "$(msbuildProperty2)",
                ExecutablePath = "$(test this string",  // Not a valid token
                WorkingDirectory = "c:\\test\\%env3%",
                LaunchBrowser = false,
                LaunchUrl = "http://localhost:8080/$(unknownproperty)",
                EnvironmentVariables = ImmutableStringDictionary<string>.EmptyOrdinal.Add("var1", "%env1%").Add("var2", "$(msbuildProperty3)"),
                OtherSettings = ImmutableStringDictionary<object>.EmptyOrdinal.Add("setting1", "%env1%").Add("setting2", true),
            };

            var resolvedProfile = await replacer.ReplaceTokensInProfileAsync(launchProfile);

            // Name and Command name should never be touched
            Assert.Equal("$(msbuildProperty1)", resolvedProfile.Name);
            Assert.Equal("$(msbuildProperty2)", resolvedProfile.CommandName);
            Assert.Equal("envVariable1", resolvedProfile.CommandLineArgs);
            Assert.Equal("$(test this string", resolvedProfile.ExecutablePath);
            Assert.False(resolvedProfile.LaunchBrowser);
            Assert.Equal("http://localhost:8080/", resolvedProfile.LaunchUrl);
            Assert.Equal("c:\\test\\Property6", resolvedProfile.WorkingDirectory);
            Assert.Equal("envVariable1", resolvedProfile.EnvironmentVariables["var1"]);
            Assert.Equal("Property3", resolvedProfile.EnvironmentVariables["var2"]);
            Assert.Equal("envVariable1", resolvedProfile.OtherSettings["setting1"]);
            Assert.True((bool)resolvedProfile.OtherSettings["setting2"]);
        }

        [Theory]
        [InlineData("this is msbuild: $(msbuildProperty5) %env1%",                      "this is msbuild: Property5 envVariable1", true)]
        [InlineData("this is msbuild: $(msbuildProperty5) %env1%",                      "this is msbuild: Property5 %env1%", false)]
        [InlineData("this is msbuild: $(UnknownMsbuildProperty) %env1%",                "this is msbuild:  envVariable1", true)]
        [InlineData("this is msbuild: $(UnknownMsbuildProperty) %Unknown%",             "this is msbuild:  %Unknown%", true)]
        [InlineData("this is msbuild: %env3% $(msbuildProperty2) $(msbuildProperty3)",  "this is msbuild: Property6 Property2 Property3", true)]
        [InlineData(null, null, true)]
        [InlineData(" ", " ", true)]
        public async Task ReplaceTokensInStringTests(string input, string expected, bool expandEnvVars)
        {
            var replacer = CreateInstance();

            // Test msbuild vars
            string result = await replacer.ReplaceTokensInStringAsync(input, expandEnvVars);
            Assert.Equal(expected, result);
        }

        private DebugTokenReplacer CreateInstance()
        {
            var environmentHelper = _envHelper.Object;
            var activeDebugFramework = Mock.Of<IActiveDebugFrameworkServices>();

            string projectFile = @"<Project>
                <PropertyGroup>
                    <msbuildProperty1>Property1</msbuildProperty1>
                    <msbuildProperty2>Property2</msbuildProperty2>
                    <msbuildProperty3>Property3</msbuildProperty3>
                    <msbuildProperty4>Property4</msbuildProperty4>
                    <msbuildProperty5>Property5</msbuildProperty5>
                    <msbuildProperty6>Property6</msbuildProperty6>
                </PropertyGroup>
                </Project>";

            return new DebugTokenReplacer(environmentHelper, activeDebugFramework, IProjectAccessorFactory.Create(projectFile));
        }
    }
}
