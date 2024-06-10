using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Luthetus.Common.RazorLib.Keys.Models;
using Luthetus.Common.RazorLib.RenderStates.Models;
using Luthetus.Common.RazorLib.Keyboards.Models;
using Luthetus.Common.RazorLib.Clipboards.Models;
using Luthetus.Common.RazorLib.JavaScriptObjects.Models;
using Luthetus.TextEditor.RazorLib.Cursors.Models;
using Luthetus.TextEditor.RazorLib.TextEditors.Models;
using Luthetus.TextEditor.RazorLib.TextEditors.Models.Internals;
using Luthetus.TextEditor.RazorLib.Characters.Models;
using Luthetus.TextEditor.RazorLib.TextEditors.Models.TextEditorServices;
using Luthetus.TextEditor.RazorLib.Lexes.Models;
using Luthetus.TextEditor.RazorLib.Installations.Models;
using Luthetus.TextEditor.RazorLib.Groups.Models;
using Luthetus.TextEditor.RazorLib.JsRuntimes.Models;
using Luthetus.TextEditor.RazorLib.Events.Models;
using Luthetus.TextEditor.RazorLib.ComponentRenderers.Models;

namespace Luthetus.TextEditor.RazorLib.Commands.Models.Defaults;

public class TextEditorCommandDefaultFunctions
{
    public static TextEditorEdit DoNothingDiscardFactory()
    {
        return (IEditContext editContext) =>
        {
            return Task.CompletedTask;
        };
    }

    public static TextEditorEdit CopyFactory(
        ResourceUri modelResourceUri, Key<TextEditorViewModel> viewModelKey, TextEditorCommandArgs commandArgs)
    {
        return async (IEditContext editContext) =>
        {
            var modelModifier = editContext.GetModelModifier(modelResourceUri);
            var viewModelModifier = editContext.GetViewModelModifier(viewModelKey);
            var cursorModifierBag = editContext.GetCursorModifierBag(viewModelModifier?.ViewModel);
            var primaryCursorModifier = editContext.GetPrimaryCursorModifier(cursorModifierBag);

            if (modelModifier is null || viewModelModifier is null || cursorModifierBag is null || primaryCursorModifier is null)
                return;

            var selectedText = TextEditorSelectionHelper.GetSelectedText(primaryCursorModifier, modelModifier);
            selectedText ??= modelModifier.GetLineTextRange(primaryCursorModifier.LineIndex, 1);

            await commandArgs.ServiceProvider.GetRequiredService<IClipboardService>().SetClipboard(selectedText).ConfigureAwait(false);
            await viewModelModifier.ViewModel.FocusFactory().Invoke(editContext).ConfigureAwait(false);
        };
    }

    public static TextEditorEdit CutFactory(
        ResourceUri modelResourceUri, Key<TextEditorViewModel> viewModelKey, TextEditorCommandArgs commandArgs)
    {
        return async (IEditContext editContext) =>
        {
            var modelModifier = editContext.GetModelModifier(modelResourceUri);
            var viewModelModifier = editContext.GetViewModelModifier(viewModelKey);
            var cursorModifierBag = editContext.GetCursorModifierBag(viewModelModifier?.ViewModel);
            var primaryCursorModifier = editContext.GetPrimaryCursorModifier(cursorModifierBag);
            
            if (modelModifier is null || viewModelModifier is null || cursorModifierBag is null || primaryCursorModifier is null)
                return;

            if (!TextEditorSelectionHelper.HasSelectedText(primaryCursorModifier))
            {
                var positionIndex = modelModifier.GetPositionIndex(primaryCursorModifier);
                var lineInformation = modelModifier.GetLineInformationFromPositionIndex(positionIndex);

                primaryCursorModifier.SelectionAnchorPositionIndex = lineInformation.StartPositionIndexInclusive;
                primaryCursorModifier.SelectionEndingPositionIndex = lineInformation.EndPositionIndexExclusive;
            }

            var selectedText = TextEditorSelectionHelper.GetSelectedText(primaryCursorModifier, modelModifier) ?? string.Empty;
            await commandArgs.ServiceProvider.GetRequiredService<IClipboardService>().SetClipboard(selectedText).ConfigureAwait(false);

            await viewModelModifier.ViewModel.FocusFactory().Invoke(editContext).ConfigureAwait(false);

            modelModifier.HandleKeyboardEvent(
                new KeyboardEventArgs { Key = KeyboardKeyFacts.MetaKeys.DELETE },
                cursorModifierBag,
                CancellationToken.None);
        };
    }

    public static TextEditorEdit PasteFactory(
        ResourceUri modelResourceUri, Key<TextEditorViewModel> viewModelKey, TextEditorCommandArgs commandArgs)
    {
        return async (IEditContext editContext) =>
        {
            var modelModifier = editContext.GetModelModifier(modelResourceUri);
            var viewModelModifier = editContext.GetViewModelModifier(viewModelKey);
            var cursorModifierBag = editContext.GetCursorModifierBag(viewModelModifier?.ViewModel);
            var primaryCursorModifier = editContext.GetPrimaryCursorModifier(cursorModifierBag);

            if (modelModifier is null || viewModelModifier is null || cursorModifierBag is null || primaryCursorModifier is null)
                return;

            var clipboard = await commandArgs.ServiceProvider.GetRequiredService<IClipboardService>().ReadClipboard().ConfigureAwait(false);
            modelModifier.Insert(clipboard, cursorModifierBag, cancellationToken: CancellationToken.None);
        };
    }

    public static TextEditorEdit SaveFactory(
        ResourceUri modelResourceUri, Key<TextEditorViewModel> viewModelKey, TextEditorCommandArgs commandArgs)
    {
        return (IEditContext editContext) =>
        {
            var modelModifier = editContext.GetModelModifier(modelResourceUri);
            var viewModelModifier = editContext.GetViewModelModifier(viewModelKey);
            var cursorModifierBag = editContext.GetCursorModifierBag(viewModelModifier?.ViewModel);
            var primaryCursorModifier = editContext.GetPrimaryCursorModifier(cursorModifierBag);

            if (modelModifier is null || viewModelModifier is null || cursorModifierBag is null || primaryCursorModifier is null)
                return Task.CompletedTask;

            viewModelModifier.ViewModel.OnSaveRequested?.Invoke(modelModifier);
            modelModifier.SetIsDirtyFalse();
            return Task.CompletedTask;
        };
    }

