using Community.VisualStudio.Toolkit;
using EnvDTE;
using Extension.SnippetFormats;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.Settings;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using static System.Net.Mime.MediaTypeNames;
using Brushes = System.Windows.Media.Brushes;
using FontFamily = System.Windows.Media.FontFamily;

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
		private readonly EditorSettings _settings;
		private ITextViewLine _line;



		private string? _currentCode;
		private int _currentSnippet = 0;
		private int _totalSnippets = 0;

		/// <summary>
		/// Initializes a new instance of the <see cref="InlineCompletionView"/> class.
		/// </summary>
		/// <param name="view">Text view to create the adornment for</param>
		public InlineCompletionView(IWpfTextView view, EditorSettings settings)
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
				Height = height,
				Text = content
			};
			Canvas.SetLeft(textBlock, onlyTextG.Bounds.Left);
			Canvas.SetTop(textBlock, geometry.Bounds.Bottom);

			_layer.AddAdornment(AdornmentPositioningBehavior.TextRelative, _line.Extent, Preview_Tag, textBlock, (tag, ui) => { });
		}

		private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
		{
			if (_layer.IsEmpty)
			{
				UpdateSnippetPreview(_currentCode, _currentSnippet, _totalSnippets);
			}
		}

		internal void UpdateSnippetPreview(string? code, int current, int total)
		{
			_currentCode = code;
			_currentSnippet = current;
			_totalSnippets = total;
			_layer.RemoveAllAdornments();

			DrawSnippetPreview();
			DrawCompletionInstructions();
		}

		internal void RemoveVisuals()
		{
			_view.LayoutChanged -= OnLayoutChanged;
			_layer.RemoveAllAdornments();
		}

		private double GetFontSize(string familyName, short size)
		{
			var f = new Font(familyName, size);
			var family = new System.Drawing.FontFamily(familyName);
			var d = family.GetCellDescent(FontStyle.Regular);
			var emHeight = family.GetEmHeight(FontStyle.Regular);
			var descend = (f.Size * d) / emHeight;
			var textBlockSize = f.Height - descend;

			return textBlockSize;
		}
	}
}
