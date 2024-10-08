namespace Luthetus.Common.RazorLib.Menus.Models;

public record MenuOptionRecord(
    string DisplayName,
    MenuOptionKind MenuOptionKind,
    Func<Task>? OnClickFunc = null,
    MenuRecord? SubMenu = null,
    Type? WidgetRendererType = null,
    Dictionary<string, object?>? WidgetParameterMap = null);