    public static TextEditorEdit SelectAllFactory(
        ResourceUri modelResourceUri, Key<TextEditorViewModel> viewModelKey, TextEditorCommandArgs commandArgs)
    {
        return (IEditContext editContext) =>
        {
            var modelModifier = editContext.GetModelModifier(modelResourceUri);
            var viewModelModifier = editContext.GetViewModelModifier(viewModelKey);
            var cursorModifierBag = editContext.GetCursorModifierBag(viewModelModifier?.ViewModel);
            var primaryCursorModifier = editContext.GetPrimaryCursorModifier(cursorModifierBag);

            if (modelModifier is null || viewModelModifier is null || cursorModifierBag is null || primaryCursorModifier is null)
                return Task.CompletedTask;

            primaryCursorModifier.SelectionAnchorPositionIndex = 0;
            primaryCursorModifier.SelectionEndingPositionIndex = modelModifier.CharCount;
            return Task.CompletedTask;
        };
    }

    public static TextEditorEdit UndoFactory(
        ResourceUri modelResourceUri, Key<TextEditorViewModel> viewModelKey, TextEditorCommandArgs commandArgs)
    {
        return commandArgs.TextEditorService.ModelApi.UndoEditFactory(modelResourceUri);
    }

    public static TextEditorEdit RedoFactory(
        ResourceUri modelResourceUri, Key<TextEditorViewModel> viewModelKey, TextEditorCommandArgs commandArgs)
    {
        return commandArgs.TextEditorService.ModelApi.RedoEditFactory(modelResourceUri);
    }

    public static TextEditorEdit RemeasureFactory(
        ResourceUri modelResourceUri, Key<TextEditorViewModel> viewModelKey, TextEditorCommandArgs commandArgs)
    {
        return (IEditContext editContext) =>
        {
            editContext.TextEditorService.OptionsApi.SetRenderStateKey(Key<RenderState>.NewKey());
            return Task.CompletedTask;
        };
    }

    public static TextEditorEdit ScrollLineDownFactory(
        ResourceUri modelResourceUri, Key<TextEditorViewModel> viewModelKey, TextEditorCommandArgs commandArgs)
    {
        return (IEditContext editContext) =>
        {
            var modelModifier = editContext.GetModelModifier(modelResourceUri);
            var viewModelModifier = editContext.GetViewModelModifier(viewModelKey);
            var cursorModifierBag = editContext.GetCursorModifierBag(viewModelModifier?.ViewModel);
            var primaryCursorModifier = editContext.GetPrimaryCursorModifier(cursorModifierBag);

            if (modelModifier is null || viewModelModifier is null || cursorModifierBag is null || primaryCursorModifier is null)
                return Task.CompletedTask;

            return editContext.TextEditorService.ViewModelApi.MutateScrollVerticalPositionFactory(
                    viewModelKey,
                    viewModelModifier.ViewModel.CharAndLineMeasurements.LineHeight)
                .Invoke(editContext);
        };
    }

    public static TextEditorEdit ScrollLineUpFactory(
        ResourceUri modelResourceUri, Key<TextEditorViewModel> viewModelKey, TextEditorCommandArgs commandArgs)
    {
        return (IEditContext editContext) =>
        {
            var modelModifier = editContext.GetModelModifier(modelResourceUri);
            var viewModelModifier = editContext.GetViewModelModifier(viewModelKey);
            var cursorModifierBag = editContext.GetCursorModifierBag(viewModelModifier?.ViewModel);
            var primaryCursorModifier = editContext.GetPrimaryCursorModifier(cursorModifierBag);

            if (modelModifier is null || viewModelModifier is null || cursorModifierBag is null || primaryCursorModifier is null)
                return Task.CompletedTask;

            return editContext.TextEditorService.ViewModelApi.MutateScrollVerticalPositionFactory(
                    viewModelKey,
                    -1 * viewModelModifier.ViewModel.CharAndLineMeasurements.LineHeight)
                .Invoke(editContext);
        };
    }

    public static TextEditorEdit ScrollPageDownFactory(
        ResourceUri modelResourceUri, Key<TextEditorViewModel> viewModelKey, TextEditorCommandArgs commandArgs)
    {
        return (IEditContext editContext) =>
        {
            var modelModifier = editContext.GetModelModifier(modelResourceUri);
            var viewModelModifier = editContext.GetViewModelModifier(viewModelKey);
            var cursorModifierBag = editContext.GetCursorModifierBag(viewModelModifier?.ViewModel);
            var primaryCursorModifier = editContext.GetPrimaryCursorModifier(cursorModifierBag);

            if (modelModifier is null || viewModelModifier is null || cursorModifierBag is null || primaryCursorModifier is null)
                return Task.CompletedTask;

            return editContext.TextEditorService.ViewModelApi.MutateScrollVerticalPositionFactory(
                    viewModelKey,
                    viewModelModifier.ViewModel.TextEditorDimensions.Height)
                .Invoke(editContext);
        };
    }

    public static TextEditorEdit ScrollPageUpFactory(
        ResourceUri modelResourceUri, Key<TextEditorViewModel> viewModelKey, TextEditorCommandArgs commandArgs)
    {
        return (IEditContext editContext) =>
        {
            var modelModifier = editContext.GetModelModifier(modelResourceUri);
            var viewModelModifier = editContext.GetViewModelModifier(viewModelKey);
            var cursorModifierBag = editContext.GetCursorModifierBag(viewModelModifier?.ViewModel);
            var primaryCursorModifier = editContext.GetPrimaryCursorModifier(cursorModifierBag);

            if (modelModifier is null || viewModelModifier is null || cursorModifierBag is null || primaryCursorModifier is null)
                return Task.CompletedTask;

            return editContext.TextEditorService.ViewModelApi.MutateScrollVerticalPositionFactory(
                    viewModelKey,
                    -1 * viewModelModifier.ViewModel.TextEditorDimensions.Height)
                .Invoke(editContext);
        };
    }

