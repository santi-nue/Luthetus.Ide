﻿using Fluxor;
using Luthetus.Common.RazorLib.BackgroundTasks.Models;
using Luthetus.Common.RazorLib.ComponentRenderers.Models;
using Luthetus.Common.RazorLib.Keys.Models;
using Luthetus.Common.RazorLib.TreeViews.Models;
using Luthetus.Common.RazorLib.WatchWindows.Models;
using Luthetus.Ide.RazorLib.CompilerServices.States;
using Luthetus.Ide.RazorLib.ComponentRenderers.Models;
using Luthetus.Ide.RazorLib.TreeViewImplementations.Models;
using System.Collections.Immutable;

namespace Luthetus.Ide.RazorLib.CompilerServices.Models;

public class LuthetusIdeCompilerServiceBackgroundTaskApi
{
    private readonly IBackgroundTaskService _backgroundTaskService;
    private readonly IState<CompilerServiceExplorerState> _compilerServiceExplorerStateWrap;
    private readonly CompilerServiceRegistry _compilerServiceRegistry;
    private readonly ILuthetusIdeComponentRenderers _ideComponentRenderers;
    private readonly ILuthetusCommonComponentRenderers _commonComponentRenderers;
    private readonly ITreeViewService _treeViewService;
    private readonly IDispatcher _dispatcher;

    public LuthetusIdeCompilerServiceBackgroundTaskApi(
        IBackgroundTaskService backgroundTaskService,
        IState<CompilerServiceExplorerState> compilerServiceExplorerStateWrap,
        CompilerServiceRegistry compilerServiceRegistry,
        ILuthetusIdeComponentRenderers ideComponentRenderers,
        ILuthetusCommonComponentRenderers commonComponentRenderers,
        ITreeViewService treeViewService,
        IDispatcher dispatcher)
    {
        _backgroundTaskService = backgroundTaskService;
        _compilerServiceExplorerStateWrap = compilerServiceExplorerStateWrap;
        _compilerServiceRegistry = compilerServiceRegistry;
        _ideComponentRenderers = ideComponentRenderers;
        _commonComponentRenderers = commonComponentRenderers;
        _treeViewService = treeViewService;
        _dispatcher = dispatcher;
    }

