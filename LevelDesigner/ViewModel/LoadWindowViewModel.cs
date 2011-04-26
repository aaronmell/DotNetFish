using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DotNetFish.BaseMvvm;
using DotNetFish.GameObjects;
using System.Windows.Input;

namespace DotNetFish.Wpf.LevelDesigner.ViewModel
{
	public class LoadWindowViewModel : ViewModelBase
	{
		private RelayCommand _openNewMap;
		private RelayCommand _openExistingMap;
		private RelayCommand _closeCommand;
		public event EventHandler RequestClose;

		public LoadWindowViewModel()
		{
			
		}

		public ICommand OpenNewMap
		{
			get
			{
				if (_openNewMap == null)
					_openNewMap = new RelayCommand(param => OpenNewMapCommand());

				return _openNewMap;
				
			}
		}

		public ICommand OpenExistingMap
		{
			get
			{
				if (_openExistingMap == null)
					_openExistingMap = new RelayCommand(param => OpenExistingMapCommand());

				return _openExistingMap;

			}
		}

		public ICommand CloseCommand
		{
			get
			{
				if (_closeCommand == null)
					_closeCommand = new RelayCommand(param => this.OnRequestClose());

				return _closeCommand;
			}
		}

		private void OpenExistingMapCommand()
		{
			Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
			dlg.DefaultExt = ".dfl"; // Default file extension
			dlg.Filter = "DotNetFish Level (.dfl)|*.dfl";

			Nullable<bool> result = dlg.ShowDialog();

			if (result == true)
			{
				// Open document
				string filename = dlg.FileName;
				GameWorld gameWorld = DotNetFish.LevelBuilder.FileIO.LoadMap(filename);
				EditLevel editLevel = new EditLevel(gameWorld);
				editLevel.Show();
			}
		}

		private void OpenNewMapCommand()
		{
			SelectMapRegion selectMapRegion = new SelectMapRegion();
			SelectMapRegionViewModel viewModel = new SelectMapRegionViewModel();
			selectMapRegion.DataContext = viewModel;
			selectMapRegion.Show();
			//this.Hide();  
		}

		void OnRequestClose()
		{
			EventHandler handler = this.RequestClose;
			if (handler != null)
				handler(this, EventArgs.Empty);
		}
	}
}
