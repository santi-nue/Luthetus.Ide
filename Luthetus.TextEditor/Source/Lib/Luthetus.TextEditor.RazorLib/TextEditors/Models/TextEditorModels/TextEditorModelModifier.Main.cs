﻿using Luthetus.Common.RazorLib.Keyboards.Models;
using Luthetus.Common.RazorLib.Keymaps.Models;
using Luthetus.Common.RazorLib.Keys.Models;
using Luthetus.Common.RazorLib.RenderStates.Models;
using Luthetus.TextEditor.RazorLib.Characters.Models;
using Luthetus.TextEditor.RazorLib.Commands.Models;
using Luthetus.TextEditor.RazorLib.CompilerServices.Interfaces;
using Luthetus.TextEditor.RazorLib.Cursors.Models;
using Luthetus.TextEditor.RazorLib.Decorations.Models;
using Luthetus.TextEditor.RazorLib.Edits.Models;
using Luthetus.TextEditor.RazorLib.Lexes.Models;
using Luthetus.TextEditor.RazorLib.Options.Models;
using Luthetus.TextEditor.RazorLib.Rows.Models;
using Microsoft.AspNetCore.Components.Web;
using System.Collections.Immutable;

namespace Luthetus.TextEditor.RazorLib.TextEditors.Models.TextEditorModels;

/// <summary>
/// Any modified state needs to be 'null coallesce assigned' to the existing TextEditorModel's value
///
/// When reading state, if the state had been 'null coallesce assigned' then the field will
/// be read. Otherwise, the existing TextEditorModel's value will be read.
/// <br/><br/>
/// <inheritdoc cref="ITextEditorModel"/>
/// </summary>
public partial class TextEditorModelModifier
{
    private readonly TextEditorModel _textEditorModel;

    public TextEditorModelModifier(TextEditorModel model)
    {
        if (model.PartitionSize < 2)
            throw new ApplicationException($"{nameof(model)}.{nameof(PartitionSize)} must be >= 2");

        PartitionSize = model.PartitionSize;
        WasDirty = model.IsDirty;

        _isDirty = model.IsDirty;

        _textEditorModel = model;
        _partitionList = _textEditorModel.PartitionList;
    }

    /// <summary>
    /// TODO: Awkward naming convention is being used here. This is an expression bound property,...
    ///       ...with the naming conventions of a private field.
    ///       |
    ///       The reason for breaking convention here is that every other purpose of this type is done
    ///       through a private field.
    ///       |
    ///       Need to revisit naming this.
    /// </summary>
    private ImmutableList<char> _charList => _partitionList.SelectMany(x => x.CharList).ToImmutableList();
    /// <summary>
    /// TODO: Awkward naming convention is being used here. This is an expression bound property,...
    ///       ...with the naming conventions of a private field.
    ///       |
    ///       The reason for breaking convention here is that every other purpose of this type is done
    ///       through a private field.
    ///       |
    ///       Need to revisit naming this.
    /// </summary>
    private ImmutableList<byte> _decorationByteList => _partitionList.SelectMany(x => x.DecorationByteList).ToImmutableList();
    private ImmutableList<TextEditorPartition> _partitionList = new TextEditorPartition[] { TextEditorPartition.Empty }.ToImmutableList();

    private List<EditBlock>? _editBlocksList;
    private List<LineEnd>? _lineEndingPositionsList;
    private List<(LineEndKind rowEndingKind, int count)>? _lineEndingKindCountsList;
    private List<TextEditorPresentationModel>? _presentationModelsList;
    private List<int>? _tabKeyPositionsList;

    private LineEndKind? _onlyLineEndKind;
    /// <summary>
    /// Awkward special case here: <see cref="_onlyLineEndKind"/> is allowed to be null.
    /// So, the design of this class where null means unmodified, doesn't work well here.
    /// </summary>
    private bool _onlyLineEndKindWasModified;

    private LineEndKind? _usingLineEndKind;
    private ResourceUri? _resourceUri;
    private DateTime? _resourceLastWriteTime;
    private string? _fileExtension;
    private IDecorationMapper? _decorationMapper;
    private ILuthCompilerService? _compilerService;
    private TextEditorSaveFileHelper? _textEditorSaveFileHelper;
    private int? _editBlockIndex;
    private bool _isDirty;
    private (int rowIndex, int rowLength)? _mostCharactersOnASingleLineTuple;
    private Key<RenderState>? _renderStateKey = Key<RenderState>.NewKey();
    private Keymap? _textEditorKeymap;
    private TextEditorOptions? _textEditorOptions;

    /// <summary>
    /// This property optimizes the dirty state tracking. If _wasDirty != _isDirty then track the state change.
    /// This involves writing to dependency injectable state, then triggering a re-render in the <see cref="Edits.Displays.DirtyResourceUriInteractiveIconDisplay"/>
    /// </summary>
    public bool WasDirty { get; }

    private int PartitionSize { get; }
    public bool WasModified { get; internal set; }

