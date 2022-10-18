﻿using Extension.Caching;
using GraphQLClient;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static Extension.Settings.OptionsPageProvider;

namespace Extension.Settings
{
	/// <summary>
	/// Interaction logic for OptionsPage.xaml
	/// </summary>
	public partial class OptionsPage : UserControl
	{
		private CodigaClient CodigaClient { get; }

		internal CodigaOptionPage extensionOptionsPage;

		public OptionsPage()
		{
			InitializeComponent();
			CodigaClient = new CodigaClient();
		}
		
		public void Initialize()
		{
			cbUseCodingAssistant.IsChecked = CodigaOptions.Instance.UseCodingAssistant;
			cbUseInlineCompletion.IsChecked = CodigaOptions.Instance.UseInlineCompletion;
			txtToken.Text = CodigaOptions.Instance.ApiToken;
			CodigaOptions.Instance.Save();
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
			CodigaClient.SetApiToken(txtToken.Text);

			btnVerify.IsEnabled = false;
			lblUserName.Text = "";
			imgError.Visibility = Visibility.Collapsed; 
			imgCheck.Visibility = Visibility.Collapsed;

			var result = ThreadHelper.JoinableTaskFactory.Run(async () =>
			{
				return await CodigaClient.GetUserAsync();
			});

			string response;
			if (result.Errors != null && result.Errors.Any())
			{
				response = result.Errors.First().Message;
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