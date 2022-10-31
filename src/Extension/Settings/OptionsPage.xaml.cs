using Extension.Caching;
using Extension.Logging;
using GraphQLClient;
using Microsoft.VisualStudio.Shell;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Extension.Settings
{
	/// <summary>
	/// Interaction logic for OptionsPage.xaml
	/// </summary>
	public partial class OptionsPage : UserControl
	{
		internal CodigaOptionPage extensionOptionsPage;

		public OptionsPage()
		{
			InitializeComponent();
		}
		
		public void Initialize()
		{
			try
			{
				var settings = EditorSettingsProvider.GetCurrentCodigaSettings();

				cbUseCodingAssistant.IsChecked = settings.UseCodingAssistant;
				cbUseInlineCompletion.IsChecked = settings.UseInlineCompletion;
				txtToken.Text = settings.ApiToken;

				CodigaOptions.Instance.Save();
			}
			catch (System.Exception e)
			{
				ExtensionLogger.LogException(e);
			}
		}

		private void UseCodingAssistant_Checked(object sender, System.Windows.RoutedEventArgs e)
		{
			CodigaOptions.Instance.UseCodingAssistant = (bool)cbUseCodingAssistant.IsChecked;
			CodigaOptions.Instance.Save();
		}

		private void UseCodingAssistant_Unchecked(object sender, System.Windows.RoutedEventArgs e)
		{
			CodigaOptions.Instance.UseCodingAssistant = (bool)cbUseCodingAssistant.IsChecked;
			CodigaOptions.Instance.Save();
		}

		private void UseInlineCompletion_Checked(object sender, System.Windows.RoutedEventArgs e)
		{
			CodigaOptions.Instance.UseInlineCompletion = (bool)cbUseInlineCompletion.IsChecked;
			CodigaOptions.Instance.Save();
		}
		private void UseInlineCompletion_Unchecked(object sender, System.Windows.RoutedEventArgs e)
		{
			CodigaOptions.Instance.UseInlineCompletion = (bool)cbUseInlineCompletion.IsChecked;
			CodigaOptions.Instance.Save();
		}

		private void VerifyToken_Clicked(object sender, System.Windows.RoutedEventArgs e)
		{
			var provider = new DefaultCodigaClientProvider();
			if (!provider.TryGetClient(out var client))
				return;

			client.SetApiToken(txtToken.Text);

			btnVerify.IsEnabled = false;
			lblUserName.Text = "";
			imgError.Visibility = Visibility.Collapsed; 
			imgCheck.Visibility = Visibility.Collapsed;
			
			var result = ThreadHelper.JoinableTaskFactory.Run(async () =>
			{
				return await client.GetUserAsync();
			});

			string response;
			if (result.Errors != null && result.Errors.Any())
			{
				response = CodigaClient.GetReadableErrorMessage(result.Errors.First().Message);
				imgError.Visibility = Visibility.Visible;
			}
			else
			{
				CodigaOptions.Instance.ApiToken = txtToken.Text;
				CodigaOptions.Instance.Save();
				btnVerify.IsEnabled = true;
				imgCheck.Visibility = Visibility.Visible;
				response = $"Logged in as {result.Data.User.UserName}";
			}

			lblUserName.Text = response;

			btnVerify.IsEnabled = true;
		}
	}
}
