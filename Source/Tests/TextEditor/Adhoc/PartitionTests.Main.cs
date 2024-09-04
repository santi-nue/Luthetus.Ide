using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Fluxor;
using Luthetus.Common.RazorLib.BackgroundTasks.Models;
using Luthetus.Common.RazorLib.FileSystems.Models;
using Luthetus.Common.RazorLib.Exceptions;
using Luthetus.Common.RazorLib.Clipboards.Models;
using Luthetus.Common.RazorLib.Installations.Models;
using Luthetus.Common.RazorLib.Storages.Models;
using Luthetus.Common.RazorLib.Misc;
using Luthetus.Common.RazorLib.Keys.Models;
using Luthetus.TextEditor.RazorLib;
using Luthetus.TextEditor.RazorLib.Installations.Models;
using Luthetus.TextEditor.RazorLib.TextEditors.Models;
using Luthetus.TextEditor.RazorLib.Lexers.Models;
using Luthetus.TextEditor.RazorLib.Cursors.Models;
using Luthetus.TextEditor.RazorLib.BackgroundTasks.Models;

namespace Luthetus.TextEditor.Tests.Adhoc;

/// <summary>
/// When a deletion of text spans multiple partitions it seems to sometimes break the line endings.
/// </summary>
public partial class PartitionTests
{
	/// <summary>
	/// Its thought that line ending only text is best to be using for these tests.
	/// Since in specific, here we are testing why the line ending tracking is breaking.
	/// </summary>
	[Fact]
	public async Task TwoPartitionEdit_Delete()
	{
		var test = TestInitialize();
		
		var sourceText = "\r\n\r\n\r\n";
		
		var modelList = test.TextEditorService.TextEditorStateWrap.Value.ModelList;

		var model = new TextEditorModel(
			new ResourceUri($"/unitTesting.cs"),
			DateTime.UtcNow,
			ExtensionNoPeriodFacts.C_SHARP_CLASS,
            sourceText,
			decorationMapper: null,
			compilerService: null,
			partitionSize: 4);

		test.TextEditorService.ModelApi.RegisterCustom(model);

		Exception? exception = null;

		var uniqueTextEditorWork = new UniqueTextEditorWork(
			nameof(PartitionTests),
			editContext =>
			{
				try
				{
					var modelModifier = editContext.GetModelModifier(model.ResourceUri);

					if (modelModifier is null)
					{
						Console.WriteLine("modelModifier is null");
						return Task.CompletedTask;
					}
					
					// The cursor uses line and column indices
					//
					// But the selection uses position indices.
					//
					// These position indices are dangerous, because one might accidentally
					// use an index that is among a multi-byte character.
					//
					// When using the MoveCursor and etc, 
					
					LogAndNonScientificallyAssertPartitionList(modelModifier, "[\\r\\n][\\r\\n\\r\\n]");
					
					// "\r\n\r\n\r\n";
					// "[\r\n][\r\n\r\n]"
					//   ^^^^^^^^^^
					//
					// Delete the letter above the '^'.
					//
					// So, delete the only line ending in partition 1
					// and the first line ending in partition 2.
					//
					// Do this by selecting the text then insert an enter key.
					
					var cursor = new TextEditorCursor(isPrimaryCursor: true);
					var cursorModifier = new TextEditorCursorModifier(cursor);
					
					cursorModifier.SelectionAnchorPositionIndex = 0;
					cursorModifier.SelectionEndingPositionIndex = 4;
					
					cursorModifier.LineIndex = 2;
					cursorModifier.ColumnIndex = 0;
					
					var cursorModifierBag = new CursorModifierBagTextEditor(
						Key<TextEditorViewModel>.Empty,
						new List<TextEditorCursorModifier> { cursorModifier });
						
					modelModifier.Delete(
						cursorModifierBag,
						columnCount: 0, // Delete the selection, odd to give 0?
						expandWord: false,
						TextEditorModelModifier.DeleteKind.Delete);
					
					LogAndNonScientificallyAssertPartitionList(modelModifier, "[][\\r\\n]");
					
                    return Task.CompletedTask;
				}
				catch (Exception e)
				{
					exception = e;
					return Task.CompletedTask;
				}
			});

		await test.TextEditorService.PostAsync(uniqueTextEditorWork).ConfigureAwait(false);

		if (exception is not null)
			throw exception;
	}
	
