namespace Luthetus.Ide.RazorLib.Outputs.Models;

// TODO: Delete this?
public record ExceptionOutput(
	string SourceFilePath,
	int RowNumber,
	int ColumnNumber,
	string ErrorId,
	string ErrorMessage,
	string ProjectFilePath);
