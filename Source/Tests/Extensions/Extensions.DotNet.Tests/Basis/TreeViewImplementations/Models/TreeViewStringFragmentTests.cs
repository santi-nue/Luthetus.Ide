using Microsoft.Extensions.DependencyInjection;
using Luthetus.Common.RazorLib.ComponentRenderers.Models;
using Luthetus.Common.RazorLib.FileSystems.Models;
using Luthetus.Ide.RazorLib.ComponentRenderers.Models;
using Luthetus.Extensions.DotNet.TestExplorers.Models;

namespace Luthetus.Extensions.DotNet.Tests.Basis.TreeViewImplementations.Models;

/// <summary>
/// <see cref="TreeViewStringFragment"/>
/// </summary>
public class TreeViewStringFragmentTests : ExtensionsDotNetTestBase
{
    /// <summary>
    /// <see cref="TreeViewStringFragment(StringFragment, ILuthetusCommonComponentRenderers, bool, bool)"/>
    /// <br/>----<br/>
    /// <see cref="TreeViewStringFragment.CommonComponentRenderers"/>
    /// </summary>
    [Fact]
    public void Constructor()
    {
        Test_RegisterServices(out var serviceProvider);
        Test_CreateFileSystem(serviceProvider);

        var ideComponentRenderers = serviceProvider.GetRequiredService<IIdeComponentRenderers>();
        var commonComponentRenderers = serviceProvider.GetRequiredService<ICommonComponentRenderers>();
        var fileSystemProvider = serviceProvider.GetRequiredService<IFileSystemProvider>();
        var environmentProvider = serviceProvider.GetRequiredService<IEnvironmentProvider>();

        var stringFragment = new StringFragment("abc.123");

        var isExpandable = true;
        var isExpanded = true;

        var treeView = new TreeViewStringFragment(
            stringFragment,
            commonComponentRenderers,
            isExpandable,
            isExpanded);

        Assert.Equal(treeView.Item, stringFragment);
        Assert.Equal(treeView.CommonComponentRenderers, commonComponentRenderers);
        Assert.Equal(treeView.IsExpandable, isExpandable);
        Assert.Equal(treeView.IsExpanded, isExpanded);
    }

    /// <summary>
    /// <see cref="TreeViewStringFragment.Equals(object?)"/>
    /// </summary>
    [Fact]
    public void Equals_Test()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// <see cref="TreeViewStringFragment.GetHashCode()"/>
    /// </summary>
    [Fact]
    public void GetHashCode_Test()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// <see cref="TreeViewStringFragment.GetTreeViewRenderer()"/>
    /// </summary>
    [Fact]
    public void GetTreeViewRenderer()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// <see cref="TreeViewStringFragment.LoadChildListAsync()"/>
    /// </summary>
    [Fact]
    public void LoadChildListAsync()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// <see cref="TreeViewStringFragment.RemoveRelatedFilesFromParent(List{TreeViewNoType})"/>
    /// </summary>
    [Fact]
    public void RemoveRelatedFilesFromParent()
    {
        throw new NotImplementedException();
    }
}