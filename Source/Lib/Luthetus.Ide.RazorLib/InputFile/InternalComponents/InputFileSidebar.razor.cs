﻿using Fluxor;
using Luthetus.Common.RazorLib.ComponentRenderers;
using Luthetus.Common.RazorLib.Dialog;
using Luthetus.Common.RazorLib.Dimensions;
using Luthetus.Common.RazorLib.FileSystem.Interfaces;
using Luthetus.Common.RazorLib.Store.DropdownCase;
using Luthetus.Common.RazorLib.TreeView;
using Luthetus.Common.RazorLib.TreeView.Commands;
using Luthetus.Common.RazorLib.TreeView.TreeViewClasses;
using Luthetus.Ide.ClassLib.ComponentRenderersCase;
using Luthetus.Ide.ClassLib.Store.InputFileCase;
using Luthetus.Ide.ClassLib.TreeViewImplementationsCase;
using Luthetus.Ide.RazorLib.InputFile.Classes;
using Microsoft.AspNetCore.Components;
using System.Collections.Immutable;

namespace Luthetus.Ide.RazorLib.InputFile.InternalComponents;

public partial class InputFileSidebar : ComponentBase
{
    [Inject]
    private IDispatcher Dispatcher { get; set; } = null!;
    [Inject]
    private ILuthetusIdeComponentRenderers LuthetusIdeComponentRenderers { get; set; } = null!;
    [Inject]
    private ILuthetusCommonComponentRenderers LuthetusCommonComponentRenderers { get; set; } = null!;
    [Inject]
    private ITreeViewService TreeViewService { get; set; } = null!;
    [Inject]
    private IFileSystemProvider FileSystemProvider { get; set; } = null!;
    [Inject]
    private IEnvironmentProvider EnvironmentProvider { get; set; } = null!;

    [CascadingParameter(Name = "SetInputFileContentTreeViewRootFunc")]
    public Func<IAbsolutePath, Task> SetInputFileContentTreeViewRootFunc { get; set; } = null!;
    [CascadingParameter]
    public InputFileTreeViewMouseEventHandler InputFileTreeViewMouseEventHandler { get; set; } = null!;
    [CascadingParameter]
    public InputFileTreeViewKeyboardEventHandler InputFileTreeViewKeyboardEventHandler { get; set; } = null!;
    [CascadingParameter]
    public InputFileRegistry InputFileState { get; set; } = null!;
    [CascadingParameter]
    public DialogRecord DialogRecord { get; set; } = null!;

    [Parameter, EditorRequired]
    public ElementDimensions ElementDimensions { get; set; } = null!;
    [Parameter, EditorRequired]
    public Action<IAbsolutePath?> SetSelectedAbsolutePath { get; set; } = null!;

    public static readonly TreeViewStateKey TreeViewInputFileSidebarStateKey =
        TreeViewStateKey.NewKey();

    private ITreeViewCommandParameter? _mostRecentTreeViewCommandParameter;

    protected override void OnInitialized()
    {
        var directoryHomeNode = new TreeViewAbsolutePath(
            EnvironmentProvider.HomeDirectoryAbsolutePath,
            LuthetusIdeComponentRenderers,
            LuthetusCommonComponentRenderers,
            FileSystemProvider,
            EnvironmentProvider,
            true,
            false);

        var directoryRootNode = new TreeViewAbsolutePath(
            EnvironmentProvider.RootDirectoryAbsolutePath,
            LuthetusIdeComponentRenderers,
            LuthetusCommonComponentRenderers,
            FileSystemProvider,
            EnvironmentProvider,
            true,
            false);

        var adhocRootNode = TreeViewAdhoc.ConstructTreeViewAdhoc(
            directoryHomeNode,
            directoryRootNode);

        if (!TreeViewService.TryGetTreeViewState(
                TreeViewInputFileSidebarStateKey, out var treeViewState))
        {
            TreeViewService.RegisterTreeViewState(new TreeViewState(
                TreeViewInputFileSidebarStateKey,
                adhocRootNode,
                directoryHomeNode,
                ImmutableList<TreeViewNoType>.Empty));
        }

        base.OnInitialized();
    }

    private async Task OnTreeViewContextMenuFunc(ITreeViewCommandParameter treeViewCommandParameter)
    {
        _mostRecentTreeViewCommandParameter = treeViewCommandParameter;

        Dispatcher.Dispatch(new DropdownRegistry.AddActiveAction(
            InputFileContextMenu.ContextMenuEventDropdownKey));

        await InvokeAsync(StateHasChanged);
    }
}