    public TextEditorModel ToModel()
    {
        return new TextEditorModel(
            _charList is null ? _textEditorModel.CharList : _charList,
            _decorationByteList is null ? _textEditorModel.DecorationByteList : _decorationByteList,
            PartitionSize,
            _partitionList is null ? _textEditorModel.PartitionList : _partitionList,
            _editBlocksList is null ? _textEditorModel.EditBlocksList : _editBlocksList.ToImmutableList(),
            _lineEndingPositionsList is null ? _textEditorModel.LineEndPositionList : _lineEndingPositionsList.ToImmutableList(),
            _lineEndingKindCountsList is null ? _textEditorModel.LineEndKindCountList : _lineEndingKindCountsList.ToImmutableList(),
            _presentationModelsList is null ? _textEditorModel.PresentationModelList : _presentationModelsList.ToImmutableList(),
            _tabKeyPositionsList is null ? _textEditorModel.TabKeyPositionsList : _tabKeyPositionsList.ToImmutableList(),
            _onlyLineEndKindWasModified ? _onlyLineEndKind : _textEditorModel.OnlyLineEndKind,
            _usingLineEndKind ?? _textEditorModel.UsingLineEndKind,
            _resourceUri ?? _textEditorModel.ResourceUri,
            _resourceLastWriteTime ?? _textEditorModel.ResourceLastWriteTime,
            _fileExtension ?? _textEditorModel.FileExtension,
            _decorationMapper ?? _textEditorModel.DecorationMapper,
            _compilerService ?? _textEditorModel.CompilerService,
            _textEditorSaveFileHelper ?? _textEditorModel.TextEditorSaveFileHelper,
            _editBlockIndex ?? _textEditorModel.EditBlockIndex,
            IsDirty,
            _mostCharactersOnASingleLineTuple ?? _textEditorModel.MostCharactersOnASingleLineTuple,
            _renderStateKey ?? _textEditorModel.RenderStateKey);
    }

	public void ClearContentList()
    {
        _partitionList = new TextEditorPartition[] { TextEditorPartition.Empty }.ToImmutableList();
        SetIsDirtyTrue();
    }

    public void ClearRowEndingPositionsList()
    {
        _lineEndingPositionsList = new();
    }

    public void ClearRowEndingKindCountsList()
    {
        _lineEndingKindCountsList = new();
    }

    public void ClearTabKeyPositionsList()
    {
        _tabKeyPositionsList = new();
    }

    public void ClearOnlyRowEndingKind()
    {
        _onlyLineEndKind = null;
        _onlyLineEndKindWasModified = true;
    }

    public void SetUsingLineEndKind(LineEndKind rowEndingKind)
    {
        _usingLineEndKind = rowEndingKind;
    }

    public void SetResourceData(ResourceUri resourceUri, DateTime resourceLastWriteTime)
    {
        _resourceUri = resourceUri;
        _resourceLastWriteTime = resourceLastWriteTime;
    }

    public void SetDecorationMapper(IDecorationMapper decorationMapper)
    {
        _decorationMapper = decorationMapper;
    }

    public void SetCompilerService(ILuthCompilerService compilerService)
    {
        _compilerService = compilerService;
    }

    public void SetTextEditorSaveFileHelper(TextEditorSaveFileHelper textEditorSaveFileHelper)
    {
        _textEditorSaveFileHelper = textEditorSaveFileHelper;
    }