    public static TextEditorEdit CursorMovePageBottomFactory(
        ResourceUri modelResourceUri, Key<TextEditorViewModel> viewModelKey, TextEditorCommandArgs commandArgs)
    {
        return (IEditContext editContext) =>
        {
            var modelModifier = editContext.GetModelModifier(modelResourceUri);
            var viewModelModifier = editContext.GetViewModelModifier(viewModelKey);
            var cursorModifierBag = editContext.GetCursorModifierBag(viewModelModifier?.ViewModel);
            var primaryCursorModifier = editContext.GetPrimaryCursorModifier(cursorModifierBag);

            if (modelModifier is null || viewModelModifier is null || cursorModifierBag is null || primaryCursorModifier is null)
                return Task.CompletedTask;

            if (viewModelModifier.ViewModel.VirtualizationResult?.EntryList.Any() ?? false)
            {
                var lastEntry = viewModelModifier.ViewModel.VirtualizationResult.EntryList.Last();
                var lastEntriesRowLength = modelModifier.GetLineLength(lastEntry.Index);

                primaryCursorModifier.LineIndex = lastEntry.Index;
                primaryCursorModifier.ColumnIndex = lastEntriesRowLength;
            }

            return Task.CompletedTask;
        };
    }

    public static TextEditorEdit CursorMovePageTopFactory(
        ResourceUri modelResourceUri, Key<TextEditorViewModel> viewModelKey, TextEditorCommandArgs commandArgs)
    {
        return (IEditContext editContext) =>
        {
            var modelModifier = editContext.GetModelModifier(modelResourceUri);
            var viewModelModifier = editContext.GetViewModelModifier(viewModelKey);
            var cursorModifierBag = editContext.GetCursorModifierBag(viewModelModifier?.ViewModel);
            var primaryCursorModifier = editContext.GetPrimaryCursorModifier(cursorModifierBag);

            if (modelModifier is null || viewModelModifier is null || cursorModifierBag is null || primaryCursorModifier is null)
                return Task.CompletedTask;

            if (viewModelModifier.ViewModel.VirtualizationResult?.EntryList.Any() ?? false)
            {
                var firstEntry = viewModelModifier.ViewModel.VirtualizationResult.EntryList.First();

                primaryCursorModifier.LineIndex = firstEntry.Index;
                primaryCursorModifier.ColumnIndex = 0;
            }

            return Task.CompletedTask;
        };
    }

    public static TextEditorEdit DuplicateFactory(
        ResourceUri modelResourceUri, Key<TextEditorViewModel> viewModelKey, TextEditorCommandArgs commandArgs)
    {
        return (IEditContext editContext) =>
        {
            var modelModifier = editContext.GetModelModifier(modelResourceUri);
            var viewModelModifier = editContext.GetViewModelModifier(viewModelKey);
            var cursorModifierBag = editContext.GetCursorModifierBag(viewModelModifier?.ViewModel);
            var primaryCursorModifier = editContext.GetPrimaryCursorModifier(cursorModifierBag);

            if (modelModifier is null || viewModelModifier is null || cursorModifierBag is null || primaryCursorModifier is null)
                return Task.CompletedTask;

            var selectedText = TextEditorSelectionHelper.GetSelectedText(primaryCursorModifier, modelModifier);

            TextEditorCursor cursorForInsertion;
            if (selectedText is null)
            {
                // Select line
                selectedText = modelModifier.GetLineTextRange(primaryCursorModifier.LineIndex, 1);

                cursorForInsertion = new TextEditorCursor(
                    primaryCursorModifier.LineIndex,
                    0,
                    primaryCursorModifier.IsPrimaryCursor);
            }
            else
            {
                // Clone the TextEditorCursor to remove the TextEditorSelection otherwise the
                // selected text to duplicate would be overwritten by itself and do nothing
                cursorForInsertion = primaryCursorModifier.ToCursor() with
                {
                    Selection = TextEditorSelection.Empty
                };
            }

            modelModifier.Insert(
                selectedText,
                new CursorModifierBagTextEditor(Key<TextEditorViewModel>.Empty, new List<TextEditorCursorModifier>() { new(cursorForInsertion) }),
                cancellationToken: CancellationToken.None);

            return Task.CompletedTask;
        };
    }

    public static TextEditorEdit IndentMoreFactory(
        ResourceUri modelResourceUri, Key<TextEditorViewModel> viewModelKey, TextEditorCommandArgs commandArgs)
    {
        return (IEditContext editContext) =>
        {
            var modelModifier = editContext.GetModelModifier(modelResourceUri);
            var viewModelModifier = editContext.GetViewModelModifier(viewModelKey);
            var cursorModifierBag = editContext.GetCursorModifierBag(viewModelModifier?.ViewModel);
            var primaryCursorModifier = editContext.GetPrimaryCursorModifier(cursorModifierBag);

            if (modelModifier is null || viewModelModifier is null || cursorModifierBag is null || primaryCursorModifier is null)
                return Task.CompletedTask;

            if (!TextEditorSelectionHelper.HasSelectedText(primaryCursorModifier))
                return Task.CompletedTask;

            var selectionBoundsInPositionIndexUnits = TextEditorSelectionHelper.GetSelectionBounds(primaryCursorModifier);

            var selectionBoundsInRowIndexUnits = TextEditorSelectionHelper.ConvertSelectionOfPositionIndexUnitsToRowIndexUnits(
                modelModifier,
                selectionBoundsInPositionIndexUnits);

            for (var i = selectionBoundsInRowIndexUnits.lowerRowIndexInclusive;
                 i < selectionBoundsInRowIndexUnits.upperRowIndexExclusive;
                 i++)
            {
                var insertionCursor = new TextEditorCursor(i, 0, true);

                var insertionCursorModifierBag = new CursorModifierBagTextEditor(
                    Key<TextEditorViewModel>.Empty,
                    new List<TextEditorCursorModifier> { new TextEditorCursorModifier(insertionCursor) });

                modelModifier.Insert(
                    KeyboardKeyFacts.WhitespaceCharacters.TAB.ToString(),
                    insertionCursorModifierBag,
                    cancellationToken: CancellationToken.None);
            }

            var lowerBoundPositionIndexChange = 1;

            var upperBoundPositionIndexChange = selectionBoundsInRowIndexUnits.upperRowIndexExclusive -
                selectionBoundsInRowIndexUnits.lowerRowIndexInclusive;

            if (primaryCursorModifier.SelectionAnchorPositionIndex < primaryCursorModifier.SelectionEndingPositionIndex)
            {
                primaryCursorModifier.SelectionAnchorPositionIndex += lowerBoundPositionIndexChange;
                primaryCursorModifier.SelectionEndingPositionIndex += upperBoundPositionIndexChange;
            }
            else
            {
                primaryCursorModifier.SelectionAnchorPositionIndex += upperBoundPositionIndexChange;
                primaryCursorModifier.SelectionEndingPositionIndex += lowerBoundPositionIndexChange;
            }

            primaryCursorModifier.SetColumnIndexAndPreferred(1 + primaryCursorModifier.ColumnIndex);
            return Task.CompletedTask;
        };
    }

