using Luthetus.Common.RazorLib.Menus.Models;

namespace Luthetus.Ide.RazorLib.Shareds.States;

public partial record IdeHeaderState
{
	public record SetMenuFileAction(MenuRecord Menu);
	public record SetMenuToolsAction(MenuRecord Menu);
	public record SetMenuViewAction(MenuRecord Menu);
	public record SetMenuRunAction(MenuRecord Menu);
	
	public record ModifyMenuFileAction(Func<MenuRecord, MenuRecord> MenuFunc);
	public record ModifyMenuToolsAction(Func<MenuRecord, MenuRecord> MenuFunc);
	public record ModifyMenuViewAction(Func<MenuRecord, MenuRecord> MenuFunc);
	public record ModifyMenuRunAction(Func<MenuRecord, MenuRecord> MenuFunc);
}
