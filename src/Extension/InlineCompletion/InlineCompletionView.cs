using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
		private int _triggeringCaret;

		private string? _currentCode;
		private int _currentSnippet = 0;
		private int _totalSnippets = 0;
		private double _fontSize;
		private SolidColorBrush _textBrush;

		/// <summary>
		/// Initializes a new instance of the <see cref="InlineCompletionView"/> class.
		/// </summary>
		/// <param name="view">Text view to create the adornment for</param>
		public InlineCompletionView(IWpfTextView view, FontSettings settings, string? initCode, int caretPos)
		{
			if (view == null)
			{
				throw new ArgumentNullException("view");
			}

			_layer = view.GetAdornmentLayer("TextAdornment1");
			_view = view;
			_settings = settings;
			_currentCode = initCode;
			_triggeringCaret = caretPos;
			_fontSize = GetFontSize(settings.FontFamily, settings.FontSize);
			_textBrush = new SolidColorBrush(_settings.CommentColor);

			var lc = new LengthConverter();
			var fontSize = (double)lc.ConvertFrom($"{_settings.FontSize}pt");
			var textSize = GetFontSize(_settings.FontFamily, _settings.FontSize);
		}

		internal void StartDrawingCompletionView()
		{
			_view.LayoutChanged += OnLayoutChanged;
		}

		/// <summary>
		/// Draws the instructions for the completion session
		/// </summary>
		private void DrawCompletionInstructions()
		{
			var triggeringLine = GetTriggeringLine();
			var geometry = _view.TextViewLines.GetMarkerGeometry(triggeringLine.Extent);
			var textBlock = new TextBlock
			{
				Width = 600, //TODO calculate
				Foreground = _textBrush,
				Height = geometry.Bounds.Height,
				FontFamily = new FontFamily(_settings.FontFamily),
				FontSize = _fontSize,
				Text = $"[{_currentSnippet}/{_totalSnippets}] [←]Previous [→]Next [Tab]Commit [ESC]Cancel"
			};

			Canvas.SetLeft(textBlock, geometry.Bounds.Width + geometry.Bounds.Height);
			Canvas.SetTop(textBlock, geometry.Bounds.Top);
			
			_layer.AddAdornment(AdornmentPositioningBehavior.TextRelative, triggeringLine.Extent, Instructions_Tag, textBlock, (tag, ui) => { });
		}

		/// <summary>
		/// Draws the box that contains the code
		/// </summary>
		private void DrawSnippetPreview()
		{
			var triggeringLine = GetTriggeringLine();
			var geometry = _view.TextViewLines.GetMarkerGeometry(triggeringLine.Extent);
			var caretPos = _view.Caret.Position.BufferPosition;

			var wholeLineSpan = new SnapshotSpan(triggeringLine.Snapshot, new Span(triggeringLine.Start, triggeringLine.Length));
			var lineText = wholeLineSpan.GetText();
			var firstChar = wholeLineSpan.GetText().Trim().First();
			var position = lineText.IndexOf(firstChar);
			var onlyTextSpan = new SnapshotSpan(triggeringLine.Snapshot, new Span(triggeringLine.Start + position, triggeringLine.Length));
			var onlyTextG = _view.TextViewLines.GetMarkerGeometry(onlyTextSpan);

			var content = _currentCode;
			if(_currentCode == null)
			{
				content = "fetching snipppets...";
			}
			var loc = content.Split('\n').Length;

			//TODO ensure space
			//var insertedLines = EnsureSpaceFor(loc, triggeringLine);

			double height = loc * geometry.Bounds.Height;
			var brush = new SolidColorBrush(_settings.CommentColor);

			var textBlock = new TextBlock
			{
				Width = 1000,
				Foreground = _textBrush,
				FontFamily = new FontFamily(_settings.FontFamily),
				FontSize = _fontSize,
				Height = height,
				Text = content
			};

			Canvas.SetLeft(textBlock, onlyTextG.Bounds.Left);
			Canvas.SetTop(textBlock, geometry.Bounds.Bottom);

			_layer.AddAdornment(AdornmentPositioningBehavior.TextRelative, triggeringLine.Extent, Preview_Tag, textBlock, (tag, ui) => { });
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

		/// <summary>
		/// Ensures that there is enough space below the starting line for the given lines of code.
		/// </summary>
		/// <param name="linesOfCode"></param>
		/// <param name="startLine"></param>
		/// <returns></returns>
		private ITextSnapshot EnsureSpaceFor(int linesOfCode, ITextViewLine startLine)
		{
			var newLines = new string('\n', linesOfCode);

			var caretPos = _view.Caret.Position.BufferPosition;
			var edit = _view.TextBuffer.CreateEdit();
			edit.Insert(caretPos.Position, newLines);
			var newSpan = edit.Apply();
			return null; ;
		}

		private ITextViewLine GetTriggeringLine()
		{
			return _view.TextViewLines.Single(l => _triggeringCaret >= l.Start && _triggeringCaret <= l.End);
		}
	}
}