    public static TextEditorEdit IndentLessFactory(
        ResourceUri modelResourceUri, Key<TextEditorViewModel> viewModelKey, TextEditorCommandArgs commandArgs)
    {
        return (IEditContext editContext) =>
        {
            var modelModifier = editContext.GetModelModifier(modelResourceUri);
            var viewModelModifier = editContext.GetViewModelModifier(viewModelKey);
            var cursorModifierBag = editContext.GetCursorModifierBag(viewModelModifier?.ViewModel);
            var primaryCursorModifier = editContext.GetPrimaryCursorModifier(cursorModifierBag);

            if (modelModifier is null || viewModelModifier is null || cursorModifierBag is null || primaryCursorModifier is null)
                return Task.CompletedTask;

            var selectionBoundsInPositionIndexUnits = TextEditorSelectionHelper.GetSelectionBounds(primaryCursorModifier);

            var selectionBoundsInRowIndexUnits = TextEditorSelectionHelper.ConvertSelectionOfPositionIndexUnitsToRowIndexUnits(
                modelModifier,
                selectionBoundsInPositionIndexUnits);

            bool isFirstLoop = true;

            for (var i = selectionBoundsInRowIndexUnits.lowerRowIndexInclusive;
                 i < selectionBoundsInRowIndexUnits.upperRowIndexExclusive;
                 i++)
            {
                var rowPositionIndex = modelModifier.GetPositionIndex(i, 0);
                var characterReadCount = TextEditorModel.TAB_WIDTH;
                var lengthOfRow = modelModifier.GetLineLength(i);

                characterReadCount = Math.Min(lengthOfRow, characterReadCount);

                var readResult = modelModifier.GetString(rowPositionIndex, characterReadCount);
                var removeCharacterCount = 0;

                if (readResult.StartsWith(KeyboardKeyFacts.WhitespaceCharacters.TAB))
                {
                    removeCharacterCount = 1;

                    var cursorForDeletion = new TextEditorCursor(i, 0, true);

                    modelModifier.DeleteByRange(
                        removeCharacterCount, // Delete a single "Tab" character
                        new CursorModifierBagTextEditor(Key<TextEditorViewModel>.Empty, new List<TextEditorCursorModifier> { new(cursorForDeletion) }),
                        CancellationToken.None);
                }
                else if (readResult.StartsWith(KeyboardKeyFacts.WhitespaceCharacters.SPACE))
                {
                    var cursorForDeletion = new TextEditorCursor(i, 0, true);
                    var contiguousSpaceCount = 0;

                    foreach (var character in readResult)
                    {
                        if (character == KeyboardKeyFacts.WhitespaceCharacters.SPACE)
                            contiguousSpaceCount++;
                    }

                    removeCharacterCount = contiguousSpaceCount;

                    modelModifier.DeleteByRange(
                        removeCharacterCount,
                        new CursorModifierBagTextEditor(Key<TextEditorViewModel>.Empty, new List<TextEditorCursorModifier> { new(cursorForDeletion) }),
                        CancellationToken.None);
                }

                // Modify the lower bound of user's text selection
                if (isFirstLoop)
                {
                    isFirstLoop = false;

                    if (primaryCursorModifier.SelectionAnchorPositionIndex < primaryCursorModifier.SelectionEndingPositionIndex)
                        primaryCursorModifier.SelectionAnchorPositionIndex -= removeCharacterCount;
                    else
                        primaryCursorModifier.SelectionEndingPositionIndex -= removeCharacterCount;
                }

                // Modify the upper bound of user's text selection
                if (primaryCursorModifier.SelectionAnchorPositionIndex < primaryCursorModifier.SelectionEndingPositionIndex)
                    primaryCursorModifier.SelectionEndingPositionIndex -= removeCharacterCount;
                else
                    primaryCursorModifier.SelectionAnchorPositionIndex -= removeCharacterCount;

                // Modify the column index of user's cursor
                if (i == primaryCursorModifier.LineIndex)
                {
                    var nextColumnIndex = primaryCursorModifier.ColumnIndex - removeCharacterCount;

                    primaryCursorModifier.LineIndex = primaryCursorModifier.LineIndex;
                    primaryCursorModifier.ColumnIndex = Math.Max(0, nextColumnIndex);
                }
            }

            return Task.CompletedTask;
        };
    }

    public static TextEditorEdit ClearTextSelectionFactory(
        ResourceUri modelResourceUri, Key<TextEditorViewModel> viewModelKey, TextEditorCommandArgs commandArgs)
    {
        return (IEditContext editContext) =>
        {
            var modelModifier = editContext.GetModelModifier(modelResourceUri);
            var viewModelModifier = editContext.GetViewModelModifier(viewModelKey);
            var cursorModifierBag = editContext.GetCursorModifierBag(viewModelModifier?.ViewModel);
            var primaryCursorModifier = editContext.GetPrimaryCursorModifier(cursorModifierBag);

            if (modelModifier is null || viewModelModifier is null || cursorModifierBag is null || primaryCursorModifier is null)
                return Task.CompletedTask;

            primaryCursorModifier.SelectionAnchorPositionIndex = null;
            return Task.CompletedTask;
        };
    }