	/// <summary>
	/// Its thought that line ending only text is best to be using for these tests.
	/// Since in specific, here we are testing why the line ending tracking is breaking.
	/// </summary>
	[Fact]
	public async Task TwoPartitionEdit_Backspace()
	{
		var test = TestInitialize();
		
		var sourceText = "\r\n\r\n\r\n";
		
		var modelList = test.TextEditorService.TextEditorStateWrap.Value.ModelList;

		var model = new TextEditorModel(
			new ResourceUri($"/unitTesting.cs"),
			DateTime.UtcNow,
			ExtensionNoPeriodFacts.C_SHARP_CLASS,
            sourceText,
			decorationMapper: null,
			compilerService: null,
			partitionSize: 4);

		test.TextEditorService.ModelApi.RegisterCustom(model);

		Exception? exception = null;

		var uniqueTextEditorWork = new UniqueTextEditorWork(
			nameof(PartitionTests),
			editContext =>
			{
				try
				{
					var modelModifier = editContext.GetModelModifier(model.ResourceUri);

					if (modelModifier is null)
					{
						Console.WriteLine("modelModifier is null");
						return Task.CompletedTask;
					}
					
					// The cursor uses line and column indices
					//
					// But the selection uses position indices.
					//
					// These position indices are dangerous, because one might accidentally
					// use an index that is among a multi-byte character.
					//
					// When using the MoveCursor and etc, 
					
					LogAndNonScientificallyAssertPartitionList(modelModifier, "[\\r\\n][\\r\\n\\r\\n]");
					
					// "\r\n\r\n\r\n";
					// "[\r\n][\r\n\r\n]"
					//   ^^^^^^^^^^
					//
					// Delete the letter above the '^'.
					//
					// So, delete the only line ending in partition 1
					// and the first line ending in partition 2.
					//
					// Do this by selecting the text then insert an enter key.
					
					var cursor = new TextEditorCursor(isPrimaryCursor: true);
					var cursorModifier = new TextEditorCursorModifier(cursor);
					
					cursorModifier.SelectionAnchorPositionIndex = 0;
					cursorModifier.SelectionEndingPositionIndex = 4;
					
					cursorModifier.LineIndex = 2;
					cursorModifier.ColumnIndex = 0;
					
					var cursorModifierBag = new CursorModifierBagTextEditor(
						Key<TextEditorViewModel>.Empty,
						new List<TextEditorCursorModifier> { cursorModifier });
						
					modelModifier.Delete(
						cursorModifierBag,
						columnCount: 0, // Delete the selection, odd to give 0?
						expandWord: false,
						TextEditorModelModifier.DeleteKind.Backspace);
					
					LogAndNonScientificallyAssertPartitionList(modelModifier, "[][\\r\\n]");
					
                    return Task.CompletedTask;
				}
				catch (Exception e)
				{
					exception = e;
					return Task.CompletedTask;
				}
			});

		await test.TextEditorService.PostAsync(uniqueTextEditorWork).ConfigureAwait(false);

		if (exception is not null)
			throw exception;
	}
	
