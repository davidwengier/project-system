﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot.Filters;

using Xunit;

#nullable disable

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot
{
    public sealed class DuplicatedDependenciesSnapshotFilterTests
    {
        [Fact]
        public void BeforeAddOrUpdate_NoDuplicate_ShouldNotUpdateCaption()
        {
            // Both top level
            // Same provider type
            // Different captions
            //   -> No change

            const string providerType = "provider";

            var dependency = new TestDependency
            {
                Id = "dependency1",
                Caption = "caption1",
                ProviderType = providerType,
                TopLevel = true
            };

            var otherDependency = new TestDependency
            {
                Id = "dependency2",
                Caption = "caption2",
                ProviderType = providerType,
                TopLevel = true
            };

            var worldBuilder = new IDependency[] { dependency, otherDependency }.ToImmutableDictionary(d => d.Id).ToBuilder();

            var context = new AddDependencyContext(worldBuilder);

            var filter = new DuplicatedDependenciesSnapshotFilter();

            filter.BeforeAddOrUpdate(
                null,
                dependency,
                null,
                null,
                context);

            // Accepts unchanged dependency
            Assert.Same(dependency, context.GetResult(filter));

            // No other changes made
            Assert.False(context.Changed);
        }

        [Fact]
        public void BeforeAddOrUpdate_WhenThereIsMatchingDependencies_ShouldUpdateCaptionForAll()
        {
            // Both top level
            // Same provider type
            // Same captions
            //   -> Changes caption for both to match alias

            const string providerType = "provider";
            const string caption = "caption1";

            var dependency = new TestDependency
            {
                Id = "dependency1",
                Alias = "dependency1 (dependency1ItemSpec)",
                ProviderType = providerType,
                Caption = caption,
                TopLevel = true
            };

            var otherDependency = new TestDependency
            {
                ClonePropertiesFrom = dependency, // clone, with changes

                Id = "dependency2",
                Alias = "dependency2 (dependency2ItemSpec)"
            };

            var worldBuilder = new IDependency[] { dependency, otherDependency }.ToImmutableDictionary(d => d.Id).ToBuilder();

            var context = new AddDependencyContext(worldBuilder);

            var filter = new DuplicatedDependenciesSnapshotFilter();

            filter.BeforeAddOrUpdate(
                null,
                dependency,
                null,
                null,
                context);

            // The context changed, beyond just the filtered dependency
            Assert.True(context.Changed);

            // The filtered dependency had its caption changed to its alias
            var dependencyAfter = context.GetResult(filter);
            dependencyAfter.AssertEqualTo(
                new TestDependency { ClonePropertiesFrom = dependency, Caption = dependency.Alias });

            // The other dependency had its caption changed to its alias
            Assert.True(context.TryGetDependency(otherDependency.Id, out IDependency otherDependencyAfter));
            otherDependencyAfter.AssertEqualTo(
                new TestDependency { ClonePropertiesFrom = otherDependency, Caption = otherDependency.Alias });
        }

        [Fact]
        public void BeforeAddOrUpdate_WhenThereIsMatchingDependencyWithAliasApplied_ShouldUpdateCaptionForCurrentDependency()
        {
            // Both top level
            // Same provider type
            // Duplicate caption, though with parenthesized text after one instance
            //   -> Changes caption of non-parenthesized

            const string providerType = "provider";
            const string caption = "caption";

            var dependency = new TestDependency
            {
                Id = "dependency1",
                Alias = "dependency1 (dependency1ItemSpec)",
                ProviderType = providerType,
                Caption = caption,
                TopLevel = true
            };

            var otherDependency = new TestDependency
            {
                ClonePropertiesFrom = dependency,

                Id = "dependency2",
                OriginalItemSpec = "dependency2ItemSpec",
                Caption = $"{caption} (dependency2ItemSpec)" // caption already includes alias
            };

            var worldBuilder = new IDependency[] { dependency, otherDependency }.ToImmutableDictionary(d => d.Id).ToBuilder();

            var context = new AddDependencyContext(worldBuilder);

            var filter = new DuplicatedDependenciesSnapshotFilter();

            filter.BeforeAddOrUpdate(
                null,
                dependency,
                null,
                null,
                context);

            // The context was unchanged, beyond the filtered dependency
            Assert.False(context.Changed);

            // The filtered dependency had its caption changed to its alias
            var dependencyAfter = context.GetResult(filter);
            dependencyAfter.AssertEqualTo(
                new TestDependency { ClonePropertiesFrom = dependency, Caption = dependency.Alias });
        }

        [Fact]
        public void BeforeAddOrUpdate_WhenThereIsMatchingDependency_WithSubstringCaption()
        {
            // Both top level
            // Same provider type
            // Duplicate caption prefix
            //   -> No change

            const string providerType = "provider";
            const string caption = "caption";

            var dependency = new TestDependency
            {
                Id = "dependency1",
                ProviderType = providerType,
                Caption = caption,
                TopLevel = true
            };

            var otherDependency = new TestDependency
            {
                ClonePropertiesFrom = dependency,

                Id = "dependency2",
                OriginalItemSpec = "dependency2ItemSpec",
                Caption = $"{caption}X" // identical caption prefix
            };

            // TODO test a longer suffix here -- looks like the implementation might not handle it correctly

            var worldBuilder = new IDependency[] { dependency, otherDependency }.ToImmutableDictionary(d => d.Id).ToBuilder();

            var context = new AddDependencyContext(worldBuilder);

            var filter = new DuplicatedDependenciesSnapshotFilter();

            filter.BeforeAddOrUpdate(
                null,
                dependency,
                null,
                null,
                context);

            // Accepts unchanged dependency
            Assert.Same(dependency, context.GetResult(filter));

            // No other changes made
            Assert.False(context.Changed);
        }
    }
}