    public static TextEditorEdit NewLineBelowFactory(
        ResourceUri modelResourceUri, Key<TextEditorViewModel> viewModelKey, TextEditorCommandArgs commandArgs)
    {
        return (IEditContext editContext) =>
        {
            var modelModifier = editContext.GetModelModifier(modelResourceUri);
            var viewModelModifier = editContext.GetViewModelModifier(viewModelKey);
            var cursorModifierBag = editContext.GetCursorModifierBag(viewModelModifier?.ViewModel);
            var primaryCursorModifier = editContext.GetPrimaryCursorModifier(cursorModifierBag);

            if (modelModifier is null || viewModelModifier is null || cursorModifierBag is null || primaryCursorModifier is null)
                return Task.CompletedTask;

            primaryCursorModifier.SelectionAnchorPositionIndex = null;

            var lengthOfRow = modelModifier.GetLineLength(primaryCursorModifier.LineIndex);

            primaryCursorModifier.LineIndex = primaryCursorModifier.LineIndex;
            primaryCursorModifier.ColumnIndex = lengthOfRow;

            modelModifier.Insert("\n", cursorModifierBag, cancellationToken: CancellationToken.None);
            return Task.CompletedTask;
        };
    }

    public static TextEditorEdit NewLineAboveFactory(
        ResourceUri modelResourceUri, Key<TextEditorViewModel> viewModelKey, TextEditorCommandArgs commandArgs)
    {
        return (IEditContext editContext) =>
        {
            var modelModifier = editContext.GetModelModifier(modelResourceUri);
            var viewModelModifier = editContext.GetViewModelModifier(viewModelKey);
            var cursorModifierBag = editContext.GetCursorModifierBag(viewModelModifier?.ViewModel);
            var primaryCursorModifier = editContext.GetPrimaryCursorModifier(cursorModifierBag);

            if (modelModifier is null || viewModelModifier is null || cursorModifierBag is null || primaryCursorModifier is null)
                return Task.CompletedTask;

            primaryCursorModifier.SelectionAnchorPositionIndex = null;

            primaryCursorModifier.LineIndex = primaryCursorModifier.LineIndex;
            primaryCursorModifier.ColumnIndex = 0;

            modelModifier.Insert("\n", cursorModifierBag, cancellationToken: CancellationToken.None);

            if (primaryCursorModifier.LineIndex > 1)
            {
                primaryCursorModifier.LineIndex--;
                primaryCursorModifier.ColumnIndex = 0;
            }

            return Task.CompletedTask;
        };
    }

    public static TextEditorEdit GoToMatchingCharacterFactory(
        ResourceUri modelResourceUri, Key<TextEditorViewModel> viewModelKey, TextEditorCommandArgs commandArgs)
    {
        return async (IEditContext editContext) =>
        {
            var modelModifier = editContext.GetModelModifier(modelResourceUri);
            var viewModelModifier = editContext.GetViewModelModifier(viewModelKey);
            var cursorModifierBag = editContext.GetCursorModifierBag(viewModelModifier?.ViewModel);
            var primaryCursorModifier = editContext.GetPrimaryCursorModifier(cursorModifierBag);

            if (modelModifier is null || viewModelModifier is null || cursorModifierBag is null || primaryCursorModifier is null)
                return;

            var cursorPositionIndex = modelModifier.GetPositionIndex(primaryCursorModifier);

            if (commandArgs.ShouldSelectText)
            {
                if (!TextEditorSelectionHelper.HasSelectedText(primaryCursorModifier))
                    primaryCursorModifier.SelectionAnchorPositionIndex = cursorPositionIndex;
            }
            else
            {
                primaryCursorModifier.SelectionAnchorPositionIndex = null;
            }

            var previousCharacter = modelModifier.GetCharacter(cursorPositionIndex - 1);
            var currentCharacter = modelModifier.GetCharacter(cursorPositionIndex);

            char? characterToMatch = null;
            char? match = null;

            var fallbackToPreviousCharacter = false;

            if (CharacterKindHelper.CharToCharacterKind(currentCharacter) == CharacterKind.Punctuation)
            {
                // Prefer current character
                match = KeyboardKeyFacts.MatchPunctuationCharacter(currentCharacter);

                if (match is not null)
                    characterToMatch = currentCharacter;
            }

            if (characterToMatch is null && CharacterKindHelper.CharToCharacterKind(previousCharacter) == CharacterKind.Punctuation)
            {
                // Fallback to the previous current character
                match = KeyboardKeyFacts.MatchPunctuationCharacter(previousCharacter);

                if (match is not null)
                {
                    characterToMatch = previousCharacter;
                    fallbackToPreviousCharacter = true;
                }
            }

            if (characterToMatch is null || match is null)
                return;

            var directionToFindMatchingPunctuationCharacter = KeyboardKeyFacts.DirectionToFindMatchingPunctuationCharacter(
                characterToMatch.Value);

            if (directionToFindMatchingPunctuationCharacter is null)
                return;

            var unmatchedCharacters = fallbackToPreviousCharacter && -1 == directionToFindMatchingPunctuationCharacter
                ? 0
                : 1;

            while (true)
            {
                KeyboardEventArgs keyboardEventArgs;

                if (directionToFindMatchingPunctuationCharacter == -1)
                {
                    keyboardEventArgs = new KeyboardEventArgs
                    {
                        Key = KeyboardKeyFacts.MovementKeys.ARROW_LEFT,
                        ShiftKey = commandArgs.ShouldSelectText,
                    };
                }
                else
                {
                    keyboardEventArgs = new KeyboardEventArgs
                    {
                        Key = KeyboardKeyFacts.MovementKeys.ARROW_RIGHT,
                        ShiftKey = commandArgs.ShouldSelectText,
                    };
                }

                await editContext.TextEditorService.ViewModelApi.MoveCursorUnsafeFactory(
                        keyboardEventArgs,
                        modelModifier.ResourceUri,
                        viewModelModifier.ViewModel.ViewModelKey,
                        primaryCursorModifier)
                    .Invoke(editContext)
					.ConfigureAwait(false);

                var positionIndex = modelModifier.GetPositionIndex(primaryCursorModifier);
                var characterAt = modelModifier.GetCharacter(positionIndex);

                if (characterAt == match)
                    unmatchedCharacters--;
                else if (characterAt == characterToMatch)
                    unmatchedCharacters++;

                if (unmatchedCharacters == 0)
                    break;

                if (positionIndex <= 0 || positionIndex >= modelModifier.CharCount)
                    break;
            }

            if (commandArgs.ShouldSelectText)
                primaryCursorModifier.SelectionEndingPositionIndex = modelModifier.GetPositionIndex(primaryCursorModifier);
        };
    }

