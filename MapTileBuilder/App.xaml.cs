using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using MapTileBuilder.ViewModel;

namespace MapTileBuilder
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);
			var viewModel = new MainWindowViewModel();
			MainWindow window = new MainWindow();
			window.DataContext = viewModel;

			EventHandler handler = null;
			handler = delegate
			{
				viewModel.RequestClose -= handler;
				window.Close();
			};
			viewModel.RequestClose += handler;

			window.Show();	
		}
	}
}
