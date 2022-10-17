﻿using Microsoft.VisualStudio.Package;
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
	/// This class is responsible for drawing the adornments for the inlince completion instructions
	/// that provides the users with the keyboard shortcuts they need to know.
	/// </summary>
	internal sealed class InlineCompletionInstructionsView
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

		private int _currentSnippetIndex = 0;
		private int _totalSnippetCount = 0;
		private double _fontSize;
		private SolidColorBrush _textBrush;
		private SolidColorBrush _textBackgroundBrush;

		/// <summary>
		/// Initializes a new instance of the <see cref="InlineCompletionInstructionsView"/> class.
		/// </summary>
		/// <param name="view">Text view to create the adornment for</param>
		public InlineCompletionInstructionsView(IWpfTextView view, int caretPos)
		{
			if (view == null)
			{
				throw new ArgumentNullException("view");
			}

			_layer = view.GetAdornmentLayer("InlineCompletionInstructions");

			_settings = EditorSettingsProvider.GetCurrentFontSettings();
			_view = view;
			_triggeringCaret = caretPos;
			_fontSize = GetFontSize(_settings.FontFamily, _settings.FontSize);
			_textBrush = new SolidColorBrush(_settings.CommentColor);
			_textBackgroundBrush = new SolidColorBrush(_settings.TextBackgroundColor);
		}

		internal void StartDrawingInstructions()
		{
			_view.LayoutChanged += OnLayoutChanged;
		}

		/// <summary>
		/// Draws the instructions for the completion session by adding a TextBlock to the adornment layer.
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
				UpdateInstructions(_currentSnippetIndex, _totalSnippetCount);
			}
		}

		/// <summary>
		/// Updates the binding fields and initializes a refresh on the adornments
		/// </summary>
		/// <param name="code"></param>
		/// <param name="current"></param>
		/// <param name="total"></param>
		internal void UpdateInstructions(int current, int total)
		{
			_currentSnippetIndex = current;
			_totalSnippetCount = total;
			_layer.RemoveAllAdornments();

			DrawCompletionInstructions();
		}

		/// <summary>
		/// Removes all adornments from the layer
		/// </summary>
		internal void RemoveInstructions()
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

		private ITextViewLine GetTriggeringLine()
		{
			return _view.TextViewLines.Single(l => _triggeringCaret >= l.Start && _triggeringCaret <= l.End);
		}
	}
}
