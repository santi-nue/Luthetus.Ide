﻿using Microsoft.AspNetCore.Components.Web;
using Luthetus.TextEditor.RazorLib.Rows.Models;
using Luthetus.TextEditor.RazorLib.TextEditors.Models;
using Luthetus.TextEditor.RazorLib.Lexes.Models;
using Luthetus.TextEditor.RazorLib.Cursors.Models;
using Luthetus.TextEditor.RazorLib.Decorations.Models;
using Luthetus.Common.RazorLib.Keys.Models;
using Luthetus.TextEditor.RazorLib.TextEditors.Models.TextEditorModels;

namespace Luthetus.TextEditor.RazorLib.TextEditors.States;

public partial class TextEditorModelState
{
    public record RegisterAction(TextEditorModel Model);
    public record DisposeAction(ResourceUri ResourceUri);
    public record ForceRerenderAction(ResourceUri ResourceUri);
    
    public record RegisterPresentationModelAction(
            ResourceUri ResourceUri,
            TextEditorPresentationModel PresentationModel);
    
    public record UndoEditAction(
            ResourceUri ResourceUri,
            Key<AuthenticatedAction> AuthenticatedActionKey)
        : AuthenticatedAction(AuthenticatedActionKey);

    public record RedoEditAction(
            ResourceUri ResourceUri,
            Key<AuthenticatedAction> AuthenticatedActionKey)
        : AuthenticatedAction(AuthenticatedActionKey);

    public record CalculatePresentationModelAction(
            ResourceUri ResourceUri,
            Key<TextEditorPresentationModel> PresentationKey,
            Key<AuthenticatedAction> AuthenticatedActionKey)
        : AuthenticatedAction(AuthenticatedActionKey);

    public record ReloadAction(
            ResourceUri ResourceUri,
            string Content,
            DateTime ResourceLastWriteTime,
            Key<AuthenticatedAction> AuthenticatedActionKey)
        : AuthenticatedAction(AuthenticatedActionKey);

    public record SetResourceDataAction(
            ResourceUri ResourceUri,
            DateTime ResourceLastWriteTime,
            Key<AuthenticatedAction> AuthenticatedActionKey)
        : AuthenticatedAction(AuthenticatedActionKey);

    public record SetUsingRowEndingKindAction(
            ResourceUri ResourceUri,
            RowEndingKind RowEndingKind,
            Key<AuthenticatedAction> AuthenticatedActionKey)
        : AuthenticatedAction(AuthenticatedActionKey);

    public record KeyboardEventAction(
            ResourceUri ResourceUri,
            Key<TextEditorViewModel>? ViewModelKey,
            TextEditorCursorModifierBag CursorModifierBag,
            KeyboardEventArgs KeyboardEventArgs,
            CancellationToken CancellationToken,
            Key<AuthenticatedAction> AuthenticatedActionKey)
        : AuthenticatedAction(AuthenticatedActionKey);

    public record InsertTextAction(
            ResourceUri ResourceUri,
            Key<TextEditorViewModel>? ViewModelKey,
            TextEditorCursorModifierBag CursorModifierBag,
            string Content,
            CancellationToken CancellationToken,
            Key<AuthenticatedAction> AuthenticatedActionKey)
        : AuthenticatedAction(AuthenticatedActionKey);

    public record DeleteTextByMotionAction(
            ResourceUri ResourceUri,
            Key<TextEditorViewModel>? ViewModelKey,
            TextEditorCursorModifierBag CursorModifierBag,
            MotionKind MotionKind,
            CancellationToken CancellationToken,
            Key<AuthenticatedAction> AuthenticatedActionKey)
        : AuthenticatedAction(AuthenticatedActionKey);

    public record DeleteTextByRangeAction(
            ResourceUri ResourceUri,
            Key<TextEditorViewModel>? ViewModelKey,
            TextEditorCursorModifierBag CursorModifierBag,
            int Count,
            CancellationToken CancellationToken,
            Key<AuthenticatedAction> AuthenticatedActionKey)
        : AuthenticatedAction(AuthenticatedActionKey);
}