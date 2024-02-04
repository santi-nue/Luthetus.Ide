﻿using Luthetus.Common.RazorLib.BackgroundTasks.Models;
using Luthetus.Common.RazorLib.Installations.Models;
using Luthetus.Common.RazorLib.Options.Models;
using Microsoft.AspNetCore.Components;

namespace Luthetus.Common.RazorLib.Installations.Displays;

public partial class LuthetusCommonInitializer : ComponentBase
{
    [Inject]
    private LuthetusCommonConfig CommonConfig { get; set; } = null!;
    [Inject]
    private IAppOptionsService AppOptionsService { get; set; } = null!;
    [Inject]
    private LuthetusHostingInformation LuthetusHostingInformation { get; set; } = null!;
    [Inject]
    private ContinuousBackgroundTaskWorker ContinuousBackgroundTaskWorker { get; set; } = null!;
    [Inject]
    private BlockingBackgroundTaskWorker BlockingBackgroundTaskWorker { get; set; } = null!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            if (LuthetusHostingInformation.LuthetusHostingKind != LuthetusHostingKind.ServerSide &&
                LuthetusHostingInformation.LuthetusHostingKind != LuthetusHostingKind.Photino)
            {
                _ = Task.Run(async () => await ContinuousBackgroundTaskWorker
                            .StartAsync(CancellationToken.None)
                            .ConfigureAwait(false))
                        .ConfigureAwait(false);

                _ = Task.Run(async () => await BlockingBackgroundTaskWorker
                            .StartAsync(CancellationToken.None)
                            .ConfigureAwait(false))
                        .ConfigureAwait(false);
            }

            AppOptionsService.SetActiveThemeRecordKey(CommonConfig.InitialThemeKey, false);
            await AppOptionsService.SetFromLocalStorageAsync().ConfigureAwait(false);
        }

        await base.OnAfterRenderAsync(firstRender).ConfigureAwait(false);
    }
}