    public static TextEditorEdit GoToDefinitionFactory(
        ResourceUri modelResourceUri, Key<TextEditorViewModel> viewModelKey, TextEditorCommandArgs commandArgs)
    {
        return (IEditContext editContext) =>
        {
            var modelModifier = editContext.GetModelModifier(modelResourceUri);
            var viewModelModifier = editContext.GetViewModelModifier(viewModelKey);
            var cursorModifierBag = editContext.GetCursorModifierBag(viewModelModifier?.ViewModel);
            var primaryCursorModifier = editContext.GetPrimaryCursorModifier(cursorModifierBag);

            if (modelModifier is null || viewModelModifier is null || cursorModifierBag is null || primaryCursorModifier is null)
                return Task.CompletedTask;

            if (modelModifier.CompilerService.Binder is null)
                return Task.CompletedTask;

            var positionIndex = modelModifier.GetPositionIndex(primaryCursorModifier);
            var wordTextSpan = modelModifier.GetWordTextSpan(positionIndex);

            if (wordTextSpan is null)
                return Task.CompletedTask;

            var definitionTextSpan = modelModifier.CompilerService.Binder.GetDefinition(wordTextSpan);

            if (definitionTextSpan is null)
                return Task.CompletedTask;

            var definitionModel = commandArgs.TextEditorService.ModelApi.GetOrDefault(definitionTextSpan.ResourceUri);

            if (definitionModel is null)
            {
                if (commandArgs.TextEditorService.TextEditorConfig.RegisterModelFunc is not null)
                {
                    commandArgs.TextEditorService.TextEditorConfig.RegisterModelFunc.Invoke(
                        new RegisterModelArgs(definitionTextSpan.ResourceUri, commandArgs.ServiceProvider));

                    var definitionModelModifier = editContext.GetModelModifier(definitionTextSpan.ResourceUri);

                    if (definitionModel is null)
                        return Task.CompletedTask;
                }
                else
                {
                    return Task.CompletedTask;
                }
            }

            var definitionViewModels = commandArgs.TextEditorService.ModelApi.GetViewModelsOrEmpty(definitionTextSpan.ResourceUri);

            if (!definitionViewModels.Any())
            {
                if (commandArgs.TextEditorService.TextEditorConfig.TryRegisterViewModelFunc is not null)
                {
                    commandArgs.TextEditorService.TextEditorConfig.TryRegisterViewModelFunc.Invoke(new TryRegisterViewModelArgs(
                        Key<TextEditorViewModel>.NewKey(),
                        definitionTextSpan.ResourceUri,
                        new Category("main"),
                        true,
                        commandArgs.ServiceProvider));

                    definitionViewModels = commandArgs.TextEditorService.ModelApi.GetViewModelsOrEmpty(definitionTextSpan.ResourceUri);

                    if (!definitionViewModels.Any())
                        return Task.CompletedTask;
                }
                else
                {
                    return Task.CompletedTask;
                }
            }

            var definitionViewModelKey = definitionViewModels.First().ViewModelKey;
            
            var definitionViewModelModifier = editContext.GetViewModelModifier(definitionViewModelKey);
            var definitionCursorModifierBag = editContext.GetCursorModifierBag(definitionViewModelModifier?.ViewModel);
            var definitionPrimaryCursorModifier = editContext.GetPrimaryCursorModifier(definitionCursorModifierBag);

            if (definitionViewModelModifier is null || definitionCursorModifierBag is null || definitionPrimaryCursorModifier is null)
                return Task.CompletedTask;

            var rowData = definitionModel.GetLineInformationFromPositionIndex(definitionTextSpan.StartingIndexInclusive);
            var columnIndex = definitionTextSpan.StartingIndexInclusive - rowData.StartPositionIndexInclusive;

            definitionPrimaryCursorModifier.SelectionAnchorPositionIndex = null;
            definitionPrimaryCursorModifier.LineIndex = rowData.Index;
            definitionPrimaryCursorModifier.ColumnIndex = columnIndex;
            definitionPrimaryCursorModifier.PreferredColumnIndex = columnIndex;

            if (commandArgs.TextEditorService.TextEditorConfig.TryShowViewModelFunc is not null)
            {
                commandArgs.TextEditorService.TextEditorConfig.TryShowViewModelFunc.Invoke(new TryShowViewModelArgs(
                    definitionViewModelKey,
                    Key<TextEditorGroup>.Empty,
                    commandArgs.ServiceProvider));
            }

            return Task.CompletedTask;
        };
    }

    public static TextEditorEdit ShowFindAllDialogFactory(
        ResourceUri modelResourceUri, Key<TextEditorViewModel> viewModelKey, TextEditorCommandArgs commandArgs)
    {
        return (IEditContext editContext) =>
        {
            commandArgs.TextEditorService.OptionsApi.ShowFindAllDialog();
            return Task.CompletedTask;
        };
    }