	[Fact]
	public async Task TwoPartitionEdit_Insert()
	{
		var test = TestInitialize();
		
		var sourceText = "\r\n\r\n\r\n";
		
		var modelList = test.TextEditorService.TextEditorStateWrap.Value.ModelList;

		var model = new TextEditorModel(
			new ResourceUri($"/unitTesting.cs"),
			DateTime.UtcNow,
			ExtensionNoPeriodFacts.C_SHARP_CLASS,
            sourceText,
			decorationMapper: null,
			compilerService: null,
			partitionSize: 4);

		test.TextEditorService.ModelApi.RegisterCustom(model);

		Exception? exception = null;

		var uniqueTextEditorWork = new UniqueTextEditorWork(
			nameof(PartitionTests),
			editContext =>
			{
				try
				{
					var modelModifier = editContext.GetModelModifier(model.ResourceUri);

					if (modelModifier is null)
					{
						Console.WriteLine("modelModifier is null");
						return Task.CompletedTask;
					}
					
					// The cursor uses line and column indices
					//
					// But the selection uses position indices.
					//
					// These position indices are dangerous, because one might accidentally
					// use an index that is among a multi-byte character.
					//
					// When using the MoveCursor and etc, 
					
					LogAndNonScientificallyAssertPartitionList(modelModifier, "[\\r\\n][\\r\\n\\r\\n]");
					
					// "\r\n\r\n\r\n";
					// "[\r\n][\r\n\r\n]"
					//   ^^^^^^^^^^
					//
					// Delete the letter above the '^'.
					//
					// So, delete the only line ending in partition 1
					// and the first line ending in partition 2.
					//
					// Do this by selecting the text then insert an enter key.
					
					var cursor = new TextEditorCursor(isPrimaryCursor: true);
					var cursorModifier = new TextEditorCursorModifier(cursor);
					
					cursorModifier.SelectionAnchorPositionIndex = 0;
					cursorModifier.SelectionEndingPositionIndex = 4;
					
					cursorModifier.LineIndex = 2;
					cursorModifier.ColumnIndex = 0;
					
					var cursorModifierBag = new CursorModifierBagTextEditor(
						Key<TextEditorViewModel>.Empty,
						new List<TextEditorCursorModifier> { cursorModifier });
					
					modelModifier.Insert(
				        "\r\n",
				        cursorModifierBag,
				        useLineEndKindPreference: false);
					
					LogAndNonScientificallyAssertPartitionList(modelModifier, "[\\r\\n][\\r\\n]");
					
                    return Task.CompletedTask;
				}
				catch (Exception e)
				{
					exception = e;
					return Task.CompletedTask;
				}
			});

		await test.TextEditorService.PostAsync(uniqueTextEditorWork).ConfigureAwait(false);

		if (exception is not null)
			throw exception;
	}
	
	[Fact]
	public async Task ThreePartitionEdit_Delete()
	{
		var test = TestInitialize();
		
		var sourceText = "\r\n\r\n\r\n\r\n";
		
		var modelList = test.TextEditorService.TextEditorStateWrap.Value.ModelList;

		var model = new TextEditorModel(
			new ResourceUri($"/unitTesting.cs"),
			DateTime.UtcNow,
			ExtensionNoPeriodFacts.C_SHARP_CLASS,
            sourceText,
			decorationMapper: null,
			compilerService: null,
			partitionSize: 4);

		test.TextEditorService.ModelApi.RegisterCustom(model);

		Exception? exception = null;

		var uniqueTextEditorWork = new UniqueTextEditorWork(
			nameof(PartitionTests),
			editContext =>
			{
				try
				{
					var modelModifier = editContext.GetModelModifier(model.ResourceUri);

					if (modelModifier is null)
					{
						Console.WriteLine("modelModifier is null");
						return Task.CompletedTask;
					}
					
					// The cursor uses line and column indices
					//
					// But the selection uses position indices.
					//
					// These position indices are dangerous, because one might accidentally
					// use an index that is among a multi-byte character.
					//
					// When using the MoveCursor and etc, 
					
					LogAndNonScientificallyAssertPartitionList(modelModifier, "[\\r\\n][\\r\\n][\\r\\n\\r\\n]");
					
					// "\r\n\r\n\r\n\r\n";
					// "[\r\n][\r\n][\r\n\r\n]"
					//   ^^^^^^^^^^^^^^^^
					//
					// Delete the letter above the '^'.
					//
					// So, delete the only line ending in partition 1
					// and the first line ending in partition 2.
					//
					// Do this by selecting the text then insert an enter key.
					
					var cursor = new TextEditorCursor(isPrimaryCursor: true);
					var cursorModifier = new TextEditorCursorModifier(cursor);
					
					cursorModifier.SelectionAnchorPositionIndex = 0;
					cursorModifier.SelectionEndingPositionIndex = 6;
					
					cursorModifier.LineIndex = 3;
					cursorModifier.ColumnIndex = 0;
					
					var cursorModifierBag = new CursorModifierBagTextEditor(
						Key<TextEditorViewModel>.Empty,
						new List<TextEditorCursorModifier> { cursorModifier });
					
					modelModifier.Delete(
						cursorModifierBag,
						columnCount: 0, // Delete the selection, odd to give 0?
						expandWord: false,
						TextEditorModelModifier.DeleteKind.Backspace);
					
					LogAndNonScientificallyAssertPartitionList(modelModifier, "[][][\\r\\n]");
					
                    return Task.CompletedTask;
				}
				catch (Exception e)
				{
					exception = e;
					return Task.CompletedTask;
				}
			});

		await test.TextEditorService.PostAsync(uniqueTextEditorWork).ConfigureAwait(false);

		if (exception is not null)
			throw exception;
	}
	
