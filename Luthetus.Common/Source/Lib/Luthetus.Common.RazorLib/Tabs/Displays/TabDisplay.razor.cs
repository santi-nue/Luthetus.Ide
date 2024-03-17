using Fluxor;
using Luthetus.Common.RazorLib.Dimensions.Models;
using Luthetus.Common.RazorLib.Drags.Displays;
using Luthetus.Common.RazorLib.Drags.Models;
using Luthetus.Common.RazorLib.Resizes.Models;
using Luthetus.Common.RazorLib.JavaScriptObjects.Models;
using Luthetus.Common.RazorLib.Tabs.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Luthetus.Common.RazorLib.Tabs.Displays;

public partial class TabDisplay : ComponentBase, IDisposable
{
	[Inject]
    private IState<DragState> DragStateWrap { get; set; } = null!;
    [Inject]
    private IDispatcher Dispatcher { get; set; } = null!;
	[Inject]
    private IJSRuntime JsRuntime { get; set; } = null!;

	[Parameter, EditorRequired]
	public ITab Tab { get; set; } = null!;

	[Parameter]
	public string CssClassString { get; set; }
	[Parameter]
	public bool ShouldDisplayCloseButton { get; set; } = true;
	[Parameter]
	public string CssStyleString { get; set; }
	[Parameter]
	public bool IsBeingDragged { get; set; }

    private bool _thinksLeftMouseButtonIsDown;
	private Func<(MouseEventArgs firstMouseEventArgs, MouseEventArgs secondMouseEventArgs), Task>? _dragEventHandler;
    private MouseEventArgs? _previousDragMouseEventArgs;

	private string _htmlIdDragged = null;
	private string _htmlId = null;
	private string HtmlId => IsBeingDragged
		? _htmlId ??= $"luth_polymorphic-tab_{Tab.Key}"
		: _htmlIdDragged ??= $"luth_polymorphic-tab-drag_{Tab.Key}";

	private string IsActiveCssClass => Tab.GetIsActive()
		? "luth_active"
	    : string.Empty;

	protected override void OnInitialized()
    {
        DragStateWrap.StateChanged += DragStateWrapOnStateChanged;

        base.OnInitialized();
    }

    private async void DragStateWrapOnStateChanged(object? sender, EventArgs e)
    {
		if (IsBeingDragged)
			return;

        if (!DragStateWrap.Value.ShouldDisplay)
        {
            _dragEventHandler = null;
            _previousDragMouseEventArgs = null;
            _thinksLeftMouseButtonIsDown = false;
        }
        else
        {
            var mouseEventArgs = DragStateWrap.Value.MouseEventArgs;

            if (_dragEventHandler is not null)
            {
                if (_previousDragMouseEventArgs is not null && mouseEventArgs is not null)
                {
                    await _dragEventHandler
                        .Invoke((_previousDragMouseEventArgs, mouseEventArgs));
                }

                _previousDragMouseEventArgs = mouseEventArgs;
                await InvokeAsync(StateHasChanged);
            }
        }
    }

	private async Task CloseTabOnClickAsync()
	{
		if (IsBeingDragged)
			return;

        await Tab.CloseAsync();
	}

	private async Task HandleOnMouseDownAsync(MouseEventArgs mouseEventArgs)
	{
		if (IsBeingDragged)
			return;

		if (mouseEventArgs.Button == 1)
            await CloseTabOnClickAsync();

        _thinksLeftMouseButtonIsDown = true;
	}

	private void HandleOnMouseUp()
    {
		if (IsBeingDragged)
			return;

        _thinksLeftMouseButtonIsDown = false;
    }

