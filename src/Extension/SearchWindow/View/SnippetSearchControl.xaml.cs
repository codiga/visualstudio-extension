using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;
using EventTrigger = Microsoft.Xaml.Behaviors.EventTrigger;

namespace Extension.SearchWindow.View
{
	/// <summary>
	/// Interaction logic for SnippetSearchControl.
	/// </summary>
	public partial class SnippetSearchControl : UserControl
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SnippetSearchControl"/> class.
		/// </summary>
		public SnippetSearchControl()
		{
			// workaround see https://github.com/microsoft/XamlBehaviorsWpf/issues/86
			_ = new EventTrigger();
			InitializeComponent();
		}
	}
}