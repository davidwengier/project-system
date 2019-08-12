﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Build.Construction;
using Microsoft.VisualStudio.Build;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Configuration
{
    public class ConfigurationProjectConfigurationDimensionProviderTests
    {
        private const string Configurations = nameof(Configurations);

        private readonly string projectXml =
@"<Project>
  <PropertyGroup>
    <Configurations>Debug;Release;CustomConfiguration</Configurations>
  </PropertyGroup>
</Project>";

        [Fact]
        public async Task GetDefaultValuesForDimensionsAsync()
        {
            var projectAccessor = IProjectAccessorFactory.Create(projectXml);
            var provider = new ConfigurationProjectConfigurationDimensionProvider(projectAccessor);

            var project = UnconfiguredProjectFactory.Create();

            var values = await provider.GetDefaultValuesForDimensionsAsync(project);

            Assert.Single(values);
            var value = values.First();
            Assert.Equal(ConfigurationGeneral.ConfigurationProperty, value.Key);
            Assert.Equal("Debug", value.Value);
        }

        [Fact]
        public async Task GetDefaultValuesForDimensionsAsync_NoPropertyValue()
        {
            var projectAccessor = IProjectAccessorFactory.Create("<Project />");
            var provider = new ConfigurationProjectConfigurationDimensionProvider(projectAccessor);

            var project = UnconfiguredProjectFactory.Create();

            var values = await provider.GetDefaultValuesForDimensionsAsync(project);

            Assert.Empty(values);
        }

        [Fact]
        public async Task GetProjectConfigurationDimensionsAsync()
        {
            var projectAccessor = IProjectAccessorFactory.Create(projectXml);
            var provider = new ConfigurationProjectConfigurationDimensionProvider(projectAccessor);

            var project = UnconfiguredProjectFactory.Create();

            var values = await provider.GetProjectConfigurationDimensionsAsync(project);

            Assert.Single(values);
            var value = values.First();
            Assert.Equal(ConfigurationGeneral.ConfigurationProperty, value.Key);
            string[] dimensionValues = value.Value.ToArray();
            Assert.Equal(3, dimensionValues.Length);
            Assert.Equal("Debug", dimensionValues[0]);
            Assert.Equal("Release", dimensionValues[1]);
            Assert.Equal("CustomConfiguration", dimensionValues[2]);
        }

        [Fact]
        public async Task GetProjectConfigurationDimensionsAsync_NoPropertyValue()
        {
            var projectAccessor = IProjectAccessorFactory.Create("<Project />");

            var provider = new ConfigurationProjectConfigurationDimensionProvider(projectAccessor);


            var project = UnconfiguredProjectFactory.Create();
            var values = await provider.GetProjectConfigurationDimensionsAsync(project);

            Assert.Empty(values);
        }

        [Fact]
        public async Task OnDimensionValueChanged_Add()
        {
            var rootElement = ProjectRootElementFactory.Create(projectXml);
            var projectAccessor = IProjectAccessorFactory.Create(rootElement);

            var provider = new ConfigurationProjectConfigurationDimensionProvider(projectAccessor);

            var project = UnconfiguredProjectFactory.Create();

            // On ChangeEventStage.After nothing should be changed
            var args = new ProjectConfigurationDimensionValueChangedEventArgs(
                project,
                ConfigurationDimensionChange.Add,
                ChangeEventStage.After,
                ConfigurationGeneral.ConfigurationProperty,
                "CustomConfig");
            await provider.OnDimensionValueChangedAsync(args);
            var property = BuildUtilities.GetProperty(rootElement, Configurations);
            Assert.NotNull(property);
            Assert.Equal("Debug;Release;CustomConfiguration", property!.Value);

            // On ChangeEventStage.Before the property should be added
            args = new ProjectConfigurationDimensionValueChangedEventArgs(
                project,
                ConfigurationDimensionChange.Add,
                ChangeEventStage.Before,
                ConfigurationGeneral.ConfigurationProperty,
                "CustomConfig");
            await provider.OnDimensionValueChangedAsync(args);
            property = BuildUtilities.GetProperty(rootElement, Configurations);
            Assert.NotNull(property);
            Assert.Equal("Debug;Release;CustomConfiguration;CustomConfig", property!.Value);
        }

        [Fact]
        public async Task OnDimensionValueChanged_Remove()
        {
            var rootElement = ProjectRootElementFactory.Create(projectXml);
            var projectAccessor = IProjectAccessorFactory.Create(rootElement);

            var provider = new ConfigurationProjectConfigurationDimensionProvider(projectAccessor);

            var project = UnconfiguredProjectFactory.Create();

            // On ChangeEventStage.After nothing should be changed
            var args = new ProjectConfigurationDimensionValueChangedEventArgs(
                project,
                ConfigurationDimensionChange.Delete,
                ChangeEventStage.After,
                ConfigurationGeneral.ConfigurationProperty,
                "CustomConfiguration");
            await provider.OnDimensionValueChangedAsync(args);
            var property = BuildUtilities.GetProperty(rootElement, Configurations);
            Assert.NotNull(property);
            Assert.Equal("Debug;Release;CustomConfiguration", property!.Value);

            // On ChangeEventStage.Before the property should be removed
            args = new ProjectConfigurationDimensionValueChangedEventArgs(
                project,
                ConfigurationDimensionChange.Delete,
                ChangeEventStage.Before,
                ConfigurationGeneral.ConfigurationProperty,
                "CustomConfiguration");
            await provider.OnDimensionValueChangedAsync(args);
            property = BuildUtilities.GetProperty(rootElement, Configurations);
            Assert.NotNull(property);
            Assert.Equal("Debug;Release", property!.Value);
        }

        [Fact]
        public async Task OnDimensionValueChanged_Remove_MissingValue()
        {
            var rootElement = ProjectRootElementFactory.Create(projectXml);
            var projectAccessor = IProjectAccessorFactory.Create(rootElement);

            var provider = new ConfigurationProjectConfigurationDimensionProvider(projectAccessor);

            var project = UnconfiguredProjectFactory.Create();

            var args = new ProjectConfigurationDimensionValueChangedEventArgs(
                project,
                ConfigurationDimensionChange.Delete,
                ChangeEventStage.Before,
                ConfigurationGeneral.ConfigurationProperty,
                "NonExistantConfiguration");
            await Assert.ThrowsAsync<ArgumentException>(() => provider.OnDimensionValueChangedAsync(args));
            var property = BuildUtilities.GetProperty(rootElement, Configurations);
            Assert.NotNull(property);
            Assert.Equal("Debug;Release;CustomConfiguration", property!.Value);
        }

        [Fact]
        public async Task OnDimensionValueChanged_Rename()
        {
            var rootElement = ProjectRootElementFactory.Create(projectXml);
            var projectAccessor = IProjectAccessorFactory.Create(rootElement);

            var provider = new ConfigurationProjectConfigurationDimensionProvider(projectAccessor);

            var project = UnconfiguredProjectFactory.Create();

            // On ChangeEventStage.Before nothing should be changed
            var args = new ProjectConfigurationDimensionValueChangedEventArgs(
                project,
                ConfigurationDimensionChange.Rename,
                ChangeEventStage.Before,
                ConfigurationGeneral.ConfigurationProperty,
                "RenamedConfiguration",
                "CustomConfiguration");
            await provider.OnDimensionValueChangedAsync(args);
            var property = BuildUtilities.GetProperty(rootElement, Configurations);
            Assert.NotNull(property);
            Assert.Equal("Debug;Release;CustomConfiguration", property!.Value);

            // On ChangeEventStage.Before the property should be renamed
            args = new ProjectConfigurationDimensionValueChangedEventArgs(
                project,
                ConfigurationDimensionChange.Rename,
                ChangeEventStage.After,
                ConfigurationGeneral.ConfigurationProperty,
                "RenamedConfiguration",
                "CustomConfiguration");
            await provider.OnDimensionValueChangedAsync(args);
            property = BuildUtilities.GetProperty(rootElement, Configurations);
            Assert.NotNull(property);
            Assert.Equal("Debug;Release;RenamedConfiguration", property!.Value);
        }

        [Fact]
        public async Task OnDimensionValueChanged_Rename_MissingValue()
        {
            var rootElement = ProjectRootElementFactory.Create(projectXml);
            var projectAccessor = IProjectAccessorFactory.Create(rootElement);

            var provider = new ConfigurationProjectConfigurationDimensionProvider(projectAccessor);

            var project = UnconfiguredProjectFactory.Create();

            var args = new ProjectConfigurationDimensionValueChangedEventArgs(
                project,
                ConfigurationDimensionChange.Rename,
                ChangeEventStage.After,
                ConfigurationGeneral.ConfigurationProperty,
                "RenamedConfiguration",
                "NonExistantConfiguration");
            await Assert.ThrowsAsync<ArgumentException>(() => provider.OnDimensionValueChangedAsync(args));
            var property = BuildUtilities.GetProperty(rootElement, Configurations);
            Assert.NotNull(property);
            Assert.Equal("Debug;Release;CustomConfiguration", property!.Value);
        }

        [Theory]
        [InlineData("Dbg",                      "Dbg")]
        [InlineData(" Dbg ",                    "Dbg")]
        [InlineData("Retail",                   "Retail")]
        [InlineData("Dbg;",                     "Dbg")]
        [InlineData("Dbg;Retail",               "Dbg")]
        [InlineData(";Dbg;Retail",              "Dbg")]
        [InlineData("$(Foo);Dbg;Retail",        "Dbg")]
        [InlineData("$(Foo); Dbg ;Retail",      "Dbg")]
        [InlineData("Dbg_$(Foo); Dbg ;Retail",  "Dbg")]
        public async Task GetBestGuessDefaultValuesForDimensionsAsync_ReturnsFirstParseableValue(string configurations, string expected)
        {
            string projectXml =
$@"<Project>
  <PropertyGroup>
    <Configurations>{configurations}</Configurations>
  </PropertyGroup>
</Project>";

            var provider = CreateInstance(projectXml);

            var result = await provider.GetBestGuessDefaultValuesForDimensionsAsync(UnconfiguredProjectFactory.Create());

            Assert.Single(result);
            Assert.Equal("Configuration", result.First().Key);
            Assert.Equal(expected, result.First().Value);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(";")]
        [InlineData(" ;")]
        [InlineData(" ; ")]
        [InlineData(";;;")]
        [InlineData("$(Property)")]
        [InlineData("Foo_$(Property)")]
        [InlineData("Foo_$(Property);")]
        [InlineData(";Foo_$(Property);")]
        public async Task GetBestGuessDefaultValuesForDimensionsAsync_WhenConfigurationsIsEmpty_ReturnsDefault(string configurations)
        {
            string projectXml =
$@"<Project>
  <PropertyGroup>
    <Configurations>{configurations}</Configurations>
  </PropertyGroup>
</Project>";

            var provider = CreateInstance(projectXml);

            var result = await provider.GetBestGuessDefaultValuesForDimensionsAsync(UnconfiguredProjectFactory.Create());

            Assert.Single(result);
            Assert.Equal("Configuration", result.First().Key);
            Assert.Equal("Debug", result.First().Value);
        }

        [Fact]
        public async Task GetBestGuessDefaultValuesForDimensionsAsync_ReturnsFirstValueFromLastConfigurationsElement()
        {
            string projectXml =
$@"<Project>
  <PropertyGroup>
    <Configurations>Dbg</Configurations>
    <Configurations>Retail;Dbg</Configurations>
  </PropertyGroup>
</Project>";

            var provider = CreateInstance(projectXml);

            var result = await provider.GetBestGuessDefaultValuesForDimensionsAsync(UnconfiguredProjectFactory.Create());

            Assert.Single(result);
            Assert.Equal("Configuration", result.First().Key);
            Assert.Equal("Retail", result.First().Value);
        }

        [Fact]
        public async Task GetBestGuessDefaultValuesForDimensionsAsync_WhenConfigurationsIsMissing_ReturnsDefault()
        {
            string projectXml =
$@"<Project>
  <PropertyGroup>
  </PropertyGroup>
</Project>";

            var provider = CreateInstance(projectXml);

            var result = await provider.GetBestGuessDefaultValuesForDimensionsAsync(UnconfiguredProjectFactory.Create());

            Assert.Single(result);
            Assert.Equal("Configuration", result.First().Key);
            Assert.Equal("Debug", result.First().Value);
        }

        [Theory]
        [InlineData(
@"<Project>
  <PropertyGroup>
    <Configurations Condition=""'$(BuildingInsideVisualStudio)' != 'true'"">Dbg</Configurations>
  </PropertyGroup>
</Project>")]
        [InlineData(
@"<Project>
  <PropertyGroup>
    <Configurations Condition=""'$(OS)' != 'Windows_NT'"">Dbg</Configurations>
  </PropertyGroup>
</Project>")]
        [InlineData(
@"<Project>
  <PropertyGroup>
    <Configurations Condition=""'$(OS)' == 'Unix'"">Dbg</Configurations>
  </PropertyGroup>
</Project>")]
        [InlineData(
@"<Project>
  <PropertyGroup>
    <Configurations Condition=""'$(Foo)' == 'true'"">Dbg</Configurations>
  </PropertyGroup>
</Project>")]
        [InlineData(
@"<Project>
  <PropertyGroup>
    <Configuration>Dbg</Configuration>
    <Configurations Condition=""'$(OS)' != 'Windows_NT'"">Dbg</Configurations>
  </PropertyGroup>
</Project>")]
        public async Task GetBestGuessDefaultValuesForDimensionsAsync_WhenConfigurationsHasUnrecognizedCondition_ReturnsDefault(string projectXml)
        {
            var provider = CreateInstance(projectXml);

            var result = await provider.GetBestGuessDefaultValuesForDimensionsAsync(UnconfiguredProjectFactory.Create());

            Assert.Single(result);
            Assert.Equal("Configuration", result.First().Key);
            Assert.Equal("Debug", result.First().Value);
        }


        [Theory]
        [InlineData(
@"<Project>
  <PropertyGroup>
    <Configurations Condition=""'$(BuildingInsideVisualStudio)' == 'true'"">Dbg</Configurations>
    <Configurations Condition=""'$(BuildingInsideVisualStudio)' != 'true'"">Retail</Configurations>
  </PropertyGroup>
</Project>")]
        [InlineData(
@"<Project>
  <PropertyGroup>
    <Configurations Condition=""'$(OS)' == 'Windows_NT'"">Dbg</Configurations>
    <Configurations Condition=""'$(OS)' != 'Windows_NT'"">Retail</Configurations>
  </PropertyGroup>
</Project>")]
        [InlineData(
@"<Project>
  <PropertyGroup>
    <Configurations Condition=""'$(OS)' == 'Windows_NT'"">Dbg</Configurations>
    <Configurations Condition=""'$(OS)' == 'Unix'"">Retail</Configurations>
  </PropertyGroup>
</Project>")]
        [InlineData(
@"<Project>
  <PropertyGroup>
    <Configurations Condition=""true"">Dbg</Configurations>
  </PropertyGroup>
</Project>")]
        [InlineData(
@"<Project>
  <PropertyGroup>
    <Configurations Condition="""">Dbg</Configurations>
  </PropertyGroup>
</Project>")]
        public async Task GetBestGuessDefaultValuesForDimensionsAsync_WhenConfigurationsHasRecognizedCondition_ReturnsValue(string projectXml)
        {
            var provider = CreateInstance(projectXml);

            var result = await provider.GetBestGuessDefaultValuesForDimensionsAsync(UnconfiguredProjectFactory.Create());

            Assert.Single(result);
            Assert.Equal("Configuration", result.First().Key);
            Assert.Equal("Dbg", result.First().Value);
        }

        private static ConfigurationProjectConfigurationDimensionProvider CreateInstance(string projectXml)
        {
            var projectAccessor = IProjectAccessorFactory.Create(projectXml);

            return new ConfigurationProjectConfigurationDimensionProvider(projectAccessor);
        }
    }
}
