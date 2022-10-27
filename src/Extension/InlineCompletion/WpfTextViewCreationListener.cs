﻿using Extension.AssistantCompletion;
using Extension.Logging;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace Extension.InlineCompletion
{
	/// <summary>
	/// Establishes an <see cref="IAdornmentLayer"/> to place the adornment on and exports the <see cref="IWpfTextViewCreationListener"/>
	/// that instantiates the adornment on the event of a <see cref="IWpfTextView"/>'s creation
	/// </summary>
	[Export(typeof(IWpfTextViewCreationListener))]
	[ContentType("text")]
	[TextViewRole(PredefinedTextViewRoles.Document)]
	internal sealed class WpfTextViewCreationListener : IWpfTextViewCreationListener
	{
		/// <summary>
		/// Defines the adornment layer for the adornment. This layer is ordered
		/// after the selection layer in the Z-order
		/// </summary>
		[Export(typeof(AdornmentLayerDefinition))]
		[Name(InlineCompletionView.PreviewLayerName)]
		[Order(After = PredefinedAdornmentLayers.Text)]
		private AdornmentLayerDefinition editorAdornmentLayer;

		[Import]
		internal IVsEditorAdaptersFactoryService AdapterService = null;

		[Import]
		internal ExpansionClient ExpansionClient;

		internal Dictionary<IWpfTextView, InlineCompletionClient> InlineCompletionClients { get;}


		public WpfTextViewCreationListener()
		{
			InlineCompletionClients = new Dictionary<IWpfTextView, InlineCompletionClient>();
		}

		/// <summary>
		/// Called when a text view having matching roles is created over a text data model having a matching content type.
		/// Instantiates a TextAdornment1 manager when the textView is created.
		/// </summary>
		/// <param name="textView">The <see cref="IWpfTextView"/> upon which the adornment should be placed</param>
		public void TextViewCreated(IWpfTextView textView)
		{
			if (textView == null)
				return;

			try
			{
				textView.Closed += TextView_Closed;
				var client = new InlineCompletionClient();
				InlineCompletionClients.Add(textView, client);
				client.Initialize(textView, ExpansionClient);
			}
			catch(Exception e)
			{
				ExtensionLogger.LogException(e);
			}
		}

		private void TextView_Closed(object sender, System.EventArgs e)
		{
			var view = (IWpfTextView)sender;
			if(InlineCompletionClients.TryGetValue(view, out var client))
			{
				client.Dispose();
				InlineCompletionClients.Remove(view);
			}
		}
	}
}
