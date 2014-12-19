using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DtxUpload {
	public class ApplicationSettings : Settings {

		private string app_dir;
		private string company;
		private string title;
		private string version;

		/// <summary>
		/// Constructor for Config class.
		/// </summary>
		/// <param name="callback">Delegate that is called when the config file does not exist.  Can call upgrade inside to pull in the previous settings.</param>
		/// <param name="upgrade">Set to true to upgrade the settings from the last version.</param>
		public ApplicationSettings(Action<ApplicationSettings> callback, bool upgrade) {
			app_dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			object[] company_atts = typeof(ApplicationSettings).Assembly.GetCustomAttributes(typeof(AssemblyCompanyAttribute), true);
			object[] title_atts = typeof(ApplicationSettings).Assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), true);
			object[] version_atts = typeof(ApplicationSettings).Assembly.GetCustomAttributes(typeof(AssemblyFileVersionAttribute), true);

			if (company_atts.Length == 1 && title_atts.Length == 1 && version_atts.Length == 1) {
				company = ((AssemblyCompanyAttribute)company_atts[0]).Company;
				title = ((AssemblyTitleAttribute)title_atts[0]).Title;
				version = ((AssemblyFileVersionAttribute)version_atts[0]).Version;
			} else {
				throw new Exception("Unable to find company or title information in current assembly.");
			}

			settings_file = Path.Combine(app_dir, company, title, version, "settings.ini");
			
			if (!File.Exists(settings_file)) {
				Directory.CreateDirectory(Path.GetDirectoryName(settings_file));

				if (upgrade) {
					Upgrade(false);
				}
				callback(this);
				Save();
			} else {
				Reload();
			}
		}

		/// <summary>
		/// Copy the previous settings from this applciation to the new settings file.  Will reload the settings in the file to the application.
		/// Will overwrite 
		/// </summary>
		/// <param name="overwrite">If set to true, the current existing settings file will be overwridden.</param>
		/// <returns>True on successful import, false otherwise.</returns>
		public bool Upgrade(bool overwrite = false) {
			if (overwrite == false && File.Exists(settings_file)) {
				return false;
			}

			string base_dir =  Path.Combine(app_dir, company, title);
			var directory = new DirectoryInfo(base_dir);
			var version_directories = directory.GetDirectories();
			List<Version> versions = new List<Version>();

			for (int i = 0; i < version_directories.Length; i++) {
				try {
					versions.Add(new Version(version_directories[i].Name));
				} catch { }
			}

			var ordered_versions = versions.OrderBy(ver => ver).ToArray();

			if (ordered_versions.Length == 1) {
				return false;
			}

			var previous_version = ordered_versions[ordered_versions.Length - 2];
			var previous_setting_file = Path.Combine(base_dir, previous_version.ToString(), "settings.ini");

			if (!File.Exists(previous_setting_file)) {
				return false;
			}

			File.Copy(previous_setting_file, settings_file);

			Reload();

			return true;

		}
	}
}
