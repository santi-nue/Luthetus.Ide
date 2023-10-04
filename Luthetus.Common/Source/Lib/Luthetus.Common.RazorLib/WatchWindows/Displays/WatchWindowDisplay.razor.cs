﻿using System.Collections.Immutable;
using Fluxor.Blazor.Web.Components;
using Microsoft.AspNetCore.Components;
using Luthetus.Common.RazorLib.ComponentRenderers.Models;
using Luthetus.Common.RazorLib.Commands.Models;
using Luthetus.Common.RazorLib.TreeViews.Models;
using Luthetus.Common.RazorLib.Dropdowns.Models;
using Luthetus.Common.RazorLib.Keys.Models;
using Luthetus.Common.RazorLib.WatchWindows.Models;

namespace Luthetus.Common.RazorLib.WatchWindows.Displays;

/// <summary>
/// TODO: SphagettiCode - TextEditor css classes are referenced in this
/// tree view? Furthermore, because of this, the Text Editor css classes are not being
/// set to the correct theme (try a non visual studio clone theme -- it doesn't work). (2023-09-19)
/// </summary>
public partial class WatchWindowDisplay : FluxorComponent
{
    [Inject]
    private ITreeViewService TreeViewService { get; set; } = null!;
    [Inject]
    private IDropdownService DropdownService { get; set; } = null!;
    [Inject]
    private ILuthetusCommonComponentRenderers LuthetusCommonComponentRenderers { get; set; } = null!;

    [Parameter, EditorRequired]
    public WatchWindowObjectWrap WatchWindowObjectWrap { get; set; } = null!;

    public static Key<TreeViewContainer> TreeViewStateKey { get; } = Key<TreeViewContainer>.NewKey();
    public static Key<DropdownRecord> WatchWindowContextMenuDropdownKey { get; } = Key<DropdownRecord>.NewKey();

    private TreeViewCommandParameter? _mostRecentTreeViewContextMenuCommandParameter;
    private TreeViewMouseEventHandler _treeViewMouseEventHandler = null!;
    private TreeViewKeyboardEventHandler _treeViewKeyboardEventHandler = null!;
    private bool _disposed;

    protected override void OnInitialized()
    {
        _treeViewMouseEventHandler = new(TreeViewService);
        _treeViewKeyboardEventHandler = new(TreeViewService);
        base.OnInitialized();
    }

    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            if (!TreeViewService.TryGetTreeViewState(TreeViewStateKey, out var treeViewState))
            {
                var rootNode = new TreeViewReflection(
                    WatchWindowObjectWrap,
                    true,
                    false,
                    LuthetusCommonComponentRenderers);

                TreeViewService.RegisterTreeViewState(new TreeViewContainer(
                    TreeViewStateKey,
                    rootNode,
                    rootNode,
                    ImmutableList<TreeViewNoType>.Empty));
            }
        }

        return base.OnAfterRenderAsync(firstRender);
    }

    private async Task OnTreeViewContextMenuFunc(TreeViewCommandParameter commandParameter)
    {
        _mostRecentTreeViewContextMenuCommandParameter = commandParameter;

        DropdownService.AddActiveDropdownKey(WatchWindowContextMenuDropdownKey);

        await InvokeAsync(StateHasChanged);
    }

    protected override void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _disposed = true;

            TreeViewService.DisposeTreeViewState(TreeViewStateKey);
        }

        base.Dispose(disposing);
    }
}