    public void PerformDeletions(
        KeyboardEventArgs keyboardEventArgs,
        TextEditorCursorModifierBag cursorModifierBag,
        int count,
        CancellationToken cancellationToken)
    {
        SetIsDirtyTrue();

        // Any modified state needs to be 'null coallesce assigned' to the existing TextEditorModel's value. When reading state, if the state had been 'null coallesce assigned' then the field will be read. Otherwise, the existing TextEditorModel's value will be read.
        {
            _lineEndingPositionsList ??= _textEditorModel.LineEndPositionList.ToList();
            _tabKeyPositionsList ??= _textEditorModel.TabKeyPositionsList.ToList();
            _mostCharactersOnASingleLineTuple ??= _textEditorModel.MostCharactersOnASingleLineTuple;
        }

        EnsureUndoPoint(TextEditKind.Deletion);

        // Awkward batching foreach loop. Need to continue improving this.
        // Multi-cursor logic also causes some oddities.
        // Can one do per multi-cursor the batch entirely then move on to the next cursor?
        // It is believed one would need to iterate the cursors per batch entry instead. These don't seem equivalent.
        foreach (var _ in Enumerable.Range(0, count)) 
        {
            foreach (var cursorModifier in cursorModifierBag.List)
            {
                var lineStartPositionIndexInclusive = this.GetLineStartPositionIndexInclusive(cursorModifier.LineIndex);
                var cursorPositionIndex = lineStartPositionIndexInclusive + cursorModifier.ColumnIndex;

                // If cursor is out of bounds then continue
                if (cursorPositionIndex > _charList.Count)
                    continue;

                int startingPositionIndexToRemoveInclusive;
                int countToRemove;
                bool moveBackwards;

                // Cannot calculate this after text was deleted - it would be wrong
                int? selectionUpperBoundRowIndex = null;
                // Needed for when text selection is deleted
                (int rowIndex, int columnIndex)? selectionLowerBoundIndexCoordinates = null;

                // TODO: The deletion logic should be the same whether it be 'Delete' 'Backspace' 'CtrlModified' or 'DeleteSelection'. What should change is one needs to calculate the starting and ending index appropriately foreach case.
                if (TextEditorSelectionHelper.HasSelectedText(cursorModifier))
                {
                    var lowerPositionIndexInclusiveBound = cursorModifier.SelectionAnchorPositionIndex ?? 0;
                    var upperPositionIndexExclusive = cursorModifier.SelectionEndingPositionIndex;

                    if (lowerPositionIndexInclusiveBound > upperPositionIndexExclusive)
                        (lowerPositionIndexInclusiveBound, upperPositionIndexExclusive) = (upperPositionIndexExclusive, lowerPositionIndexInclusiveBound);

                    var lowerRowMetaData = this.GetLineInformationFromPositionIndex(lowerPositionIndexInclusiveBound);
                    var upperRowMetaData = this.GetLineInformationFromPositionIndex(upperPositionIndexExclusive);

                    // Value is needed when knowing what row ending positions to update after deletion is done
                    selectionUpperBoundRowIndex = upperRowMetaData.LineIndex;

                    // Value is needed when knowing where to position the cursor after deletion is done
                    selectionLowerBoundIndexCoordinates = (lowerRowMetaData.LineIndex,
                        lowerPositionIndexInclusiveBound - lowerRowMetaData.LineStartPositionIndexInclusive);

                    startingPositionIndexToRemoveInclusive = upperPositionIndexExclusive - 1;
                    countToRemove = upperPositionIndexExclusive - lowerPositionIndexInclusiveBound;
                    moveBackwards = true;

                    cursorModifier.SelectionAnchorPositionIndex = null;
                }
                else if (KeyboardKeyFacts.MetaKeys.BACKSPACE == keyboardEventArgs.Key)
                {
                    moveBackwards = true;

                    if (keyboardEventArgs.CtrlKey)
                    {
                        var columnIndexOfCharacterWithDifferingKind = this.GetColumnIndexOfCharacterWithDifferingKind(
                            cursorModifier.LineIndex,
                            cursorModifier.ColumnIndex,
                            moveBackwards);

                        columnIndexOfCharacterWithDifferingKind = columnIndexOfCharacterWithDifferingKind == -1
                            ? 0
                            : columnIndexOfCharacterWithDifferingKind;

                        countToRemove = cursorModifier.ColumnIndex -
                            columnIndexOfCharacterWithDifferingKind;

                        countToRemove = countToRemove == 0
                            ? 1
                            : countToRemove;
                    }
                    else
                    {
                        countToRemove = 1;
                    }

                    startingPositionIndexToRemoveInclusive = cursorPositionIndex - 1;
                }
                else if (KeyboardKeyFacts.MetaKeys.DELETE == keyboardEventArgs.Key)
                {
                    moveBackwards = false;

                    if (keyboardEventArgs.CtrlKey)
                    {
                        var columnIndexOfCharacterWithDifferingKind = this.GetColumnIndexOfCharacterWithDifferingKind(
                            cursorModifier.LineIndex,
                            cursorModifier.ColumnIndex,
                            moveBackwards);

                        columnIndexOfCharacterWithDifferingKind = columnIndexOfCharacterWithDifferingKind == -1
                            ? this.GetLengthOfLine(cursorModifier.LineIndex)
                            : columnIndexOfCharacterWithDifferingKind;

                        countToRemove = columnIndexOfCharacterWithDifferingKind -
                            cursorModifier.ColumnIndex;

                        countToRemove = countToRemove == 0
                            ? 1
                            : countToRemove;
                    }
                    else
                    {
                        countToRemove = 1;
                    }

                    startingPositionIndexToRemoveInclusive = cursorPositionIndex;
                }
                else
                {
                    throw new ApplicationException($"The keyboard key: {keyboardEventArgs.Key} was not recognized");
                }

                var charactersRemovedCount = 0;
                var rowsRemovedCount = 0;

                var indexToRemove = startingPositionIndexToRemoveInclusive;

                while (countToRemove-- > 0)
                {
                    if (indexToRemove < 0 || indexToRemove > _charList.Count - 1)
                        break;

                    var charToDelete = _charList[indexToRemove];

                    int startingIndexToRemoveRange;
                    int countToRemoveRange;

                    if (KeyboardKeyFacts.IsLineEndingCharacter(charToDelete))
                    {
                        rowsRemovedCount++;

                        // rep.positionIndex == indexToRemove + 1
                        //     ^is for backspace
                        //
                        // rep.positionIndex == indexToRemove + 2
                        //     ^is for delete
                        var rowEndingTupleIndex = _lineEndingPositionsList.FindIndex(rep =>
                            rep.EndPositionIndexExclusive == indexToRemove + 1 ||
                            rep.EndPositionIndexExclusive == indexToRemove + 2);

                        var rowEndingTuple = LineEndPositionList[rowEndingTupleIndex];

                        LineEndPositionList.RemoveAt(rowEndingTupleIndex);

                        var lengthOfRowEnding = rowEndingTuple.LineEndKind.AsCharacters().Length;

                        if (moveBackwards)
                            startingIndexToRemoveRange = indexToRemove - (lengthOfRowEnding - 1);
                        else
                            startingIndexToRemoveRange = indexToRemove;

                        countToRemove -= lengthOfRowEnding - 1;
                        countToRemoveRange = lengthOfRowEnding;

                        MutateRowEndingKindCount(rowEndingTuple.LineEndKind, -1);
                    }
                    else
                    {
                        if (charToDelete == KeyboardKeyFacts.WhitespaceCharacters.TAB)
                            TabKeyPositionsList.Remove(indexToRemove);

                        startingIndexToRemoveRange = indexToRemove;
                        countToRemoveRange = 1;
                    }

                    charactersRemovedCount += countToRemoveRange;

                    __RemoveRange(startingIndexToRemoveRange, countToRemoveRange);

                    if (moveBackwards)
                        indexToRemove -= countToRemoveRange;
                }

                if (charactersRemovedCount == 0 && rowsRemovedCount == 0)
                    return;

                if (moveBackwards && !selectionUpperBoundRowIndex.HasValue)
                {
                    var startCurrentRowPositionIndex = this.GetLineStartPositionIndexInclusive(
                        cursorModifier.LineIndex - rowsRemovedCount);

                    var endingPositionIndex = cursorPositionIndex - charactersRemovedCount;
                
                    cursorModifier.LineIndex -= rowsRemovedCount;
                    cursorModifier.SetColumnIndexAndPreferred(endingPositionIndex - startCurrentRowPositionIndex);
                }

                int firstRowIndexToModify;

                if (selectionUpperBoundRowIndex.HasValue)
                {
                    firstRowIndexToModify = selectionLowerBoundIndexCoordinates!.Value.rowIndex;
                    cursorModifier.LineIndex = selectionLowerBoundIndexCoordinates!.Value.rowIndex;
                    cursorModifier.SetColumnIndexAndPreferred(selectionLowerBoundIndexCoordinates!.Value.columnIndex);
                }
                else
                {
                    firstRowIndexToModify = cursorModifier.LineIndex;
                }

                for (var i = firstRowIndexToModify; i < LineEndPositionList.Count; i++)
                {
                    var rowEndingTuple = LineEndPositionList[i];
                    rowEndingTuple.StartPositionIndexInclusive -= charactersRemovedCount;
                    rowEndingTuple.EndPositionIndexExclusive -= charactersRemovedCount;
                }

                var firstTabKeyPositionIndexToModify = _tabKeyPositionsList.FindIndex(x => x >= startingPositionIndexToRemoveInclusive);

                if (firstTabKeyPositionIndexToModify != -1)
                {
                    for (var i = firstTabKeyPositionIndexToModify; i < TabKeyPositionsList.Count; i++)
                    {
                        TabKeyPositionsList[i] -= charactersRemovedCount;
                    }
                }

                // Reposition the Diagnostic Squigglies
                {
                    var textSpanForInsertion = new TextEditorTextSpan(
                        cursorPositionIndex,
                        cursorPositionIndex + charactersRemovedCount,
                        0,
                        new(string.Empty),
                        string.Empty);

                    var textModification = new TextEditorTextModification(false, textSpanForInsertion);

                    foreach (var presentationModel in PresentationModelsList)
                    {
                        presentationModel.CompletedCalculation?.TextModificationsSinceRequestList.Add(textModification);
                        presentationModel.PendingCalculation?.TextModificationsSinceRequestList.Add(textModification);
                    }
                }
            }

            // TODO: Fix tracking the MostCharactersOnASingleRowTuple this way is possibly inefficient - should instead only check the rows that changed
            {
                (int rowIndex, int rowLength) localMostCharactersOnASingleRowTuple = (0, 0);

                for (var i = 0; i < LineEndPositionList.Count; i++)
                {
                    var lengthOfRow = this.GetLengthOfLine(i);

                    if (lengthOfRow > localMostCharactersOnASingleRowTuple.rowLength)
                    {
                        localMostCharactersOnASingleRowTuple = (i, lengthOfRow);
                    }
                }

                localMostCharactersOnASingleRowTuple = (localMostCharactersOnASingleRowTuple.rowIndex,
                    localMostCharactersOnASingleRowTuple.rowLength + TextEditorModel.MOST_CHARACTERS_ON_A_SINGLE_ROW_MARGIN);

                _mostCharactersOnASingleLineTuple = localMostCharactersOnASingleRowTuple;
            }
        }
    }

