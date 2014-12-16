using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace DtxUpload {
	public class Settings {
		protected string settings_file;
		private ConcurrentDictionary<string, string> settings = new ConcurrentDictionary<string, string>();
		private Dictionary<string, List<Action>> value_changed_events = new Dictionary<string, List<Action>>();
		private bool modified = false;
		
		/// <summary>
		/// Constructor for Config class.
		/// </summary>
		/// <param name="settings_file">Location of the file to save the program settings file.</param>
		/// <param name="callback">Delegate that is called when the config file does not exist.</param>
		public Settings(string settings_file, Action<Settings> callback) {
			this.settings_file = settings_file;

			if (!File.Exists(settings_file)) {
				callback(this);
				Save();
			} else {
				Reload();
			}
		}

		protected Settings() { }
		
		/// <summary>
		/// Save the settings to the settings file only if there are changes to be made.
		/// </summary>
		public void Save() {
			// No need to save if the file has not changed.
			if (!modified) {
				return;
			}

			if (!Directory.Exists(settings_file)) {
				Directory.CreateDirectory(Path.GetDirectoryName(settings_file));
			}

			using (StreamWriter sw = new StreamWriter(settings_file)) {
				foreach (string key in settings.Keys) {
					sw.Write(key.ToLower());
					sw.Write("=");
					sw.Write(settings[key]);
					sw.Write(Environment.NewLine);
				}
			}
		}

		/// <summary>
		/// Load all the settings from the ini file.
		/// </summary>
		public void Reload() {
			settings.Clear();

			using (StreamReader file = new StreamReader(settings_file)) {
				string line;
				while ((line = file.ReadLine()) != null) {
					int split = line.IndexOf('=');
					if (split != -1) {
						settings.Add(line.Substring(0, split), line.Substring(split + 1));
					}
				}

			}
		}


		/// <summary>
		/// Add a event delegate to be called any time the specified value changes.
		/// </summary>
		/// <param name="name">Name of the property to add the event to.</param>
		/// <param name="callback">Event delegate to call when the value has changed.</param>
		public void AddValueChangedEvent(string name, Action callback) {
			if (value_changed_events.ContainsKey(name)) { // Check to see if there is already another delegate added on this key.
				value_changed_events[name].Add(callback);
			} else { // Key does not exist so we have to add the List to the Dict.
				var event_list = new List<Action>();
				event_list.Add(callback);
				value_changed_events.Add(name, event_list);
			}
		}
		/// <summary>
		/// Removes an event that is added to the event list.
		/// </summary>
		/// <param name="name">Name of the property to find the event inside.</param>
		/// <param name="callback">Event delegate to remove from the queue of callable events.</param>
		/// <returns>True of successful removal; False otherwise.</returns>
		public bool removeValueChangedEvent(string name, Action callback) {
			bool removed_callback = false;
			if (value_changed_events.ContainsKey(name)) {
				if (value_changed_events[name].Contains(callback)) {
					return value_changed_events[name].Remove(callback);
				}
			}
			return removed_callback;
		}
		/// <summary>
		/// Gets value assigned to the property.
		/// </summary>
		/// <typeparam name="T">Type to cast the config property to.</typeparam>
		/// <param name="name">Name of the property to retrieve.</param>
		/// <returns>Value that was requested. If property does not exist, return default(T) value.</returns>
		public T Get<T>(string name) {
			name = name.ToLower();
			if (settings.ContainsKey(name)) {
				try {
					return JsonConvert.DeserializeObject<T>(settings[name]);
				} catch {
					return default(T);
				}
			} else {
				return default(T);
			}
		}
		/// <summary>
		/// Gets value assigned to the property and sets the property if it DNE.
		/// </summary>
		/// <typeparam name="T">Type to cast the config property to.</typeparam>
		/// <param name="name">Name of the property to retrieve.</param>
		/// <param name="default_value">Sets the default value for the object if the property does not exist.</param>
		/// <returns>Value that was requested. If property does not exist, return the default_value.</returns>
		public T Get<T>(string name, T default_value) {
			name = name.ToLower();
			if (settings.ContainsKey(name)) {
				try {
					return JsonConvert.DeserializeObject<T>(settings[name]);
				} catch {
					Set(name, default_value);
					return default_value;
				}
			} else {
				Set(name, default_value);
				return default_value;
			}
		}
		/// <summary>
		/// Checks to see if the requested property exists.
		/// </summary>
		/// <param name="name">Name of the property to verify exists.</param>
		/// <returns>True if the property exists, false if it does not.</returns>
		public bool PropertyExist(string name) {
			return settings.ContainsKey(name);
		}

		/// <summary>
		/// Set a value to a proeprty.
		/// </summary>
		/// <remarks>
		/// Values are not case sensitive. All stored values are automatically converted to lowercase.
		/// </remarks>
		/// <param name="name">Property name.</param>
		/// <param name="value">Value to save to the property.</param>
		public void Set(string name, object value) {
			name = name.ToLower();
			if (settings.ContainsKey(name)) {
				settings[name] = JsonConvert.SerializeObject(value);
			} else {
				settings.Add(name, JsonConvert.SerializeObject(value));
			}

			// Check to see if we have any events queued.
			if (value_changed_events.ContainsKey(name)) {
				foreach (var vc_event in value_changed_events[name]) {
					vc_event();
				}
			}
			modified = true;
		}

		/// <summary>
		/// Sets a value to a property if it has not already been set.
		/// </summary>
		/// <param name="name">Property name.</param>
		/// <param name="value">Value to save to the property if the property is not set already.</param>
		public void SetIfEmpty(string name, object value) {
			if (settings.ContainsKey(name) == false) {
				Set(name, value);
			}
		}
	}
}