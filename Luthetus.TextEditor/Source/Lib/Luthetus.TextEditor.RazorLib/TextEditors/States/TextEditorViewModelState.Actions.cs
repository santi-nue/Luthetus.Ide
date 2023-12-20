﻿using Luthetus.Common.RazorLib.Keys.Models;
using Luthetus.TextEditor.RazorLib.Lexes.Models;
using Luthetus.TextEditor.RazorLib.TextEditors.Models;
using Luthetus.TextEditor.RazorLib.TextEditors.Models.TextEditorServices;

namespace Luthetus.TextEditor.RazorLib.TextEditors.States;

public partial class TextEditorViewModelState
{
    public record RegisterAction(
        Key<TextEditorViewModel> ViewModelKey,
        ResourceUri ResourceUri,
        ITextEditorService TextEditorService);

    public record DisposeAction(Key<TextEditorViewModel> ViewModelKey);

    public record SetViewModelWithAction(
            Key<TextEditorViewModel> ViewModelKey,
            Func<TextEditorViewModel, TextEditorViewModel> WithFunc,
            Key<AuthenticatedAction> AuthenticatedActionKey)
        : AuthenticatedAction(AuthenticatedActionKey);
}