    public void SetContent(string content)
    {
        SetIsDirtyTrue();

        // Any modified state needs to be 'null coallesce assigned' to the existing TextEditorModel's value. When reading state, if the state had been 'null coallesce assigned' then the field will be read. Otherwise, the existing TextEditorModel's value will be read.
        {
            _mostCharactersOnASingleLineTuple ??= _textEditorModel.MostCharactersOnASingleLineTuple;
            _lineEndingPositionsList ??= _textEditorModel.LineEndPositionList.ToList();
            _tabKeyPositionsList ??= _textEditorModel.TabKeyPositionsList.ToList();
            _lineEndingKindCountsList ??= _textEditorModel.LineEndKindCountList.ToList();
        }

        ClearAllStatesButEditHistory();

        var rowIndex = 0;
        var previousCharacter = '\0';

        var charactersOnRow = 0;

        var carriageReturnCount = 0;
        var linefeedCount = 0;
        var carriageReturnLinefeedCount = 0;

        for (var index = 0; index < content.Length; index++)
        {
            var character = content[index];

            charactersOnRow++;

            if (character == KeyboardKeyFacts.WhitespaceCharacters.CARRIAGE_RETURN)
            {
                if (charactersOnRow > MostCharactersOnASingleLineTuple.lineLength - TextEditorModel.MOST_CHARACTERS_ON_A_SINGLE_ROW_MARGIN)
                    _mostCharactersOnASingleLineTuple = (rowIndex, charactersOnRow + TextEditorModel.MOST_CHARACTERS_ON_A_SINGLE_ROW_MARGIN);

                LineEndPositionList.Add(new(index, index + 1, LineEndKind.CarriageReturn));
                rowIndex++;

                charactersOnRow = 0;

                carriageReturnCount++;
            }
            else if (character == KeyboardKeyFacts.WhitespaceCharacters.NEW_LINE)
            {
                if (charactersOnRow > MostCharactersOnASingleLineTuple.lineLength - TextEditorModel.MOST_CHARACTERS_ON_A_SINGLE_ROW_MARGIN)
                    _mostCharactersOnASingleLineTuple = (rowIndex, charactersOnRow + TextEditorModel.MOST_CHARACTERS_ON_A_SINGLE_ROW_MARGIN);

                if (previousCharacter == KeyboardKeyFacts.WhitespaceCharacters.CARRIAGE_RETURN)
                {
                    var lineEnding = LineEndPositionList[rowIndex - 1];
                    lineEnding.EndPositionIndexExclusive++;
                    lineEnding.LineEndKind = LineEndKind.CarriageReturnLineFeed;

                    carriageReturnCount--;
                    carriageReturnLinefeedCount++;
                }
                else
                {
                    LineEndPositionList.Add(new (index, index + 1, LineEndKind.LineFeed));
                    rowIndex++;

                    linefeedCount++;
                }

                charactersOnRow = 0;
            }

            if (character == KeyboardKeyFacts.WhitespaceCharacters.TAB)
                TabKeyPositionsList.Add(index);

            previousCharacter = character;
        }

        __InsertRange(0, content.Select(x => new RichCharacter
        {
            Value = x,
            DecorationByte = default,
        }));

        _lineEndingKindCountsList.AddRange(new List<(LineEndKind rowEndingKind, int count)>
        {
            (LineEndKind.CarriageReturn, carriageReturnCount),
            (LineEndKind.LineFeed, linefeedCount),
            (LineEndKind.CarriageReturnLineFeed, carriageReturnLinefeedCount),
        });

        CheckRowEndingPositions(true);

        LineEndPositionList.Add(new(content.Length, content.Length, LineEndKind.EndOfFile));
    }

