﻿using Fluxor;
using Luthetus.Common.RazorLib.Dialog;
using Luthetus.Common.RazorLib.Store.DialogCase;
using Luthetus.Ide.RazorLib.ComponentRenderersCase.Models;
using Luthetus.Ide.RazorLib.HtmlCase.Models;
using static Luthetus.Ide.RazorLib.InputFileCase.States.InputFileState;

namespace Luthetus.Ide.RazorLib.InputFileCase.States;

public partial record InputFileSync
{
    public Task HandleRequestInputFileStateFormAction(
        RequestInputFileStateFormAction requestInputFileStateFormAction,
        IDispatcher dispatcher)
    {
        if (_luthetusIdeComponentRenderers.InputFileRendererType is not null)
        {
            dispatcher.Dispatch(new StartInputFileStateFormAction(
                requestInputFileStateFormAction));

            var inputFileDialog = new DialogRecord(
                DialogFacts.InputFileDialogKey,
                "Input File",
                _luthetusIdeComponentRenderers.InputFileRendererType,
                null,
                HtmlFacts.Classes.DIALOG_PADDING_0)
            {
                IsResizable = true
            };

            dispatcher.Dispatch(new DialogRegistry.RegisterAction(
                inputFileDialog));
        }

        return Task.CompletedTask;
    }
}