	/// <summary>
	/// I can wrap my head around my unit test much easier if I
	/// type the partitions out like:
	///
	/// Given:
	/// 	PartitionSize: = 4
	/// 	Input text: "\r\n\r\n\r\n"
	///
	/// Then:
	/// 	"[\r\n][\r\n\r\n]"
	/// , is how am visualizing the partition.
	///
	/// So if I can assert my visualization it makes creation of good tests easier,
	/// then if my visualization introduces inaccuracies I can add a more
	/// 1 to 1 assertion by iterating over the partitions and asserting the richCharacter.Value.
	/// </summary>
	public void LogAndNonScientificallyAssertPartitionList(
		ITextEditorModel model,
		string expectedVisualizationText)
	{
		/*
[abcd][efgh][ijkl][mnop][qrst][uvwx][yz]

[abcd]
	insert e
[ab][cdef]
	insert g
[abc][cd][efg]

-------------------

PartitionSize = 4
"\r\n\r\n\r\n"

[\r\n\r\n]
	insert \r
[\r\n][\r\n\r\n]

*/	
		var actualVisualizationTextBuilder = new StringBuilder();
		
		Console.WriteLine("\n\nLogPartition_START\n");
		
		foreach (var partition in model.PartitionList)
		{
			actualVisualizationTextBuilder.Append('[');
			foreach (var richCharacter in partition.RichCharacterList)
			{
				if (richCharacter.Value == '\r')
				{
					actualVisualizationTextBuilder.Append("\\r");
				}
				else if (richCharacter.Value == '\n')
				{
					actualVisualizationTextBuilder.Append("\\n");
				}
				else if (richCharacter.Value == '\t')
				{
					actualVisualizationTextBuilder.Append("\\t");
				}
				else
				{
					actualVisualizationTextBuilder.Append(richCharacter.Value);
				}
			}
			actualVisualizationTextBuilder.Append(']');
		}
		
		var actualVisualizationText = actualVisualizationTextBuilder.ToString();
		Console.Write(actualVisualizationText);
		
		Console.WriteLine("\n\nLogPartition_END\n\n");
		
		Assert.Equal(expectedVisualizationText, actualVisualizationText);
	}

