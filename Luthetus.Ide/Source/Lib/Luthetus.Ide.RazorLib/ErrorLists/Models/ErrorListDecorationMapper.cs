using Luthetus.TextEditor.RazorLib.Decorations.Models;

namespace Luthetus.Ide.RazorLib.ErrorLists.Models;

public class ErrorListDecorationMapper : IDecorationMapper
{
    public string Map(byte decorationByte)
    {
        var decoration = (ErrorListDecorationKind)decorationByte;

        return decoration switch
        {
            ErrorListDecorationKind.None => string.Empty,
            ErrorListDecorationKind.Error => "luth_tree-view-exception",
            _ => string.Empty,
        };
    }
}