    public void ClearAllStatesButEditHistory()
    {
        ClearContentList();
        ClearRowEndingKindCountsList();
        ClearRowEndingPositionsList();
        ClearTabKeyPositionsList();
        ClearOnlyRowEndingKind();
        SetUsingLineEndKind(LineEndKind.Unset);
    }

    public void HandleKeyboardEvent(
        KeyboardEventArgs keyboardEventArgs,
        TextEditorCursorModifierBag cursorModifierBag,
        CancellationToken cancellationToken)
    {
        if (KeyboardKeyFacts.IsMetaKey(keyboardEventArgs))
        {
            if (KeyboardKeyFacts.MetaKeys.BACKSPACE == keyboardEventArgs.Key ||
                KeyboardKeyFacts.MetaKeys.DELETE == keyboardEventArgs.Key)
            {
                PerformDeletions(
                    keyboardEventArgs,
                    cursorModifierBag,
                    1,
                    cancellationToken);
            }
        }
        else
        {
            for (int i = cursorModifierBag.List.Count - 1; i >= 0; i--)
            {
                var cursor = cursorModifierBag.List[i];

                var singledCursorModifierBag = new TextEditorCursorModifierBag(
                    cursorModifierBag.ViewModelKey,
                    new List<TextEditorCursorModifier> { cursor });

                var valueToInsert = keyboardEventArgs.Key.First().ToString();

                if (keyboardEventArgs.Code == KeyboardKeyFacts.WhitespaceCodes.ENTER_CODE)
                    valueToInsert = UsingLineEndKind.AsCharacters();
                else if (keyboardEventArgs.Code == KeyboardKeyFacts.WhitespaceCodes.TAB_CODE)
                    valueToInsert = "\t";

                Insert(
                    valueToInsert,
                    singledCursorModifierBag,
                    cancellationToken);
            }
        }
    }

    /// <summary>
    /// Use <see cref="Insert(string, TextEditorCursorModifierBag, CancellationToken)"/> instead.
    /// This version is unsafe because it won't respect the user's cursor.
    /// </summary>
    public void Insert_Unsafe(
        string value,
        int rowIndex,
        int columnIndex,
        CancellationToken cancellationToken)
    {
        var cursor = new TextEditorCursor(rowIndex, columnIndex, true);
        var cursorModifier = new TextEditorCursorModifier(cursor);

        var cursorModifierBag = new TextEditorCursorModifierBag(
            Key<TextEditorViewModel>.Empty,
            new List<TextEditorCursorModifier>() { cursorModifier });

        Insert(value, cursorModifierBag, cancellationToken);
    }

    public void Insert(
        string value,
        TextEditorCursorModifierBag cursorModifierBag,
        CancellationToken cancellationToken)
    {
        SetIsDirtyTrue();

        // Any modified state needs to be 'null coallesce assigned' to the existing TextEditorModel's value. When reading state, if the state had been 'null coallesce assigned' then the field will be read. Otherwise, the existing TextEditorModel's value will be read.
        {
            _lineEndingPositionsList ??= _textEditorModel.LineEndPositionList.ToList();
            _tabKeyPositionsList ??= _textEditorModel.TabKeyPositionsList.ToList();
            _mostCharactersOnASingleLineTuple ??= _textEditorModel.MostCharactersOnASingleLineTuple;
        }

        EnsureUndoPoint(TextEditKind.Insertion);

        for (int i = cursorModifierBag.List.Count - 1; i >= 0; i--)
        {
            var cursorModifier = cursorModifierBag.List[i];

            if (TextEditorSelectionHelper.HasSelectedText(cursorModifier))
            {
                var selectionBounds = TextEditorSelectionHelper.GetSelectionBounds(cursorModifier);

                var lowerRowData = this.GetLineInformationFromPositionIndex(selectionBounds.lowerPositionIndexInclusive);
                var lowerColumnIndex = selectionBounds.lowerPositionIndexInclusive - lowerRowData.LineStartPositionIndexInclusive;

                PerformDeletions(
                    new KeyboardEventArgs
                    {
                        Code = KeyboardKeyFacts.MetaKeys.DELETE,
                        Key = KeyboardKeyFacts.MetaKeys.DELETE,
                    },
                    cursorModifierBag,
                    1,
                    CancellationToken.None);

                // Move cursor to lower bound of text selection
                cursorModifier.LineIndex = lowerRowData.LineIndex;
                cursorModifier.SetColumnIndexAndPreferred(lowerColumnIndex);
            }

            // Validate the text to be inserted
            {
                // TODO: Do not convert '\r' and '\r\n' to '\n'. Instead take the string and insert it...
                //       ...as it was given.
                //       |
                //       The reason for converting to '\n' is, if one inserts a carriage return character,
                //       meanwhile the text editor model happens to have a line feed character at the position
                //       you are inserting (example: Insert("\r", ...)).
                //       |
                //       Then, the '\r' takes the position of the '\n', and the '\n' is shifted further
                //       by 1 position in order to allow space the '\r'.
                //       |
                //       Well, now the text editor model sees its contents as "\r\n".
                //       What is to be done in this scenario?
                //
                // The order of these replacements is important.
                value = value
                    .Replace("\r\n", "\n")
                    .Replace("\r", "\n");
            }

            // Remember the cursorPositionIndex
            var startOfRowPositionIndex = this.GetLineStartPositionIndexInclusive(cursorModifier.LineIndex);
            var cursorPositionIndex = startOfRowPositionIndex + cursorModifier.ColumnIndex;

            // Track metadata with the cursorModifier itself
            //
            // Metadata must be done prior to 'InsertValue'
            InsertMetadata(value, cursorModifier, cancellationToken);

            // Now the text still needs to be inserted.
            // The cursorModifier is invalid, because the metadata step moved its position.
            // So, use the 'cursorPositionIndex' variable that was calculated prior to the metadata step.
            InsertValue(value, cursorPositionIndex, cancellationToken);
        }
    }