	/// <summary>
	/// This test isn't the most useful thing for determining errors.
	/// But it is quite interesting to see the different partition sizes and when it fails.
	///
	/// Then drill into a more specific unit test once an issue is found here
	/// is the way I'd look at this unit test if its ever even used.
	/// </summary>
	[Fact]
	public async Task Large_Throughput_Test()
	{
		var test = TestInitialize();
        
        // "\r" fails on indexPartitionSize: 93;
        //      after removing deletedCount, this now does not fail.
        // "\n" fails on indexPartitionSize: 93;
        //      after removing deletedCount, this now does not fail.
        // "\r\n" fails on indexPartitionSize: Does not fail;
        var lineEndingList = new [] {  "\r\n", "\r", "\n",  };
        
        for (int lineEndingIndex = 0; lineEndingIndex < lineEndingList.Length; lineEndingIndex++)
        {
        	var lineEnding = lineEndingList[lineEndingIndex];
        	
        	var lineEndingText = "a";
				
			if (lineEnding == "\r")
				lineEndingText = "\\r";
			else if (lineEnding == "\n")
				lineEndingText = "\\n";
			else if (lineEnding == "\r\n")
				lineEndingText = "\\r\\n";
        	
        	var testText = @"public class Person
{
	public Person(string firstName, string lastName)
	{
		FirstName = firstName;
		LastName = lastName;
	}

	public string FirstName { get; set }
	public string LastName { get; set; }
}
".ReplaceLineEndings(lineEnding);
        
	        for (int indexPartitionSize = 4; indexPartitionSize < testText.Length + 1; indexPartitionSize++)
			{
				var modelList = test.TextEditorService.TextEditorStateWrap.Value.ModelList;
	
				var model = new TextEditorModel(
					new ResourceUri($"/unitTesting_{lineEndingText}_{indexPartitionSize}.cs"),
					DateTime.UtcNow,
					ExtensionNoPeriodFacts.C_SHARP_CLASS,
	                testText,
					decorationMapper: null,
					compilerService: null,
					partitionSize: indexPartitionSize);
	
				test.TextEditorService.ModelApi.RegisterCustom(model);
	
				Exception? exception = null;
	
				var uniqueTextEditorWork = new UniqueTextEditorWork(
					nameof(PartitionTests),
					editContext =>
					{
						try
						{
							var modelModifier = editContext.GetModelModifier(model.ResourceUri);
	
							if (modelModifier is null)
							{
								Console.WriteLine("modelModifier is null");
								return Task.CompletedTask;
							}
	
							var cursor = new TextEditorCursor(isPrimaryCursor: true);
							var cursorModifier = new TextEditorCursorModifier(cursor);
	
							cursorModifier.LineIndex = 2;
							cursorModifier.ColumnIndex = 0;
							var startLineIndexTwoPositionIndex = modelModifier.GetPositionIndex(cursorModifier);
	
							cursorModifier.LineIndex = 3;
							cursorModifier.ColumnIndex = 0;
							var startLineIndexThreePositionIndex = modelModifier.GetPositionIndex(cursorModifier);
	
							cursorModifier.SelectionAnchorPositionIndex = startLineIndexTwoPositionIndex;
							cursorModifier.SelectionEndingPositionIndex = startLineIndexThreePositionIndex;
	
							var cursorModifierBag = new CursorModifierBagTextEditor(
								Key<TextEditorViewModel>.Empty,
								new List<TextEditorCursorModifier> { cursorModifier });
	
							modelModifier.Delete(
								cursorModifierBag,
								columnCount: 0, // Delete the selection, odd to give 0?
								expandWord: false,
								TextEditorModelModifier.DeleteKind.Delete);
	
							var expectedText = @"public class Person
	{
		{
			FirstName = firstName;
			LastName = lastName;
		}
	
		public string FirstName { get; set }
		public string LastName { get; set; }
	}
	".ReplaceLineEndings(lineEnding);
	
							var actualText = modelModifier.GetAllText();
	
							Assert.Equal(expectedText, actualText);
	                        return Task.CompletedTask;
						}
						catch (Exception e)
						{
							exception = e;
							return Task.CompletedTask;
						}
					});
	
				await test.TextEditorService.PostAsync(uniqueTextEditorWork).ConfigureAwait(false);
				
				
	
				if (exception is not null)
					throw new Exception($"lineEndingText: {lineEndingText}; indexPartitionSize: {indexPartitionSize}; " + exception.Message);
	        }
	    }
	}
	
