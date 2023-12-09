﻿using Luthetus.TextEditor.RazorLib.Decorations.Models;

namespace Luthetus.TextEditor.Tests.Basis.Diffs.Models;

public class TextEditorDiffDecorationMapperTests
{
    public string Map(byte decorationByte)
    {
        var decoration = (TextEditorDiffDecorationKind)decorationByte;

        return decoration switch
        {
            TextEditorDiffDecorationKind.None => string.Empty,
            TextEditorDiffDecorationKind.LongestCommonSubsequence => "luth_te_diff-longest-common-subsequence",
            TextEditorDiffDecorationKind.Insertion => "luth_te_diff-insertion",
            TextEditorDiffDecorationKind.Deletion => "luth_te_diff-deletion",
            TextEditorDiffDecorationKind.Modification => "luth_te_diff-modification",
            _ => string.Empty,
        };
    }
}