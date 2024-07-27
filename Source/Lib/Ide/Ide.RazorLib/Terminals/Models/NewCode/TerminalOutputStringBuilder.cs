namespace Luthetus.Ide.RazorLib.Terminals.Models.NewCode;

public class TerminalOutputStringBuilder : ITerminalOutput
{
	private readonly ITerminal _terminal;

	public TerminalInputTextEditor(ITerminal terminal)
	{
		_terminal = terminal;
	}

	public void OnAfterWorkingDirectoryChanged(string workingDirectoryAbsolutePathString)
	{
	}
	
	public void OnHandleCommandStarting()
	{
	}
	
	public void OnOutput(CommandEvent cmdEvent)
	{
		var output = (string?)null;

		switch (cmdEvent)
		{
			case StartedCommandEvent started:
				// TODO: If the source of the terminal command is a user having...
				//       ...typed themselves, then hitting enter, do not write this out.
				//       |
				//       This is here for when the command was started programmatically
				//       without a user typing into the terminal.
				output = $"{terminalCommand.FormattedCommand.Value}\n";
				break;
			case StandardOutputCommandEvent stdOut:
				output = $"{stdOut.Text}\n";
				break;
			case StandardErrorCommandEvent stdErr:
				output = $"{stdErr.Text}\n";
				break;
			case ExitedCommandEvent exited:
				output = $"Process exited; Code: {exited.ExitCode}\n";
				break;
		}

		if (output is not null)
		{
			var outputTextSpanList = new List<TextEditorTextSpan>();

			if (terminalCommand.OutputParser is not null)
			{
				outputTextSpanList = terminalCommand.OutputParser.OnAfterOutputLine(
					terminalCommand,
					output);
			}
			
			if (terminalCommand.OutputBuilder is null)
			{
				TerminalOnOutput(
					outputOffset,
					output,
					outputTextSpanList,
					terminalCommand,
					terminalCommandBoundary);

				outputOffset += output.Length;
			}
			else
			{
				terminalCommand.OutputBuilder.Append(output);
				terminalCommand.TextSpanList = outputTextSpanList;
			}
		}
	}
}
