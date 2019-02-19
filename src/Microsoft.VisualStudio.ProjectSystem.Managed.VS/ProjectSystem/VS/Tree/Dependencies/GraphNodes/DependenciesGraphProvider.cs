﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.GraphModel.CodeSchema;
using Microsoft.VisualStudio.GraphModel.Schemas;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.GraphNodes.Actions;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.GraphNodes
{
    /// <summary>
    /// Provides actual dependencies nodes under Dependencies\[DependencyType]\[TopLevel]\[....] sub nodes. 
    /// Note: when dependency has <see cref="ProjectTreeFlags.Common.BrokenReference"/> flag,
    /// <see cref="IGraphProvider"/> API are not called for that node.
    /// </summary>
    [Export(typeof(DependenciesGraphProvider))]
    [Export(typeof(IDependenciesGraphBuilder))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal sealed class DependenciesGraphProvider : OnceInitialisedOnceDisposedAsync, IGraphProvider, IDependenciesGraphBuilder
    {
        /// <summary>The set of commands this provider supports.</summary>
        private static readonly GraphCommand[] s_commands =
        {
            // The "Contains" command finds a graph node's children
            new GraphCommand(
                GraphCommandDefinition.Contains,
                targetCategories: null,
                linkCategories: new[] {GraphCommonSchema.Contains},
                trackChanges: true)
        };

        private readonly IAsyncServiceProvider _serviceProvider;

        private readonly IDependenciesGraphChangeTracker _changeTracker;

        [ImportMany] private readonly OrderPrecedenceImportCollection<IDependenciesGraphActionHandler> _graphActionHandlers;

        private GraphIconCache _iconCache;

        [ImportingConstructor]
        public DependenciesGraphProvider(
            IDependenciesGraphChangeTracker changeTracker,
            [Import(typeof(SAsyncServiceProvider))] IAsyncServiceProvider serviceProvider,
            JoinableTaskContext joinableTaskContext)
            : base(new JoinableTaskContextNode(joinableTaskContext))
        {
            _serviceProvider = serviceProvider;
            _changeTracker = changeTracker;

            _graphActionHandlers = new OrderPrecedenceImportCollection<IDependenciesGraphActionHandler>(
                ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesLast);
        }

        protected override async Task InitialiseCoreAsync(CancellationToken cancellationToken)
        {
            _iconCache = await GraphIconCache.CreateAsync(_serviceProvider);
        }

        protected override Task DisposeCoreAsync(bool Initialised)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Entry point for progression. Gets called every time when progression
        ///  - Needs to know if a node has children
        ///  - Wants to get children for a node
        ///  - During solution explorer search
        /// </summary>
        public void BeginGetGraphData(IGraphContext context)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                try
                {
                    await InitialiseAsync();

                    foreach (Lazy<IDependenciesGraphActionHandler, IOrderPrecedenceMetadataView> handler in _graphActionHandlers)
                    {
                        if (handler.Value.TryHandleRequest(context))
                        {
                            _changeTracker.RegisterGraphContext(context);

                            // Only one handler should succeed
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    context.ReportError(ex);
                }
                finally
                {
                    // OnCompleted must be called to display changes 
                    context.OnCompleted();
                }
            });
        }

        public IEnumerable<GraphCommand> GetCommands(IEnumerable<GraphNode> nodes) => s_commands;

        public T GetExtension<T>(GraphObject graphObject, T previous) where T : class => null;

        public Graph Schema => null;

        public GraphNode AddGraphNode(
            IGraphContext graphContext,
            string projectPath,
            GraphNode parentNode,
            IDependencyViewModel viewModel)
        {
            Assumes.True(IsInitialised);

            string modelId = viewModel.OriginalModel == null ? viewModel.Caption : viewModel.OriginalModel.Id;
            GraphNodeId newNodeId = GetGraphNodeId(projectPath, parentNode, modelId);
            return DoAddGraphNode(newNodeId, graphContext, projectPath, parentNode, viewModel);
        }

        public GraphNode AddTopLevelGraphNode(
            IGraphContext graphContext,
            string projectPath,
            IDependencyViewModel viewModel)
        {
            Requires.NotNull(viewModel.OriginalModel, nameof(viewModel.OriginalModel));

            Assumes.True(IsInitialised);

            GraphNodeId newNodeId = GetTopLevelGraphNodeId(projectPath, viewModel.OriginalModel.GetTopLevelId());
            return DoAddGraphNode(newNodeId, graphContext, projectPath, parentNode: null, viewModel);
        }

        private GraphNode DoAddGraphNode(
            GraphNodeId graphNodeId,
            IGraphContext graphContext,
            string projectPath,
            GraphNode parentNode,
            IDependencyViewModel viewModel)
        {
            _iconCache.Register(viewModel.Icon);
            _iconCache.Register(viewModel.ExpandedIcon);

            GraphNode newNode = graphContext.Graph.Nodes.GetOrCreate(graphNodeId, label: viewModel.Caption, category: DependenciesGraphSchema.CategoryDependency);

            newNode.SetValue(DgmlNodeProperties.Icon, _iconCache.GetName(viewModel.Icon));

            // priority sets correct order among peers
            newNode.SetValue(CodeNodeProperties.SourceLocation, new SourceLocation(projectPath, new Position(viewModel.Priority, 0)));

            if (viewModel.OriginalModel != null)
            {
                newNode.SetValue(DependenciesGraphSchema.DependencyIdProperty, viewModel.OriginalModel.Id);
                newNode.SetValue(DependenciesGraphSchema.ResolvedProperty, viewModel.OriginalModel.Resolved);
            }

            graphContext.OutputNodes.Add(newNode);

            if (parentNode != null)
            {
                graphContext.Graph.Links.GetOrCreate(parentNode, newNode, label: null, CodeLinkCategories.Contains);
            }

            return newNode;
        }

        public void RemoveGraphNode(
            IGraphContext graphContext,
            string projectPath,
            string modelId,
            GraphNode parentNode)
        {
            Assumes.True(IsInitialised);

            GraphNodeId id = GetGraphNodeId(projectPath, parentNode, modelId);
            GraphNode nodeToRemove = graphContext.Graph.Nodes.Get(id);

            if (nodeToRemove != null)
            {
                graphContext.OutputNodes.Remove(nodeToRemove);
                graphContext.Graph.Nodes.Remove(nodeToRemove);
            }
        }

        private static GraphNodeId GetGraphNodeId(string projectPath, GraphNode parentNode, string modelId)
        {
            string parents;
            if (parentNode != null)
            {
                // to ensure Graph id for node is unique we add a hashcodes for node's parents separated by ';'
                parents = parentNode.Id.GetNestedValueByName<string>(CodeGraphNodeIdName.Namespace);
                if (string.IsNullOrEmpty(parents))
                {
                    string currentProject = parentNode.Id.GetValue(CodeGraphNodeIdName.Assembly) ?? projectPath;
                    parents = currentProject.GetHashCode().ToString();
                }
            }
            else
            {
                parents = projectPath.GetHashCode().ToString();
            }

            parents = parents + ";" + modelId.GetHashCode();

            return GraphNodeId.GetNested(
                GraphNodeId.GetPartial(CodeGraphNodeIdName.Assembly, new Uri(projectPath, UriKind.RelativeOrAbsolute)),
                GraphNodeId.GetPartial(CodeGraphNodeIdName.File, new Uri(modelId.ToLowerInvariant(), UriKind.RelativeOrAbsolute)),
                GraphNodeId.GetPartial(CodeGraphNodeIdName.Namespace, parents));
        }

        private static GraphNodeId GetTopLevelGraphNodeId(string projectPath, string modelId)
        {
            string projectFolder = Path.GetDirectoryName(projectPath)?.ToLowerInvariant() ?? string.Empty;
            var filePath = new Uri(Path.Combine(projectFolder, modelId.ToLowerInvariant()), UriKind.RelativeOrAbsolute);

            return GraphNodeId.GetNested(
                GraphNodeId.GetPartial(CodeGraphNodeIdName.Assembly, new Uri(projectPath, UriKind.RelativeOrAbsolute)),
                GraphNodeId.GetPartial(CodeGraphNodeIdName.File, filePath));
        }

        private sealed class GraphIconCache
        {
            private ImmutableHashSet<ImageMoniker> _registeredIcons = ImmutableHashSet<ImageMoniker>.Empty;

            private ImmutableDictionary<(int id, Guid guid), string> _iconNameCache = ImmutableDictionary<(int id, Guid guid), string>.Empty;

            private readonly IVsImageService2 _imageService;

            public static async Task<GraphIconCache> CreateAsync(IAsyncServiceProvider serviceProvider)
            {
#pragma warning disable RS0030 // Do not used banned APIs
                var imageService = (IVsImageService2)await serviceProvider.GetServiceAsync(typeof(SVsImageService));
#pragma warning restore RS0030 // Do not used banned APIs

                return new GraphIconCache(imageService);
            }

            private GraphIconCache(IVsImageService2 imageService) => _imageService = imageService;

            public string GetName(ImageMoniker icon)
            {
                return ImmutableInterlocked.GetOrAdd(ref _iconNameCache, (id: icon.Id, guid: icon.Guid), i => $"{i.guid:D};{i.id}");
            }

            public void Register(ImageMoniker icon)
            {
                if (ImmutableInterlocked.Update(ref _registeredIcons, (knownIcons, arg) => knownIcons.Add(arg), icon))
                {
                    _imageService.TryAssociateNameWithMoniker(GetName(icon), icon);
                }
            }
        }
    }
}
