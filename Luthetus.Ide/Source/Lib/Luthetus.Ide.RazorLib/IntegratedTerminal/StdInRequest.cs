﻿using Microsoft.AspNetCore.Components.Rendering;

namespace Luthetus.Ide.RazorLib.IntegratedTerminal;

public class StdInRequest : Std
{
    public StdInRequest(IntegratedTerminal integratedTerminal)
        : base(integratedTerminal)
    {
    }

    public bool IsCompleted { get; set; }
    public string Value { get; set; } = string.Empty;

    public override RenderTreeBuilder GetRenderTreeBuilder(RenderTreeBuilder builder, ref int sequence)
    {
        if (!IsCompleted)
        { 
            builder.OpenComponent<StdInInputDisplay>(sequence++);
            builder.AddAttribute(sequence++, nameof(StdInInputDisplay.IntegratedTerminal), _integratedTerminal);
            builder.AddAttribute(sequence++, nameof(StdInInputDisplay.StdInRequest), this);
            builder.CloseComponent();
        }
        else
        {
            builder.OpenElement(sequence++, "div");
            builder.AddAttribute(sequence++, "class", "luth_te_string-literal");
            builder.AddContent(sequence++, Value);
            builder.CloseElement();
        }

        return builder;
    }
}