    public static TextEditorEdit ShowTooltipByCursorPositionFactory(
        ResourceUri modelResourceUri, Key<TextEditorViewModel> viewModelKey, TextEditorCommandArgs commandArgs)
    {
        return async (IEditContext editContext) =>
        {
            var modelModifier = editContext.GetModelModifier(modelResourceUri);
            var viewModelModifier = editContext.GetViewModelModifier(viewModelKey);
            var cursorModifierBag = editContext.GetCursorModifierBag(viewModelModifier?.ViewModel);
            var primaryCursorModifier = editContext.GetPrimaryCursorModifier(cursorModifierBag);

            if (modelModifier is null || viewModelModifier is null || cursorModifierBag is null || primaryCursorModifier is null)
                return;

            var elementPositionInPixels = await commandArgs.TextEditorService.JsRuntimeTextEditorApi
                .GetBoundingClientRect(viewModelModifier.ViewModel.PrimaryCursorContentId)
                .ConfigureAwait(false);

            elementPositionInPixels = elementPositionInPixels with
            {
                Top = elementPositionInPixels.Top +
                    (.9 * viewModelModifier.ViewModel.CharAndLineMeasurements.LineHeight)
            };

            await HandleMouseStoppedMovingEventAsyncFactory(
					new MouseEventArgs
		            {
		                ClientX = elementPositionInPixels.Left,
		                ClientY = elementPositionInPixels.Top
		            },
					commandArgs.ComponentData,
					commandArgs.ServiceProvider.GetRequiredService<ILuthetusTextEditorComponentRenderers>(),
					modelResourceUri,
					viewModelKey)
				.Invoke(editContext)
				.ConfigureAwait(false);
        };
    }

	/// <summary>The default <see cref="AfterOnKeyDownAsync"/> will provide syntax highlighting, and autocomplete.<br/><br/>The syntax highlighting occurs on ';', whitespace, paste, undo, redo<br/><br/>The autocomplete occurs on LetterOrDigit typed or { Ctrl + Space }. Furthermore, the autocomplete is done via <see cref="IAutocompleteService"/> and the one can provide their own implementation when registering the Luthetus.TextEditor services using <see cref="LuthetusTextEditorConfig.AutocompleteServiceFactory"/></summary>
	public static TextEditorEdit HandleAfterOnKeyDownAsyncFactory(
        ResourceUri resourceUri,
        Key<TextEditorViewModel> viewModelKey,
        KeyboardEventArgs keyboardEventArgs,
		TextEditorComponentData componentData)
    {
        return async editContext =>
        {
            var modelModifier = editContext.GetModelModifier(resourceUri);
            var viewModelModifier = editContext.GetViewModelModifier(viewModelKey);
            var cursorModifierBag = editContext.GetCursorModifierBag(viewModelModifier?.ViewModel);
            var primaryCursorModifier = editContext.GetPrimaryCursorModifier(cursorModifierBag);

            if (modelModifier is null || viewModelModifier is null || cursorModifierBag is null || primaryCursorModifier is null)
                return;

            // Indexing can be invoked and this method still check for syntax highlighting and such
            if (EventUtils.IsAutocompleteIndexerInvoker(keyboardEventArgs))
            {
                _ = Task.Run(async () =>
                {
                    if (primaryCursorModifier.ColumnIndex > 0)
                    {
                        // All keyboardEventArgs that return true from "IsAutocompleteIndexerInvoker"
                        // are to be 1 character long, as well either specific whitespace or punctuation.
                        // Therefore 1 character behind might be a word that can be indexed.
                        var word = modelModifier.ReadPreviousWordOrDefault(
                            primaryCursorModifier.LineIndex,
                            primaryCursorModifier.ColumnIndex);

                        if (word is not null)
                        {
                            await editContext.TextEditorService.AutocompleteIndexer
                                .IndexWordAsync(word)
                                .ConfigureAwait(false);
                        }
                    }
                });
            }

            if (EventUtils.IsAutocompleteMenuInvoker(keyboardEventArgs))
            {
				viewModelModifier.ViewModel = viewModelModifier.ViewModel with
				{
					MenuKind = MenuKind.AutoCompleteMenu
				};
            }
            else if (EventUtils.IsSyntaxHighlightingInvoker(keyboardEventArgs))
            {
                await componentData.ThrottleApplySyntaxHighlighting(modelModifier).ConfigureAwait(false);
            }
        };
    }

	/// <summary>
	/// This method was being used in the 'OnKeyDownBatch.cs' class, which no longer exists.
	/// The replacement for 'OnKeyDownBatch.cs' is 'OnKeyDownLateBatching.cs'.
	///
	/// But, during the replacement process, this method was overlooked.
	///
	/// One would likely want to use this method when appropriate because
	/// it permits every batched keyboard event to individually be given a chance
	/// to trigger 'HandleAfterOnKeyDownAsyncFactory(...)'
	///
	/// Example: a 'space' keyboard event, batched with the letter 'a' keyboard event.
	/// Depending on what 'OnKeyDownLateBatching.cs' does, perhaps it takes the last keyboard event
	/// and uses that to fire 'HandleAfterOnKeyDownAsyncFactory(...)'.
	///
	/// Well, a 'space' keyboard event would have trigger syntax highlighting to be refreshed.
	/// Whereas, the letter 'a' keyboard event won't do anything beyond inserting the letter.
	/// Therefore, the syntax highlighting was erroneously not refreshed due to batching.
	/// This method is intended to solve this problem, but it was forgotten at some point.
	/// </summary>
	public static TextEditorEdit HandleAfterOnKeyDownRangeAsyncFactory(
		ViewModelDisplayOptions viewModelDisplayOptions,
        ResourceUri resourceUri,
        Key<TextEditorViewModel> viewModelKey,
        List<KeyboardEventArgs> keyboardEventArgsList,
		TextEditorComponentData componentData)
    {
        if (viewModelDisplayOptions.AfterOnKeyDownRangeAsyncFactory is not null)
        {
            return viewModelDisplayOptions.AfterOnKeyDownRangeAsyncFactory.Invoke(
                resourceUri,
                viewModelKey,
                keyboardEventArgsList);
        }

        return async editContext =>
        {
            var modelModifier = editContext.GetModelModifier(resourceUri);
            var viewModelModifier = editContext.GetViewModelModifier(viewModelKey);
            var cursorModifierBag = editContext.GetCursorModifierBag(viewModelModifier?.ViewModel);
            var primaryCursorModifier = editContext.GetPrimaryCursorModifier(cursorModifierBag);

            if (modelModifier is null || viewModelModifier is null || cursorModifierBag is null || primaryCursorModifier is null)
                return;

            var seenIsAutocompleteIndexerInvoker = false;
            var seenIsAutocompleteMenuInvoker = false;
            var seenIsSyntaxHighlightingInvoker = false;

            foreach (var keyboardEventArgs in keyboardEventArgsList)
            {
                if (!seenIsAutocompleteIndexerInvoker && EventUtils.IsAutocompleteIndexerInvoker(keyboardEventArgs))
                    seenIsAutocompleteIndexerInvoker = true;

                if (!seenIsAutocompleteMenuInvoker && EventUtils.IsAutocompleteMenuInvoker(keyboardEventArgs))
                    seenIsAutocompleteMenuInvoker = true;
                else if (!seenIsSyntaxHighlightingInvoker && EventUtils.IsSyntaxHighlightingInvoker(keyboardEventArgs))
                    seenIsSyntaxHighlightingInvoker = true;
            }

            if (seenIsAutocompleteIndexerInvoker)
            {
                _ = Task.Run(async () =>
                {
                    if (primaryCursorModifier.ColumnIndex > 0)
                    {
                        // All keyboardEventArgs that return true from "IsAutocompleteIndexerInvoker"
                        // are to be 1 character long, as well either specific whitespace or punctuation.
                        // Therefore 1 character behind might be a word that can be indexed.
                        var word = modelModifier.ReadPreviousWordOrDefault(
                            primaryCursorModifier.LineIndex,
                            primaryCursorModifier.ColumnIndex);

                        if (word is not null)
                        {
                            await editContext.TextEditorService.AutocompleteIndexer
                                .IndexWordAsync(word)
                                .ConfigureAwait(false);
                        }
                    }
                });
            }

            if (seenIsAutocompleteMenuInvoker)
            {
				viewModelModifier.ViewModel = viewModelModifier.ViewModel with
				{
					MenuKind = MenuKind.AutoCompleteMenu
				};
            }

            if (seenIsSyntaxHighlightingInvoker)
            {
                await componentData.ThrottleApplySyntaxHighlighting(modelModifier).ConfigureAwait(false);
            }
        };
    }

