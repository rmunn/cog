﻿using System.Windows;

namespace SIL.Cog.Views
{
	/// <summary>
	/// Interaction logic for EditVarietyDialog.xaml
	/// </summary>
	public partial class EditVarietyDialog
	{
		public EditVarietyDialog()
		{
			InitializeComponent();
		}

		private void okButton_Click(object sender, RoutedEventArgs e)
		{
			if (ViewUtilities.IsValid(this))
				DialogResult = true;
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			_nameTextBox.Focus();
			_nameTextBox.SelectAll();
		}
	}
}