    private void InsertMetadata(
        string value,
        TextEditorCursorModifier cursorModifier,
        CancellationToken cancellationToken)
    {
        // Any modified state needs to be 'null coallesce assigned' to the existing TextEditorModel's value. When reading state, if the state had been 'null coallesce assigned' then the field will be read. Otherwise, the existing TextEditorModel's value will be read.
        {
            _lineEndingPositionsList ??= _textEditorModel.LineEndPositionList.ToList();
            _tabKeyPositionsList ??= _textEditorModel.TabKeyPositionsList.ToList();
            _mostCharactersOnASingleLineTuple ??= _textEditorModel.MostCharactersOnASingleLineTuple;
        }

        var startOfRowPositionIndex = this.GetLineStartPositionIndexInclusive(cursorModifier.LineIndex);
        var cursorPositionIndex = startOfRowPositionIndex + cursorModifier.ColumnIndex;

        // If cursor is out of bounds then continue
        if (cursorPositionIndex > _charList.Count)
            return;

        for (int characterIndex = 0; characterIndex < value.Length; characterIndex++)
        {
            char character = value[characterIndex];

            var isTab = character == '\t';
            var isCarriageReturn = character == '\r';
            var isLinefeed = character == '\n';
            var isCarriageReturnLineFeed = isCarriageReturn && characterIndex != value.Length - 1 && value[1 + characterIndex] == '\n';

            var characterCountInserted = 1;

            if (isLinefeed || isCarriageReturn)
            {
                // TODO: Do not convert '\r' and '\r\n' to '\n'. Instead take the string and insert it...
                //       ...as it was given.
                //       |
                //       The reason for converting to '\n' is, if one inserts a carriage return character,
                //       meanwhile the text editor model happens to have a line feed character at the position
                //       you are inserting (example: Insert("\r", ...)).
                //       |
                //       Then, the '\r' takes the position of the '\n', and the '\n' is shifted further
                //       by 1 position in order to allow space the '\r'.
                //       |
                //       Well, now the text editor model sees its contents as "\r\n".
                //       What is to be done in this scenario?
                var rowEndingKindToInsert = LineEndKind.LineFeed;

                var richCharacters = rowEndingKindToInsert.AsCharacters().Select(character => new RichCharacter
                {
                    Value = character,
                    DecorationByte = default,
                });

                characterCountInserted = rowEndingKindToInsert.AsCharacters().Length;

                LineEndPositionList.Insert(
                    cursorModifier.LineIndex,
                    new(cursorPositionIndex, cursorPositionIndex + characterCountInserted, rowEndingKindToInsert));

                MutateRowEndingKindCount(rowEndingKindToInsert, 1);

                cursorModifier.LineIndex++;
                cursorModifier.ColumnIndex = 0;
                cursorModifier.PreferredColumnIndex = cursorModifier.ColumnIndex;
            }
            else
            {
                if (isTab)
                {
                    var index = _tabKeyPositionsList.FindIndex(x => x >= cursorPositionIndex);

                    if (index == -1)
                    {
                        _tabKeyPositionsList.Add(cursorPositionIndex);
                    }
                    else
                    {
                        for (var i = index; i < _tabKeyPositionsList.Count; i++)
                        {
                            _tabKeyPositionsList[i]++;
                        }

                        _tabKeyPositionsList.Insert(index, cursorPositionIndex);
                    }
                }

                cursorModifier.SetColumnIndexAndPreferred(1 + cursorModifier.ColumnIndex);
            }

            // Reposition the Row Endings
            {
                for (var i = cursorModifier.LineIndex; i < LineEndPositionList.Count; i++)
                {
                    var rowEndingTuple = LineEndPositionList[i];
                    rowEndingTuple.StartPositionIndexInclusive += characterCountInserted;
                    rowEndingTuple.EndPositionIndexExclusive += characterCountInserted;
                }
            }

            if (!isTab)
            {
                var firstTabKeyPositionIndexToModify = _tabKeyPositionsList.FindIndex(x => x >= cursorPositionIndex);

                if (firstTabKeyPositionIndexToModify != -1)
                {
                    for (var i = firstTabKeyPositionIndexToModify; i < TabKeyPositionsList.Count; i++)
                    {
                        TabKeyPositionsList[i] += characterCountInserted;
                    }
                }
            }

            // Reposition the Diagnostic Squigglies
            {
                var textSpanForInsertion = new TextEditorTextSpan(
                    cursorPositionIndex,
                    cursorPositionIndex + characterCountInserted,
                    0,
                    new(string.Empty),
                    string.Empty);

                var textModification = new TextEditorTextModification(true, textSpanForInsertion);

                foreach (var presentationModel in PresentationModelsList)
                {
                    presentationModel.CompletedCalculation?.TextModificationsSinceRequestList.Add(textModification);
                    presentationModel.PendingCalculation?.TextModificationsSinceRequestList.Add(textModification);
                }
            }

            // TODO: Fix tracking the MostCharactersOnASingleRowTuple this way is possibly inefficient - should instead only check the rows that changed
            {
                (int rowIndex, int rowLength) localMostCharactersOnASingleRowTuple = (0, 0);

                for (var i = 0; i < LineEndPositionList.Count; i++)
                {
                    var lengthOfRow = this.GetLengthOfLine(i);

                    if (lengthOfRow > localMostCharactersOnASingleRowTuple.rowLength)
                    {
                        localMostCharactersOnASingleRowTuple = (i, lengthOfRow);
                    }
                }

                localMostCharactersOnASingleRowTuple = (localMostCharactersOnASingleRowTuple.rowIndex,
                    localMostCharactersOnASingleRowTuple.rowLength + TextEditorModel.MOST_CHARACTERS_ON_A_SINGLE_ROW_MARGIN);

                _mostCharactersOnASingleLineTuple = localMostCharactersOnASingleRowTuple;
            }
        }
    }