	public static TextEditorEdit HandleMouseStoppedMovingEventAsyncFactory(
		MouseEventArgs mouseEventArgs,		
		TextEditorComponentData componentData,
		ILuthetusTextEditorComponentRenderers textEditorComponentRenderers,
        ResourceUri resourceUri,
        Key<TextEditorViewModel> viewModelKey)
    {
		return async editContext =>
		{
			var modelModifier = editContext.GetModelModifier(resourceUri);
	        var viewModelModifier = editContext.GetViewModelModifier(viewModelKey);
	
	        if (modelModifier is null || viewModelModifier is null)
	            return;

	        // Lazily calculate row and column index a second time. Otherwise one has to calculate it every mouse moved event.
	        var rowAndColumnIndex = await EventUtils.CalculateRowAndColumnIndex(
					resourceUri,
					viewModelKey,
					mouseEventArgs,
					componentData,
					editContext)
				.ConfigureAwait(false);

			var textEditorDimensions = viewModelModifier.ViewModel.TextEditorDimensions;
			var scrollbarDimensions = viewModelModifier.ViewModel.ScrollbarDimensions;
		
			var relativeCoordinatesOnClick = new RelativeCoordinates(
			    mouseEventArgs.ClientX - textEditorDimensions.BoundingClientRectLeft,
			    mouseEventArgs.ClientY - textEditorDimensions.BoundingClientRectTop,
			    scrollbarDimensions.ScrollLeft,
			    scrollbarDimensions.ScrollTop);
	
	        var cursorPositionIndex = modelModifier.GetPositionIndex(new TextEditorCursor(
	            rowAndColumnIndex.rowIndex,
	            rowAndColumnIndex.columnIndex,
	            true));
	
	        var foundMatch = false;
	
	        var symbols = modelModifier.CompilerService.GetSymbolsFor(modelModifier.ResourceUri);
	        var diagnostics = modelModifier.CompilerService.GetDiagnosticsFor(modelModifier.ResourceUri);
	
	        if (diagnostics.Length != 0)
	        {
	            foreach (var diagnostic in diagnostics)
	            {
	                if (cursorPositionIndex >= diagnostic.TextSpan.StartingIndexInclusive &&
	                    cursorPositionIndex < diagnostic.TextSpan.EndingIndexExclusive)
	                {
	                    // Prefer showing a diagnostic over a symbol when both exist at the mouse location.
	                    foundMatch = true;
	
	                    var parameterMap = new Dictionary<string, object?>
	                    {
	                        {
	                            nameof(ITextEditorDiagnosticRenderer.Diagnostic),
	                            diagnostic
	                        }
	                    };
	
	                    viewModelModifier.ViewModel = viewModelModifier.ViewModel with
						{
							TooltipViewModel = new(
			                    textEditorComponentRenderers.DiagnosticRendererType,
			                    parameterMap,
			                    relativeCoordinatesOnClick,
			                    null,
			                    componentData.ContinueRenderingTooltipAsync)
						};
	                }
	            }
	        }
	
	        if (!foundMatch && symbols.Length != 0)
	        {
	            foreach (var symbol in symbols)
	            {
	                if (cursorPositionIndex >= symbol.TextSpan.StartingIndexInclusive &&
	                    cursorPositionIndex < symbol.TextSpan.EndingIndexExclusive)
	                {
	                    foundMatch = true;
	
	                    var parameters = new Dictionary<string, object?>
	                    {
	                        {
	                            nameof(ITextEditorSymbolRenderer.Symbol),
	                            symbol
	                        }
	                    };
	
	                    viewModelModifier.ViewModel = viewModelModifier.ViewModel with
						{
							TooltipViewModel = new(
		                        textEditorComponentRenderers.SymbolRendererType,
		                        parameters,
		                        relativeCoordinatesOnClick,
		                        null,
		                        componentData.ContinueRenderingTooltipAsync)
						};
	                }
	            }
	        }
	
	        if (!foundMatch)
	        {
				viewModelModifier.ViewModel = viewModelModifier.ViewModel with
				{
	            	TooltipViewModel = null
				};
	        }
	
	        // TODO: Measure the tooltip, and reposition if it would go offscreen.
		};
    }
}
