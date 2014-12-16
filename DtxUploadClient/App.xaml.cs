using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace DtxUpload {
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application {

		private static ApplicationSettings settings;

		public static Settings Settings {
			get { return settings; }
		}


		private void Application_Startup(object sender, StartupEventArgs e) {
			settings = new ApplicationSettings(s => {
				var obj = new[] {
					new { Url = "upload.dtronix.com", Name = "Dtronix Upload Test ", ConnectionCount = 0 }
				};
				s.SetIfEmpty("servers.list", obj);
			}, true);
			//ApplicationSettingsBase
			//string base_dir
			//settings = new Settings()
		}
	}
}
