﻿using Fluxor;
using Luthetus.Common.RazorLib.KeyCase.Models;
using Luthetus.Ide.RazorLib.TerminalCase.Models;
using System.Collections.Immutable;

namespace Luthetus.Ide.RazorLib.TerminalCase.States;

[FeatureState]
public partial record TerminalSessionState(ImmutableDictionary<Key<TerminalSession>, TerminalSession> TerminalSessionMap)
{
    public TerminalSessionState()
        : this(ImmutableDictionary<Key<TerminalSession>, TerminalSession>.Empty)
    {
    }
}