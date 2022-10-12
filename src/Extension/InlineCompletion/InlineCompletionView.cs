﻿using Community.VisualStudio.Toolkit;
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
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using static System.Net.Mime.MediaTypeNames;

namespace Extension.InlineCompletion
{
	/// <summary>
	/// TextAdornment1 places red boxes behind all the "a"s in the editor window
	/// </summary>
	internal sealed class InlineCompletionView
	{
		/// <summary>
		/// The layer of the adornment.
		/// </summary>
		private readonly IAdornmentLayer _layer;

		/// <summary>
		/// Text view where the adornment is created.
		/// </summary>
		private readonly IWpfTextView _view;

		private ITextViewLine _line;

		/// <summary>
		/// Adornment brush.
		/// </summary>
		private readonly Brush _brush;

		/// <summary>
		/// Adornment pen.
		/// </summary>
		private readonly Pen _pen;

		private TextBlock _instructions;
		private TextBlock _snippetPreview;

		private string _initCode;

		/// <summary>
		/// Initializes a new instance of the <see cref="InlineCompletionView"/> class.
		/// </summary>
		/// <param name="view">Text view to create the adornment for</param>
		public InlineCompletionView(IWpfTextView view)
		{
			if (view == null)
			{
				throw new ArgumentNullException("view");
			}

			_layer = view.GetAdornmentLayer("TextAdornment1");

			_view = view;

			// Create the pen and brush to color the box behind the a's
			_brush = new SolidColorBrush(Color.FromArgb(0x20, 0x00, 0x00, 0xff));
			_brush.Freeze();

			var penBrush = new SolidColorBrush(Colors.Red);
			penBrush.Freeze();
			_pen = new Pen(penBrush, 0.5);
			_pen.Freeze();
		}

		/// <summary>
		/// Adds the scarlet box behind the 'a' characters within the given line
		/// </summary>
		/// <param name="line">Line to add the adornments</param>
		internal void CreateCompletionView(ITextViewLine line, string initCode)
		{
			_line = line;
			_view.LayoutChanged += OnLayoutChanged;
			_initCode = initCode;
		}

		private void DrawCompletionInstructions()
		{
			var geometry = _view.TextViewLines.GetMarkerGeometry(_line.Extent);

			var textBlock = new TextBlock
			{
				Width = 300,
				Background = Brushes.Gray,
				Height = geometry.Bounds.Height,
				Opacity = 0.5,
				Text = "[←]Previous [→]Next [Tab]Commit [ESC]Cancel"
			};
			_instructions = textBlock;
			Canvas.SetLeft(textBlock, 200);
			Canvas.SetTop(textBlock, geometry.Bounds.Top);
			
			_layer.AddAdornment(AdornmentPositioningBehavior.TextRelative, _line.Extent, null, textBlock, (tag, ui) => { });
		}

		private void DrawSnippetPreview(string code)
		{
			var geometry = _view.TextViewLines.GetMarkerGeometry(_line.Extent);

			var textBlock = new TextBlock
			{
				Width = 600,
				Background = Brushes.Black,
				Foreground = Brushes.White,
				Height = 300,
				Opacity = 0.5,
				Text = code
			};
			_snippetPreview = textBlock;
			Canvas.SetLeft(textBlock, geometry.Bounds.Left);
			Canvas.SetTop(textBlock, geometry.Bounds.Bottom);

			_layer.AddAdornment(AdornmentPositioningBehavior.TextRelative, _line.Extent, null, textBlock, (tag, ui) => { });
		}

		private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
		{
			if (_layer.IsEmpty)
			{
				DrawCompletionInstructions();
				DrawSnippetPreview(_initCode);
			}
		}

		internal void UpdateSnippetPreview(string code)
		{
			_layer.RemoveAdornment(_snippetPreview);
			DrawSnippetPreview(code);
		}

		internal void RemoveVisuals()
		{
			_view.LayoutChanged -= OnLayoutChanged;
			_layer.RemoveAllAdornments();
		}
	}
}