    private void InsertValue(
        string value,
        int cursorPositionIndex,
        CancellationToken cancellationToken)
    {
        // If cursor is out of bounds then continue
        if (cursorPositionIndex > _charList.Count)
            return;

        __InsertRange(
            cursorPositionIndex,
            value.Select(character => new RichCharacter
                {
                    Value = character,
                    DecorationByte = 0,
                }));
    }

    public void DeleteTextByMotion(
        MotionKind motionKind,
        TextEditorCursorModifierBag cursorModifierBag,
        CancellationToken cancellationToken)
    {
        var keyboardEventArgs = motionKind switch
        {
            MotionKind.Backspace => new KeyboardEventArgs { Key = KeyboardKeyFacts.MetaKeys.BACKSPACE },
            MotionKind.Delete => new KeyboardEventArgs { Key = KeyboardKeyFacts.MetaKeys.DELETE },
            _ => throw new ApplicationException($"The {nameof(MotionKind)}: {motionKind} was not recognized.")
        };
        
        HandleKeyboardEvent(
            keyboardEventArgs,
            cursorModifierBag,
            CancellationToken.None);
    }

    public void DeleteByRange(
        int count,
        TextEditorCursorModifierBag cursorModifierBag,
        CancellationToken cancellationToken)
    {
        for (int cursorIndex = cursorModifierBag.List.Count - 1; cursorIndex >= 0; cursorIndex--)
        {
            var cursor = cursorModifierBag.List[cursorIndex];

            var singledCursorModifierBag = new TextEditorCursorModifierBag(
                cursorModifierBag.ViewModelKey,
                new List<TextEditorCursorModifier> { cursor });

            // TODO: This needs to be rewritten everything should be deleted at the same time not a foreach loop for each character
            for (var deleteIndex = 0; deleteIndex < count; deleteIndex++)
            {
                HandleKeyboardEvent(
                    new KeyboardEventArgs
                    {
                        Code = KeyboardKeyFacts.MetaKeys.DELETE,
                        Key = KeyboardKeyFacts.MetaKeys.DELETE,
                    },
                    singledCursorModifierBag,
                    CancellationToken.None);
            }
        }
    }

    public void ClearEditBlocks()
    {
        // Any modified state needs to be 'null coallesce assigned' to the existing TextEditorModel's value. When reading state, if the state had been 'null coallesce assigned' then the field will be read. Otherwise, the existing TextEditorModel's value will be read.
        {
            _editBlocksList ??= _textEditorModel.EditBlocksList.ToList();
            _editBlockIndex ??= _textEditorModel.EditBlockIndex;
        }

        _editBlockIndex = 0;
        EditBlockList.Clear();
    }

    /// <summary>
    /// The, "if (EditBlockIndex == _editBlocksPersisted.Count)", is done because the active EditBlock is not yet persisted.<br/><br/>
    /// The active EditBlock is instead being 'created' as the user continues to make edits of the same <see cref="TextEditKind"/>.<br/><br/>
    /// For complete clarity, this comment refers to one possibly expecting to see, "if (EditBlockIndex == _editBlocksPersisted.Count - 1)".
    /// </summary>
    public void UndoEdit()
    {
        // Any modified state needs to be 'null coallesce assigned' to the existing TextEditorModel's value. When reading state, if the state had been 'null coallesce assigned' then the field will be read. Otherwise, the existing TextEditorModel's value will be read.
        {
            _editBlocksList ??= _textEditorModel.EditBlocksList.ToList();
            _editBlockIndex ??= _textEditorModel.EditBlockIndex;
        }

        if (!this.CanUndoEdit())
            return;

        if (EditBlockIndex == EditBlockList.Count)
        {
            // If the edit block is pending then persist it before
            // reverting back to the previous persisted edit block.
            EnsureUndoPoint(TextEditKind.ForcePersistEditBlock);
            _editBlockIndex--;
        }

        _editBlockIndex--;

        var restoreEditBlock = EditBlockList[EditBlockIndex];

        SetContent(restoreEditBlock.ContentSnapshot);
    }

    public void RedoEdit()
    {
        // Any modified state needs to be 'null coallesce assigned' to the existing TextEditorModel's value. When reading state, if the state had been 'null coallesce assigned' then the field will be read. Otherwise, the existing TextEditorModel's value will be read.
        {
            _editBlocksList ??= _textEditorModel.EditBlocksList.ToList();
            _editBlockIndex ??= _textEditorModel.EditBlockIndex;
        }
        
        if (!this.CanRedoEdit())
            return;

        _editBlockIndex++;

        var restoreEditBlock = EditBlockList[EditBlockIndex];

        SetContent(restoreEditBlock.ContentSnapshot);
    }

    public void SetIsDirtyTrue()
    {
        _isDirty = true;
    }
    
    public void SetIsDirtyFalse()
    {
        _isDirty = false;
    }

    public void PerformRegisterPresentationModelAction(
    TextEditorPresentationModel presentationModel)
    {
        // Any modified state needs to be 'null coallesce assigned' to the existing TextEditorModel's value. When reading state, if the state had been 'null coallesce assigned' then the field will be read. Otherwise, the existing TextEditorModel's value will be read.
        {
            _presentationModelsList ??= _textEditorModel.PresentationModelList.ToList();
        }

        if (!PresentationModelsList.Any(x => x.TextEditorPresentationKey == presentationModel.TextEditorPresentationKey))
            PresentationModelsList.Add(presentationModel);
    }

