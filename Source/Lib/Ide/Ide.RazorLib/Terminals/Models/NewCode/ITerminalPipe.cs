namespace Luthetus.Ide.RazorLib.Terminals.Models.NewCode;

/// <summary>Transferral of Data</summary>
public interface ITerminalPipe : IDisposable
{
	public void OnWorkingDirectoryChanged();
	public void OnHandleCommandStarting();
}