	public class TestContext
	{
		public TestContext(LuthetusHostingInformation hostingInformation, IServiceProvider serviceProvider)
		{
			HostingInformation = hostingInformation;
			ServiceProvider = serviceProvider;
			
			TextEditorService = ServiceProvider.GetRequiredService<ITextEditorService>();
			ContinuousBackgroundTaskWorker = ServiceProvider.GetRequiredService<ContinuousBackgroundTaskWorker>();
			
			HostingInformation.StartBackgroundTaskWorkers(serviceProvider);
		}
	
		public LuthetusHostingInformation HostingInformation { get; }
		public IServiceProvider ServiceProvider { get; }
		
		public ITextEditorService TextEditorService { get; }
		public ContinuousBackgroundTaskWorker ContinuousBackgroundTaskWorker { get; }
	}
	
	public TestContext TestInitialize()
	{
		var services = new ServiceCollection();
		
		var hostingInformation = new LuthetusHostingInformation(
			LuthetusHostingKind.UnitTestingAsync,
	        LuthetusPurposeKind.TextEditor,
	        new BackgroundTaskService());
		
		services
			.AddLuthetusTextEditor(hostingInformation)
			.AddScoped<ILoggerFactory, NullLoggerFactory>()
			.AddScoped<IJSRuntime, DoNothingJsRuntime>();
            
		// If one doesn't use a LuthetusPurposeKind of Photino,
		// then this code block is redundant, because the ServieCollectionExtensions.cs
		// for Luthetus.Common will register InMemory for Environment and FileSystem providers.
		//
		// But, because of the importance that under no circumstance one's actual FileSystem is used during
		// unit testing, this redundancy is here.
		{
			services.AddScoped<IEnvironmentProvider, InMemoryEnvironmentProvider>();
			services.AddScoped<IFileSystemProvider, InMemoryFileSystemProvider>();
		}
		
		// At the moment one has to manually set the following services to their InMemory implementation.
		{
			services.AddScoped<IClipboardService, InMemoryClipboardService>();
			services.AddScoped<IStorageService, InMemoryStorageService>();
		}
		
		services.AddFluxor(options => options.ScanAssemblies(
			typeof(LuthetusCommonConfig).Assembly,
			typeof(LuthetusTextEditorConfig).Assembly));
		
		var testContext = new TestContext(hostingInformation, services.BuildServiceProvider());
		
		// Check various services that should be InMemory implementations.
		{	
			{
				var fileSystemProvider = testContext.ServiceProvider.GetRequiredService<IFileSystemProvider>();
				if (fileSystemProvider is not InMemoryFileSystemProvider)
					throw new LuthetusUnitTestException(
						$"A UnitTest's {nameof(IFileSystemProvider)} must be " +
						$"{nameof(InMemoryFileSystemProvider)}, " +
						$"but was {fileSystemProvider.GetType().Name}");
			}
			
			{
				var environmentProvider = testContext.ServiceProvider.GetRequiredService<IEnvironmentProvider>();
				if (environmentProvider is not InMemoryEnvironmentProvider)
					throw new LuthetusUnitTestException(
						$"A UnitTest's {nameof(IEnvironmentProvider)} must be " +
						$"{nameof(InMemoryEnvironmentProvider)}, " +
						$"but was {environmentProvider.GetType().Name}");
			}
	
			{
				var clipboardService = testContext.ServiceProvider.GetRequiredService<IClipboardService>();
				if (clipboardService is not InMemoryClipboardService)
					throw new LuthetusUnitTestException(
						$"A UnitTest's {nameof(IClipboardService)} must be " +
						$"{nameof(InMemoryClipboardService)}, " +
						$"but was {clipboardService.GetType().Name}");
			}
	
			{
				var storageService = testContext.ServiceProvider.GetRequiredService<IStorageService>();
				if (storageService is not InMemoryStorageService)
					throw new LuthetusUnitTestException(
						$"A UnitTest's {nameof(IStorageService)} must be " +
						$"{nameof(InMemoryStorageService)}, " +
						$"but was {storageService.GetType().Name}");
			}
		}
		
		var store = testContext.ServiceProvider.GetRequiredService<IStore>();
        store.InitializeAsync().Wait();
	
		return testContext;
	}
}
