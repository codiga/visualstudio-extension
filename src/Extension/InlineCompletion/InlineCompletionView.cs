using Extension.Logging;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FontFamily = System.Windows.Media.FontFamily;

namespace Extension.InlineCompletion
{
	/// <summary>
	/// This class is responsible for drawing the adornments for the inlince completion instructions
	/// that provides the users with the keyboard shortcuts they need to know.
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
		private ITrackingSpan _triggeringLine;

		private string? _currentSnippetCode;
		private int _currentSnippetIndex = 0;
		private int _totalSnippetCount = 0;
		private SolidColorBrush _textBrush = new SolidColorBrush(Colors.DarkGreen);
		private SolidColorBrush _textBackgroundBrush = new SolidColorBrush(Colors.DarkGray);
		private FontFamily _fontFamily = Fonts.SystemFontFamilies.First();

		public const string PreviewLayerName = "InlineCompletionLayer";

		public bool ShowPreview { get; set; } = true;
		public bool ShowInstructions { get; set; } = true;

		/// <summary>
		/// Initializes a new instance of the <see cref="InlineCompletionView"/> class.
		/// </summary>
		/// <param name="view">Text view to create the adornment for</param>
		public InlineCompletionView(IWpfTextView view, ITrackingSpan triggeringLine)
		{
			if (view == null)
			{
				throw new ArgumentNullException("view");
			}

			_layer = view.GetAdornmentLayer(PreviewLayerName);

			try
			{
				_settings = EditorSettingsProvider.GetCurrentFontSettings();
			}
			catch(ArgumentException e)
			{
				ExtensionLogger.LogException(e);
			}

			_view = view;
			_triggeringLine = triggeringLine;

			if (_settings != null)
			{
				_textBrush = new SolidColorBrush(_settings.CommentColor);
				_textBackgroundBrush = new SolidColorBrush(_settings.TextBackgroundColor);
				_fontFamily = new FontFamily(_settings.FontFamily);
			}

			_textBrush.Opacity = 0.7;
		}

		internal void StartDrawingInstructions()
		{
			_currentSnippetCode = null;
			_view.LayoutChanged += OnLayoutChanged;
		}

		/// <summary>
		/// Draws the instructions for the completion session by adding a TextBlock to the adornment layer.
		/// </summary>
		private void DrawCompletionInstructions()
		{
			var triggeringLine = GetTriggeringLine();

			if (triggeringLine == null)
				return;

			var geometry = _view.TextViewLines.GetMarkerGeometry(triggeringLine.Extent);

			if (geometry == null)
				return;

			var textBlock = new TextBlock
			{
				Width = 600, 
				Foreground = _textBrush,
				Height = geometry.Bounds.Height,
				FontFamily = _fontFamily,
				FontSize = triggeringLine.Baseline,
				Text = $"[{_currentSnippetIndex}/{_totalSnippetCount}] [←]Previous [→]Next [Tab]Commit [ESC]Cancel"
			};

			Canvas.SetLeft(textBlock, geometry.Bounds.Width + geometry.Bounds.Height);
			Canvas.SetTop(textBlock, geometry.Bounds.Top);
			
			_layer.AddAdornment(AdornmentPositioningBehavior.TextRelative, triggeringLine.Extent, Instructions_Tag, textBlock, (tag, ui) => { });
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
				UpdateView(_currentSnippetCode, _currentSnippetIndex, _totalSnippetCount);
			}
		}

		/// <summary>
		/// Updates the binding fields and initializes a refresh on the adornments
		/// </summary>
		/// <param name="code"></param>
		/// <param name="current"></param>
		/// <param name="total"></param>
		internal void UpdateView(string? code, int current, int total)
		{
			_currentSnippetCode = code;
			_currentSnippetIndex = current;
			_totalSnippetCount = total;
			
			if(!_layer.IsEmpty)
				_layer.RemoveAllAdornments();

			try
			{
				if (ShowInstructions)
					DrawCompletionInstructions();
				if (ShowPreview)
					DrawSnippetPreview();
			}

			catch (Exception e)
			{
				ExtensionLogger.LogException(e);
			}
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
			var firstCharacterSpan = new SnapshotSpan(triggeringLine.Snapshot, new Span(triggeringLine.Start + position, 1));
			var onlyTextG = _view.TextViewLines.GetMarkerGeometry(firstCharacterSpan);

			var content = _currentSnippetCode;
			if (_currentSnippetCode == null)
			{
				content = "fetching snippets...";
			}
			var loc = content.Split('\n').Length;

			double height = loc * geometry.Bounds.Height;
			
			var textBlock = new TextBlock
			{
				Width = _view.ViewportWidth,
				Foreground = _textBrush,
				FontStyle = FontStyles.Italic,
				Focusable = false,
				Background = _textBackgroundBrush,
				FontFamily = _fontFamily,
				FontSize = triggeringLine.Baseline,
				Height = height,
				Text = content
			};

			var border = new Border
			{
				BorderThickness = new Thickness(1, 0, 0, 1),
				BorderBrush = _textBrush,
				Child = textBlock,
			};

			Canvas.SetLeft(border, onlyTextG.Bounds.Left);
			Canvas.SetTop(border, geometry.Bounds.Bottom);

			_layer.AddAdornment(AdornmentPositioningBehavior.TextRelative, triggeringLine.Extent, Preview_Tag, border, (tag, ui) => { });
		}

		/// <summary>
		/// Removes all adornments from the layer
		/// </summary>
		internal void RemoveInstructions()
		{
			_currentSnippetCode = null;
			_currentSnippetIndex = 0;
			_totalSnippetCount = 0;
			_view.LayoutChanged -= OnLayoutChanged;
			_layer.RemoveAllAdornments();
		}

		/// <summary>
		/// Finds the line on which the inline completion was triggered.
		/// </summary>
        private ITextViewLine GetTriggeringLine()
		{
			var lineSpan = _triggeringLine.GetSpan(_view.TextSnapshot);
			var textViewLines = _view.TextViewLines.GetTextViewLinesIntersectingSpan(lineSpan);

			if (!textViewLines.Any())
			{
				if(_view.TextViewLines.Any())
					return _view.TextViewLines.First();

				throw new ArgumentOutOfRangeException(nameof(_triggeringLine), "Cannot map tracking span to a valid view line");
			}

			return textViewLines.First();
		}
	}
}
