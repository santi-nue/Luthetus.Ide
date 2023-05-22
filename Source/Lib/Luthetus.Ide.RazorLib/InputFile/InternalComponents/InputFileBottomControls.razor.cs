﻿using Luthetus.Common.RazorLib.BackgroundTaskCase;
using Luthetus.Common.RazorLib.Dialog;
using Luthetus.Common.RazorLib.Store.DialogCase;
using Luthetus.Ide.ClassLib.Store.InputFileCase;
using Luthetus.Ide.ClassLib.ComponentRenderers;
using Luthetus.Ide.ClassLib.FileSystem.Interfaces;
using Fluxor;
using Microsoft.AspNetCore.Components;

namespace Luthetus.Ide.RazorLib.InputFile.InternalComponents;

public partial class InputFileBottomControls : ComponentBase
{
    [Inject]
    private IDispatcher Dispatcher { get; set; } = null!;
    [Inject]
    private ILuthetusIdeComponentRenderers LuthetusIdeComponentRenderers { get; set; } = null!;
    [Inject]
    private IFileSystemProvider FileSystemProvider { get; set; } = null!;
    [Inject]
    private IEnvironmentProvider EnvironmentProvider { get; set; } = null!;
    [Inject]
    private IBackgroundTaskQueue BackgroundTaskQueue { get; set; } = null!;

    [CascadingParameter]
    public DialogRecord? DialogRecord { get; set; }
    [CascadingParameter]
    public InputFileState InputFileState { get; set; } = null!;

    private ElementReference? _searchElementReference;
    private string _searchQuery = string.Empty;

    private void SelectInputFilePatternOnChange(ChangeEventArgs changeEventArgs)
    {
        var patternName = (string)(changeEventArgs.Value ?? string.Empty);

        var pattern = InputFileState.InputFilePatterns
            .FirstOrDefault(x => x.PatternName == patternName);

        if (pattern is not null)
        {
            Dispatcher.Dispatch(
                new InputFileState.SetSelectedInputFilePatternAction(
                    pattern));
        }
    }

    private string GetSelectedTreeViewModelAbsoluteFilePathString(InputFileState inputFileState)
    {
        var selectedAbsoluteFilePath = inputFileState.SelectedTreeViewModel?.Item;

        if (selectedAbsoluteFilePath is null)
            return "Selection is null";

        return selectedAbsoluteFilePath.GetAbsoluteFilePathString();
    }

    private async Task FireOnAfterSubmit()
    {
        var valid = await InputFileState.SelectionIsValidFunc.Invoke(
            InputFileState.SelectedTreeViewModel?.Item);

        if (valid)
        {
            if (DialogRecord is not null)
            {
                Dispatcher.Dispatch(
                    new DialogRecordsCollection.DisposeAction(
                        DialogRecord.DialogKey));
            }

            await InputFileState.OnAfterSubmitFunc
                .Invoke(InputFileState.SelectedTreeViewModel?.Item);
        }
    }

    private bool OnAfterSubmitIsDisabled()
    {
        return !InputFileState.SelectionIsValidFunc.Invoke(
                InputFileState.SelectedTreeViewModel?.Item)
            .Result;
    }

    private Task CancelOnClick()
    {
        if (DialogRecord is not null)
        {
            Dispatcher.Dispatch(
                new DialogRecordsCollection.DisposeAction(
                    DialogRecord.DialogKey));
        }

        return Task.CompletedTask;
    }
}