﻿using Luthetus.TextEditor.RazorLib.Lexes.Models;

namespace Luthetus.TextEditor.Tests.Basis.CompilerServices;

/// <summary>
/// Use the <see cref="Id"/> to determine if two diagnostics
/// are reporting the same diagnostic but perhaps with differing
/// messages due to variable interpolation into the string.
/// </summary>
public record TextEditorDiagnosticTests(
    TextEditorDiagnosticLevel DiagnosticLevel,
    string Message,
    TextEditorTextSpan TextSpan,
    Guid Id);