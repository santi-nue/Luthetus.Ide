using Luthetus.Ide.RazorLib.Exceptions;

namespace Luthetus.Ide.RazorLib.Terminals.Models.NewCode;

/// <summary>
/// This is a sort of the built in <see cref="System.Globalization.CultureInfo"/>
/// or <see cref="System.IFormatProvider"/> but specifically for the <see cref="ITerminal"/>.
///
/// There are a variety of ways that the terminal output is being shown,
/// and as such there is no single 'string' value associated with the
/// terminal.
///
/// Instead, through the use this type, one caches their own 'string' value.
/// And, when the terminal has new output, and one tries to read their cached
/// format output, it is first recalculated and cached once again before being returned.
///
/// i.e.: the string value associated with a <see cref="ITerminal"/> is lazily calculated.
/// </summary>
public interface ITerminalOutputFormatter
{
	/// <summary>
	/// This is used to find the respective formatter within a list
	/// that contains the available formatters.
	/// </summary>
	public string Name { get; }
	
	public string Format(ITerminal terminal);
	
	public static ITerminalOutputFormatter ConstructDefaultFormatter(string name)
	{
		switch (name)
		{
			case nameof(TerminalOutputFormatterExpand):
				return new TerminalOutputFormatterExpand();
			case nameof(TerminalOutputFormatterAll):
				return new TerminalOutputFormatterAll();
			default:
				throw new LuthetusIdeException(
					$"A {nameof(ITerminalOutputFormatter)} with name: '{name}' was not found.");
		}
	}
}
