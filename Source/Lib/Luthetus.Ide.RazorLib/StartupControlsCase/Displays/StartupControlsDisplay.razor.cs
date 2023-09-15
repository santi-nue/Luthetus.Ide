﻿using Fluxor;
using Fluxor.Blazor.Web.Components;
using Luthetus.Ide.RazorLib.CommandLineCase.Models;
using Luthetus.Ide.RazorLib.ProgramExecutionCase.States;
using Luthetus.Ide.RazorLib.TerminalCase.Models;
using Luthetus.Ide.RazorLib.TerminalCase.States;
using Microsoft.AspNetCore.Components;

namespace Luthetus.Ide.RazorLib.StartupControlsCase.Displays;

public partial class StartupControlsDisplay : FluxorComponent
{
    [Inject]
    private IState<ProgramExecutionState> ProgramExecutionStateWrap { get; set; } = null!;
    [Inject]
    private IState<TerminalSessionRegistry> TerminalSessionsStateWrap { get; set; } = null!;

    private readonly TerminalCommandKey _newDotNetSolutionTerminalCommandKey =
        TerminalCommandKey.NewKey();

    private readonly CancellationTokenSource _newDotNetSolutionCancellationTokenSource = new();

    private async Task StartProgramWithoutDebuggingOnClick()
    {
        var programExecutionState = ProgramExecutionStateWrap.Value;

        if (programExecutionState.StartupProjectAbsolutePath is null)
            return;

        var parentDirectoryAbsolutePathString = programExecutionState.StartupProjectAbsolutePath.ParentDirectory?.FormattedInput;

        if (parentDirectoryAbsolutePathString is null)
            return;

        var formattedCommand = DotNetCliFacts
            .FormatStartProjectWithoutDebugging(
                programExecutionState.StartupProjectAbsolutePath);

        var startProgramWithoutDebuggingCommand = new TerminalCommand(
            _newDotNetSolutionTerminalCommandKey,
            formattedCommand,
            parentDirectoryAbsolutePathString,
            _newDotNetSolutionCancellationTokenSource.Token);

        var executionTerminalSession = TerminalSessionsStateWrap.Value.TerminalSessionMap[
            TerminalSessionFacts.EXECUTION_TERMINAL_SESSION_KEY];

        await executionTerminalSession
            .EnqueueCommandAsync(startProgramWithoutDebuggingCommand);
    }
}