    public Task SetCompilerServiceExplorerTreeView()
    {
        return _backgroundTaskService.EnqueueAsync(Key<BackgroundTask>.NewKey(), ContinuousBackgroundTaskWorker.GetQueueKey(),
            "Set CompilerServiceExplorer TreeView",
            async () =>
            {
                var compilerServiceExplorerState = _compilerServiceExplorerStateWrap.Value;

                var xmlCompilerServiceWatchWindowObject = new WatchWindowObject(
                    _compilerServiceRegistry.XmlCompilerService, _compilerServiceRegistry.XmlCompilerService.GetType(), "XML", true);

                var dotNetSolutionCompilerServiceWatchWindowObject = new WatchWindowObject(
                    _compilerServiceRegistry.DotNetSolutionCompilerService, _compilerServiceRegistry.DotNetSolutionCompilerService.GetType(), ".NET Solution", true);

                var cSharpProjectCompilerServiceWatchWindowObject = new WatchWindowObject(
                    _compilerServiceRegistry.CSharpProjectCompilerService, _compilerServiceRegistry.CSharpProjectCompilerService.GetType(), "C# Project", true);

                var cSharpCompilerServiceWatchWindowObject = new WatchWindowObject(
                    _compilerServiceRegistry.CSharpCompilerService, _compilerServiceRegistry.CSharpCompilerService.GetType(), "C#", true);

                var razorCompilerServiceWatchWindowObject = new WatchWindowObject(
                    _compilerServiceRegistry.RazorCompilerService, _compilerServiceRegistry.RazorCompilerService.GetType(), "Razor", true);

                var cssCompilerServiceWatchWindowObject = new WatchWindowObject(
                    _compilerServiceRegistry.CssCompilerService, _compilerServiceRegistry.CssCompilerService.GetType(), "Css", true);

                var fSharpCompilerServiceWatchWindowObject = new WatchWindowObject(
                    _compilerServiceRegistry.FSharpCompilerService, _compilerServiceRegistry.FSharpCompilerService.GetType(), "F#", true);

                var javaScriptCompilerServiceWatchWindowObject = new WatchWindowObject(
                    _compilerServiceRegistry.JavaScriptCompilerService, _compilerServiceRegistry.JavaScriptCompilerService.GetType(), "JavaScript", true);

                var typeScriptCompilerServiceWatchWindowObject = new WatchWindowObject(
                    _compilerServiceRegistry.TypeScriptCompilerService, _compilerServiceRegistry.TypeScriptCompilerService.GetType(), "TypeScript", true);

                var jsonCompilerServiceWatchWindowObject = new WatchWindowObject(
                    _compilerServiceRegistry.JsonCompilerService, _compilerServiceRegistry.JsonCompilerService.GetType(), "JSON", true);

                var terminalCompilerServiceWatchWindowObject = new WatchWindowObject(
                    _compilerServiceRegistry.TerminalCompilerService, _compilerServiceRegistry.TerminalCompilerService.GetType(), "Terminal", true);

                var rootNode = TreeViewAdhoc.ConstructTreeViewAdhoc(
                    new TreeViewReflectionWithView(xmlCompilerServiceWatchWindowObject, true, false, _ideComponentRenderers, _commonComponentRenderers),
                    new TreeViewReflectionWithView(dotNetSolutionCompilerServiceWatchWindowObject, true, false, _ideComponentRenderers, _commonComponentRenderers),
                    new TreeViewReflectionWithView(cSharpProjectCompilerServiceWatchWindowObject, true, false, _ideComponentRenderers, _commonComponentRenderers),
                    new TreeViewReflectionWithView(cSharpCompilerServiceWatchWindowObject, true, false, _ideComponentRenderers, _commonComponentRenderers),
                    new TreeViewReflectionWithView(razorCompilerServiceWatchWindowObject, true, false, _ideComponentRenderers, _commonComponentRenderers),
                    new TreeViewReflectionWithView(cssCompilerServiceWatchWindowObject, true, false, _ideComponentRenderers, _commonComponentRenderers),
                    new TreeViewReflectionWithView(fSharpCompilerServiceWatchWindowObject, true, false, _ideComponentRenderers, _commonComponentRenderers),
                    new TreeViewReflectionWithView(javaScriptCompilerServiceWatchWindowObject, true, false, _ideComponentRenderers, _commonComponentRenderers),
                    new TreeViewReflectionWithView(typeScriptCompilerServiceWatchWindowObject, true, false, _ideComponentRenderers, _commonComponentRenderers),
                    new TreeViewReflectionWithView(jsonCompilerServiceWatchWindowObject, true, false, _ideComponentRenderers, _commonComponentRenderers),
                    new TreeViewReflectionWithView(terminalCompilerServiceWatchWindowObject, true, false, _ideComponentRenderers, _commonComponentRenderers));

                await rootNode.LoadChildListAsync();

                if (!_treeViewService.TryGetTreeViewContainer(
                        CompilerServiceExplorerState.TreeViewCompilerServiceExplorerContentStateKey,
                        out var treeViewState))
                {
                    _treeViewService.RegisterTreeViewContainer(new TreeViewContainer(
                        CompilerServiceExplorerState.TreeViewCompilerServiceExplorerContentStateKey,
                        rootNode,
                        new TreeViewNoType[] { rootNode }.ToImmutableList()));
                }
                else
                {
                    _treeViewService.SetRoot(
                        CompilerServiceExplorerState.TreeViewCompilerServiceExplorerContentStateKey,
                        rootNode);

                    _treeViewService.SetActiveNode(
                        CompilerServiceExplorerState.TreeViewCompilerServiceExplorerContentStateKey,
                        rootNode,
                        true,
                        false);
                }

                _dispatcher.Dispatch(new CompilerServiceExplorerState.NewAction(inCompilerServiceExplorerState =>
                    new CompilerServiceExplorerState(inCompilerServiceExplorerState.Model)));
            });
    }
}
