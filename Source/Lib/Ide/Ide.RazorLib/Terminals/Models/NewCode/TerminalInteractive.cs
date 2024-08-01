using System.Text;
using CliWrap.EventStream;
using Luthetus.TextEditor.RazorLib.CompilerServices.Utility;
using Luthetus.TextEditor.RazorLib.Lexers.Models;
using Luthetus.TextEditor.RazorLib.CompilerServices.Facts;

namespace Luthetus.Ide.RazorLib.Terminals.Models.NewCode;

public class TerminalInteractive : ITerminalInteractive
{
	private readonly ITerminal _terminal;

	public TerminalInteractive(ITerminal terminal)
	{
		_terminal = terminal;
	}

	private string? _previousWorkingDirectory;
	private string? _workingDirectory;
	
	public string? WorkingDirectory => _workingDirectory;

	public event Action? WorkingDirectoryChanged;
	
	public TerminalCommandParsed? TryHandleCommand(TerminalCommandRequest terminalCommandRequest)
	{
		var parsedCommand = Parse(terminalCommandRequest);
		
		// To set the working directory, is not mutually exclusive
		// to the "cd" command. Do not combine these.
		if (terminalCommandRequest.WorkingDirectory is not null &&
			terminalCommandRequest.WorkingDirectory != WorkingDirectory)
		{
			SetWorkingDirectory(terminalCommandRequest.WorkingDirectory);
		}
		
		switch (parsedCommand.TargetFileName)
		{
			case "cd":
				_terminal.TerminalOutput.WriteOutput(
					parsedCommand,
					new StartedCommandEvent(-1));
			
				SetWorkingDirectory(parsedCommand.Arguments);
				
				_terminal.TerminalOutput.WriteOutput(
					parsedCommand,
					new StandardOutputCommandEvent($"WorkingDirectory set to: '{parsedCommand.Arguments}'\n"));
				return null;
			case "clear":
				_terminal.TerminalOutput.ClearOutput();
				return null;
			default:
				return parsedCommand;
		}
	}
	
	public void SetWorkingDirectory(string workingDirectory)
	{
		_previousWorkingDirectory = _workingDirectory;
        _workingDirectory = workingDirectory;

        if (_previousWorkingDirectory != _workingDirectory)
            WorkingDirectoryChanged?.Invoke();
	}
	
	public TerminalCommandParsed Parse(TerminalCommandRequest terminalCommandRequest)
	{
		try
		{
			var stringWalker = new StringWalker(ResourceUri.Empty, terminalCommandRequest.CommandText);
			
			// Get target file name
			string targetFileName;
			{
				var targetFileNameBuilder = new StringBuilder();
				var startPositionIndex = stringWalker.PositionIndex;
		
				while (!stringWalker.IsEof)
				{
					if (WhitespaceFacts.ALL_LIST.Contains(stringWalker.CurrentCharacter))
						break;
					else
						targetFileNameBuilder.Append(stringWalker.CurrentCharacter);
				
					_ = stringWalker.ReadCharacter();
				}
				
				targetFileName = targetFileNameBuilder.ToString();
			}
			
			// Get arguments
			stringWalker.ReadWhitespace();
			var arguments = stringWalker.RemainingText;
		
			return new TerminalCommandParsed(
				targetFileName,
				arguments,
				terminalCommandRequest);
		}
		catch (Exception e)
		{
			Console.WriteLine(e.ToString());
			throw;
		}
	}
	
	public void Dispose()
	{
		return;
	}
}