    public void StartPendingCalculatePresentationModel(
        Key<TextEditorPresentationModel> presentationKey,
        TextEditorPresentationModel emptyPresentationModel)
    {
        // Any modified state needs to be 'null coallesce assigned' to the existing TextEditorModel's value. When reading state, if the state had been 'null coallesce assigned' then the field will be read. Otherwise, the existing TextEditorModel's value will be read.
        {
            _presentationModelsList ??= _textEditorModel.PresentationModelList.ToList();
        }

        // If the presentation model has not yet been registered, then this will register it.
        PerformRegisterPresentationModelAction(emptyPresentationModel);

        var indexOfPresentationModel = _presentationModelsList.FindIndex(
            x => x.TextEditorPresentationKey == presentationKey);

        // The presentation model is expected to always be registered at this point.

        var presentationModel = PresentationModelsList[indexOfPresentationModel];
        PresentationModelsList[indexOfPresentationModel] = presentationModel with
        {
            PendingCalculation = new(this.GetAllText())
        };
    }

    public void CompletePendingCalculatePresentationModel(
        Key<TextEditorPresentationModel> presentationKey,
        TextEditorPresentationModel emptyPresentationModel,
        ImmutableArray<TextEditorTextSpan> calculatedTextSpans)
    {
        // Any modified state needs to be 'null coallesce assigned' to the existing TextEditorModel's value. When reading state, if the state had been 'null coallesce assigned' then the field will be read. Otherwise, the existing TextEditorModel's value will be read.
        {
            _presentationModelsList ??= _textEditorModel.PresentationModelList.ToList();
        }

        // If the presentation model has not yet been registered, then this will register it.
        PerformRegisterPresentationModelAction(emptyPresentationModel);

        var indexOfPresentationModel = _presentationModelsList.FindIndex(
            x => x.TextEditorPresentationKey == presentationKey);

        // The presentation model is expected to always be registered at this point.

        var presentationModel = PresentationModelsList[indexOfPresentationModel];

        if (presentationModel.PendingCalculation is null)
            return;

        var calculation = presentationModel.PendingCalculation with
        {
            TextSpanList = calculatedTextSpans
        };

        PresentationModelsList[indexOfPresentationModel] = presentationModel with
        {
            PendingCalculation = null,
            CompletedCalculation = calculation,
        };
    }

    public TextEditorModel ForceRerenderAction()
    {
        return ToModel();
    }

    private void MutateRowEndingKindCount(LineEndKind rowEndingKind, int changeBy)
    {
        // Any modified state needs to be 'null coallesce assigned' to the existing TextEditorModel's value. When reading state, if the state had been 'null coallesce assigned' then the field will be read. Otherwise, the existing TextEditorModel's value will be read.
        {
            _lineEndingKindCountsList ??= _textEditorModel.LineEndKindCountList.ToList();
        }

        var indexOfRowEndingKindCount = _lineEndingKindCountsList.FindIndex(x => x.rowEndingKind == rowEndingKind);
        var currentRowEndingKindCount = LineEndingKindCountsList[indexOfRowEndingKindCount].count;

        LineEndingKindCountsList[indexOfRowEndingKindCount] = (rowEndingKind, currentRowEndingKindCount + changeBy);

        CheckRowEndingPositions(false);
    }

    private void CheckRowEndingPositions(bool setUsingRowEndingKind)
    {
        // Any modified state needs to be 'null coallesce assigned' to the existing TextEditorModel's value. When reading state, if the state had been 'null coallesce assigned' then the field will be read. Otherwise, the existing TextEditorModel's value will be read.
        {
            _lineEndingKindCountsList ??= _textEditorModel.LineEndKindCountList.ToList();
            _onlyLineEndKind ??= _textEditorModel.OnlyLineEndKind;
            _usingLineEndKind ??= _textEditorModel.UsingLineEndKind;
        }

        var existingRowEndingsList = LineEndingKindCountsList
            .Where(x => x.count > 0)
            .ToArray();

        if (!existingRowEndingsList.Any())
        {
            _onlyLineEndKind = LineEndKind.Unset;
            _usingLineEndKind = LineEndKind.LineFeed;
        }
        else
        {
            if (existingRowEndingsList.Length == 1)
            {
                var rowEndingKind = existingRowEndingsList.Single().lineEndingKind;

                if (setUsingRowEndingKind)
                    _usingLineEndKind = rowEndingKind;

                _onlyLineEndKind = rowEndingKind;
            }
            else
            {
                if (setUsingRowEndingKind)
                    _usingLineEndKind = existingRowEndingsList.MaxBy(x => x.count).lineEndingKind;

                _onlyLineEndKind = null;
            }
        }
    }

    private void EnsureUndoPoint(TextEditKind textEditKind, string? otherTextEditKindIdentifier = null)
    {
        // Any modified state needs to be 'null coallesce assigned' to the existing TextEditorModel's value. When reading state, if the state had been 'null coallesce assigned' then the field will be read. Otherwise, the existing TextEditorModel's value will be read.
        {
            _editBlocksList ??= _textEditorModel.EditBlocksList.ToList();
            _editBlockIndex ??= _textEditorModel.EditBlockIndex;
        }

        if (textEditKind == TextEditKind.Other && otherTextEditKindIdentifier is null)
            TextEditorCommand.ThrowOtherTextEditKindIdentifierWasExpectedException(textEditKind);

        var mostRecentEditBlock = EditBlockList.LastOrDefault();

        if (mostRecentEditBlock is null || mostRecentEditBlock.TextEditKind != textEditKind)
        {
            var newEditBlockIndex = EditBlockIndex;

            EditBlockList.Insert(newEditBlockIndex, new EditBlock(
                textEditKind,
                textEditKind.ToString(),
                this.GetAllText(),
                otherTextEditKindIdentifier));

            var removeBlocksStartingAt = newEditBlockIndex + 1;

            _editBlocksList.RemoveRange(removeBlocksStartingAt, EditBlockList.Count - removeBlocksStartingAt);

            _editBlockIndex++;
        }

        while (EditBlockList.Count > TextEditorModel.MAXIMUM_EDIT_BLOCKS && EditBlockList.Count != 0)
        {
            _editBlockIndex--;
            EditBlockList.RemoveAt(0);
        }
    }
}