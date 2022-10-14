using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using System;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Brushes = System.Windows.Media.Brushes;
using FontFamily = System.Windows.Media.FontFamily;
using FontStyle = System.Drawing.FontStyle;

namespace Extension.InlineCompletion
{
	/// <summary>
	/// TextAdornment1 places red boxes behind all the "a"s in the editor window
	/// </summary>
	internal sealed class InlineCompletionView
	{
		private const string Preview_Tag = "preview";
		private const string Instructions_Tag = "instructions";

		/// <summary>
		/// The layer of the adornment.
		/// </summary>
		private readonly IAdornmentLayer _layer;

		/// <summary>
		/// Text view where the adornment is created.
		/// </summary>
		private readonly IWpfTextView _view;
		private readonly FontSettings _settings;
		private ITextViewLine _line;

		private string? _currentCode;
		private int _currentSnippet = 0;
		private int _totalSnippets = 0;

		/// <summary>
		/// Initializes a new instance of the <see cref="InlineCompletionView"/> class.
		/// </summary>
		/// <param name="view">Text view to create the adornment for</param>
		public InlineCompletionView(IWpfTextView view, FontSettings settings)
		{
			if (view == null)
			{
				throw new ArgumentNullException("view");
			}

			_layer = view.GetAdornmentLayer("TextAdornment1");

			_view = view;
			_settings = settings;
		}

		internal void CreateCompletionView(ITextViewLine line, string? initCode)
		{
			_line = line;
			_view.LayoutChanged += OnLayoutChanged;
			_currentCode = initCode;
		}

		/// <summary>
		/// Draws the instructions for the completion session
		/// </summary>
		private void DrawCompletionInstructions()
		{
			var geometry = _view.TextViewLines.GetMarkerGeometry(_line.Extent);
			var textSize = GetFontSize(_settings.FontFamily, _settings.FontSize);
			
			var textBlock = new TextBlock
			{
				Width = 600,
				Foreground = Brushes.White,
				Height = geometry.Bounds.Height,
				FontFamily = new FontFamily(_settings.FontFamily),
				FontSize = textSize,
				Text = $"[{_currentSnippet}/{_totalSnippets}] [←]Previous [→]Next [Tab]Commit [ESC]Cancel"
			};

			Canvas.SetLeft(textBlock, 200);
			Canvas.SetTop(textBlock, geometry.Bounds.Top);
			
			_layer.AddAdornment(AdornmentPositioningBehavior.TextRelative, _line.Extent, Instructions_Tag, textBlock, (tag, ui) => { });
		}

		/// <summary>
		/// Draws the box that contains the code
		/// </summary>
		private void DrawSnippetPreview()
		{
			var geometry = _view.TextViewLines.GetMarkerGeometry(_line.Extent);
			
			var wholeLineSpan = new SnapshotSpan(_line.Snapshot, new Microsoft.VisualStudio.Text.Span(_line.Start, _line.Length));
			var lineText = wholeLineSpan.GetText();
			var firstChar = wholeLineSpan.GetText().Trim().First();
			var position = lineText.IndexOf(firstChar);
			var onlyTextSpan = new SnapshotSpan(_line.Snapshot, new Microsoft.VisualStudio.Text.Span(_line.Start + position, _line.Length));
			var onlyTextG = _view.TextViewLines.GetMarkerGeometry(onlyTextSpan);

			var fontSize = GetFontSize(_settings.FontFamily, _settings.FontSize);
			var content = _currentCode;
			if(_currentCode == null)
			{
				content = "fetching snipppets...";
			}
			var loc = content.Split('\n').Length;

			double height = loc * geometry.Bounds.Height;
			var textBlock = new TextBlock
			{
				Width = 1000,
				Foreground = Brushes.White,
				FontFamily = new FontFamily(_settings.FontFamily),
				FontSize = fontSize,
				FontWeight = FontWeights.Light,
				FontStyle = FontStyles.Normal,
				Height = height,
				Text = content
			};
			Canvas.SetLeft(textBlock, onlyTextG.Bounds.Left);
			Canvas.SetTop(textBlock, geometry.Bounds.Bottom);

			_layer.AddAdornment(AdornmentPositioningBehavior.TextRelative, _line.Extent, Preview_Tag, textBlock, (tag, ui) => { });
		}

		/// <summary>
		/// Refresh adornments whenever the layout changes
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
		{
			if (_layer.IsEmpty)
			{
				UpdateSnippetPreview(_currentCode, _currentSnippet, _totalSnippets);
			}
		}

		/// <summary>
		/// Updates the binding fields and initializes a refresh on the adornments
		/// </summary>
		/// <param name="code"></param>
		/// <param name="current"></param>
		/// <param name="total"></param>
		internal void UpdateSnippetPreview(string? code, int current, int total)
		{
			_currentCode = code;
			_currentSnippet = current;
			_totalSnippets = total;
			_layer.RemoveAllAdornments();

			DrawSnippetPreview();
			DrawCompletionInstructions();
		}

		/// <summary>
		/// Removes all adornments from the layer
		/// </summary>
		internal void RemoveVisuals()
		{
			_view.LayoutChanged -= OnLayoutChanged;
			_layer.RemoveAllAdornments();
		}

		/// <summary>
		/// Helper class to calculate the font size within the TextBlock
		/// </summary>
		/// <param name="familyName"></param>
		/// <param name="size"></param>
		/// <returns></returns>
		private double GetFontSize(string familyName, short size)
		{
			var fam = new FontFamily(familyName);
			var t = fam.FamilyNames;
			var f = new Font(familyName, size);
			
			var family = new System.Drawing.FontFamily(familyName);
			var d = family.GetCellDescent(FontStyle.Regular);
			var a = family.GetCellAscent(FontStyle.Regular);
			var emHeight = family.GetEmHeight(FontStyle.Regular);
			var descend = (f.Size * d) / emHeight;
			var ascend = (f.Size * a) / emHeight;
			var textBlockSize = f.Height - descend;

			return textBlockSize;
		}

		private ITextSnapshot AssureSpaceFor(int linesOfCode)
		{
			var caretPos = _view.Caret.Position.BufferPosition;
			var edit = _view.TextBuffer.CreateEdit();
			var newLines = new string('\n', linesOfCode);
			edit.Insert(caretPos.Position, newLines);

			return edit.Apply();
		}
	}
}
