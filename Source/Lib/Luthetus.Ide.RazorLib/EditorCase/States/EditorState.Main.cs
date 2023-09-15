﻿using Fluxor;
using Luthetus.Common.RazorLib.FileSystem.Interfaces;
using Luthetus.TextEditor.RazorLib.Group;

namespace Luthetus.Ide.RazorLib.EditorCase.States;

[FeatureState]
public partial class EditorState
{
    public static readonly TextEditorGroupKey EditorTextEditorGroupKey = TextEditorGroupKey.NewKey();
}