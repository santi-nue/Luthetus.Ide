using Luthetus.Common.RazorLib.Dynamics.Models;
using Luthetus.Common.RazorLib.Keys.Models;

namespace Luthetus.TextEditor.RazorLib.TextEditors.Models;

public interface ITabTextEditor : ITab
{
	public Key<TextEditorViewModel> ViewModelKey { get; }
}