	private async Task HandleOnMouseOutAsync(MouseEventArgs mouseEventArgs)
    {
		if (IsBeingDragged)
			return;

		var draggable = Tab.PolymorphicViewModel.DraggableViewModel;

        if (_thinksLeftMouseButtonIsDown && draggable is not null)
        {
			var measuredHtmlElementDimensions = await JsRuntime.InvokeAsync<MeasuredHtmlElementDimensions>(
                "luthetusIde.measureElementById",
                HtmlId);

			await draggable.GetDropzonesAsync();

			// Width
			{
				var widthDimensionAttribute = draggable.ElementDimensions.DimensionAttributeList.First(
	                x => x.DimensionAttributeKind == DimensionAttributeKind.Width);
	
				widthDimensionAttribute.DimensionUnitList.Clear();
	            widthDimensionAttribute.DimensionUnitList.Add(new DimensionUnit
	            {
	                Value = measuredHtmlElementDimensions.WidthInPixels,
	                DimensionUnitKind = DimensionUnitKind.Pixels
	            });
			}

			// Height
			{
				var heightDimensionAttribute = draggable.ElementDimensions.DimensionAttributeList.First(
	                x => x.DimensionAttributeKind == DimensionAttributeKind.Height);
	
				heightDimensionAttribute.DimensionUnitList.Clear();
	            heightDimensionAttribute.DimensionUnitList.Add(new DimensionUnit
	            {
	                Value = measuredHtmlElementDimensions.HeightInPixels,
	                DimensionUnitKind = DimensionUnitKind.Pixels
	            });
			}

			// Left
			{
				var leftDimensionAttribute = draggable.ElementDimensions.DimensionAttributeList.First(
	                x => x.DimensionAttributeKind == DimensionAttributeKind.Left);
	
	            leftDimensionAttribute.DimensionUnitList.Clear();
	            leftDimensionAttribute.DimensionUnitList.Add(new DimensionUnit
	            {
	                Value = mouseEventArgs.ClientX,
	                DimensionUnitKind = DimensionUnitKind.Pixels
	            });
			}

			// Top
			{
				var topDimensionAttribute = draggable.ElementDimensions.DimensionAttributeList.First(
	                x => x.DimensionAttributeKind == DimensionAttributeKind.Top);
	
	            topDimensionAttribute.DimensionUnitList.Clear();
	            topDimensionAttribute.DimensionUnitList.Add(new DimensionUnit
	            {
	                Value = mouseEventArgs.ClientY,
	                DimensionUnitKind = DimensionUnitKind.Pixels
	            });
			}

            draggable.ElementDimensions.ElementPositionKind = ElementPositionKind.Fixed;

            SubscribeToDragEventForScrolling(draggable);
        }
    }

	public void SubscribeToDragEventForScrolling(IDraggableViewModel draggable)
    {
		if (IsBeingDragged)
			return;

        _dragEventHandler = DragEventHandlerAsync;

        Dispatcher.Dispatch(new DragState.WithAction(inState => inState with
        {
            ShouldDisplay = true,
            MouseEventArgs = null,
			DraggableViewModel = draggable,
        }));
    }

	private Task DragEventHandlerAsync(
        (MouseEventArgs firstMouseEventArgs, MouseEventArgs secondMouseEventArgs) mouseEventArgsTuple)
    {
		if (IsBeingDragged)
			return Task.CompletedTask;

        var localThinksLeftMouseButtonIsDown = _thinksLeftMouseButtonIsDown;

        if (!localThinksLeftMouseButtonIsDown)
            return Task.CompletedTask;

		var draggable = Tab.PolymorphicViewModel.DraggableViewModel;

        // Buttons is a bit flag '& 1' gets if left mouse button is held
        if (draggable is not null &&
			localThinksLeftMouseButtonIsDown &&
            (mouseEventArgsTuple.secondMouseEventArgs.Buttons & 1) == 1)
        {
            ResizeHelper.Move(
                draggable.ElementDimensions,
                mouseEventArgsTuple.firstMouseEventArgs,
                mouseEventArgsTuple.secondMouseEventArgs);
        }
        else
        {
            _dragEventHandler = null;
            _previousDragMouseEventArgs = null;
            _thinksLeftMouseButtonIsDown = false;
        }

        return Task.CompletedTask;
    }

	private string GetDraggableCssStyleString()
	{
		var draggable = Tab.PolymorphicViewModel.DraggableViewModel;
		
		if (IsBeingDragged &&
			draggable is not null)
		{
			return draggable.ElementDimensions.StyleString;
		}

		return string.Empty;
	}

	private string GetIsBeingDraggedCssClassString()
	{
		return IsBeingDragged
			? "luth_drag"
			: string.Empty;
	}

	public void Dispose()
    {
        DragStateWrap.StateChanged -= DragStateWrapOnStateChanged;
    }
}