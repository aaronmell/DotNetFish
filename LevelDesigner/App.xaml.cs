using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using DotNetFish.Wpf.LevelDesigner.ViewModel;

namespace MapBuilder
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			LoadWindowViewModel viewModel = new LoadWindowViewModel();
			LoadWindow window = new LoadWindow();

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
