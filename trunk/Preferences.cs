//
// Jb Evain  <jb@nurv.fr>
// Renaud Gaudin  <reg@nurv.fr>
//
// Licensed under the GNU GPL.
// See COPYING.
//

namespace Denise {
	
	using System;
	using Gtk;
	using GConf;
	using System.Collections;
	using Gnome.Keyring;
	 
	class Preferences {
		
		GConf.Client client; // gconf client
		
		// these strings represent our gconf paths
		static readonly string GCONF_APP_PATH = "/apps/denise/denise_notifier";
		static readonly string USERNAME_KEY = GCONF_APP_PATH + "/google_username";
		static readonly string USESSL_KEY = GCONF_APP_PATH + "/google_usessl";
		static readonly string BRO_KEY = GCONF_APP_PATH + "/google_usessl";
		static readonly string BROWSER_KEY = GCONF_APP_PATH + "/browser";
		static readonly string INTERVAL_KEY = GCONF_APP_PATH + "/fetch_interval";
		static readonly string KEYRING_NAME = "GooglePassword@denise";
		
		private string m_google_username = string.Empty;
		private string m_google_password = string.Empty;
		private bool m_google_usessl	 = true;
		private string m_google_domain = string.Empty;
		private string m_browser = string.Empty;
		private string m_interval = string.Empty;
		
		private Hashtable keyring_attr = new Hashtable ();
		
		// defines our edit boxes and our checkbox
		Entry username;
		Entry password;
		CheckButton usessl;
		Entry browser;
		Entry interval;
		
		// squeleton
		public static readonly Preferences Instance = new Preferences ();
		
		public string GoogleUsername {
			get { return m_google_username; }
			set { 
				client.Set (USERNAME_KEY, value);
				m_google_username = value;
				GoogleDomain = value;
			}
		}
		
		public string GooglePassword { 
			get { return m_google_password;} 
			set { 
				Ring.CreateItem (null, ItemType.GenericSecret, "Google Password for Denise Notifier", keyring_attr, value, true); 
				m_google_password = value;
			}
		}
		
		public string DefaultBrowser {
			get { return m_browser; }
			set { 
				client.Set (BROWSER_KEY, value);
				m_browser = value;
			}
		}
		
		public string FetchInterval {
			get { return m_interval; }
			set { 
				client.Set (INTERVAL_KEY, value);
				m_interval = value;
			}
		}
		
		public bool GoogleUseSSL {
			get { return m_google_usessl;}
			set { m_google_usessl= value; }
		}
		
		public string GoogleDomain {
			get { return m_google_domain; }
			set { 
				if (value.Length > 0 && value.IndexOf ("@") != -1) {
					m_google_domain = value.Split ("@".ToCharArray ())[1];
				}
			}
		}
		
		Preferences()
		{
			// Keyring attribute needed to match
			keyring_attr["name"] = KEYRING_NAME;
			ItemData[] keyring_items = Ring.Find (ItemType.GenericSecret, keyring_attr);
			if (keyring_items.Length == 0) {
				Ring.CreateItem (null, ItemType.GenericSecret, "Google Password for Denise Notifier", keyring_attr, String.Empty, true);
			}
			
			// creates our link to gconf
			client = new GConf.Client();
			// tries to grab values from gconf and update the GUI
			UpdateFromGConf();
			// sets the function to be called if some key changes
			//client.AddNotify (GCONF_APP_PATH, new NotifyEventHandler (GConf_Changed));
		}
		
		public void Display()
		{
			
			// creates new window with the given title 
			Window w = new Window("Denise Preferences");
			w.SetPosition ( Gtk.WindowPosition.Center );
			// creates all edit boxes
			username = new Entry();
			password = new Entry();
			password.Visibility = false;
			browser = new Entry();
			interval = new Entry();
			usessl = new CheckButton("Use SSL URLs ?");
			// we create a big vertical box where we will place other widgets
			VBox globvbox = new VBox();
			
			// setting up the box so we look more HIGish
			globvbox.BorderWidth = 12;
			globvbox.Spacing = 6;
			
			// adding the vbox to our window
			w.Add(globvbox);
			
			// we set up our label
			Label info = new Label("<span weight=\"bold\" size=\"large\"> Your Information</span>");
			info.UseMarkup = true;
			// and add it to the vertical box
			globvbox.PackStart(info, false, false, 0);
			
			// creates a new horizontal box
			HBox horz = new HBox();
			horz.Spacing = 6;
			
			// adds the horizontal box the the global vertical
			globvbox.Add(horz);
			
			// creating another vbox which will hold our labels
			VBox lblcnt = new VBox();
			lblcnt.Spacing = 6;
			// adds the vbox to the horizontal one
			horz.PackStart(lblcnt, false, false, 0);
			
			// creates label
			Label lbl = new Label("Username:");
			lbl.Xalign = 0; // aligns it to the left
			lblcnt.PackStart(lbl, true, false, 0);
			
			Label lblp = new Label("Password:");
			lblp.Xalign = 0; // aligns it to the left
			lblcnt.PackStart(lblp, true, false, 0);
			
			Label lblb = new Label("Browser:");
			lblb.Xalign = 0; // aligns it to the left
			lblcnt.PackStart(lblb, true, false, 0);
			
			Label lbli = new Label("Fecth Interval:");
			lbli.Xalign = 0; // aligns it to the left
			lblcnt.PackStart(lbli, true, false, 0);
		
			
			// another vbox to hold the edit boxes
			VBox ntrycnt = new VBox();
			ntrycnt.Spacing = 6; // HIGying
			// adds the vbox containing the edit boxes to the horizontal one
			horz.PackStart(ntrycnt, true, true, 0);
			
			// adding all the edit boxes
			ntrycnt.PackStart(username, true, true, 0);
			ntrycnt.PackStart(password, true, true, 0);
			ntrycnt.PackStart(browser, true, true, 0);
			ntrycnt.PackStart(interval, true, true, 0);
			// last, but not least - the check box
			globvbox.PackStart(usessl, false, false, 0); 
			
			// hooks events
			usessl.Toggled += on_usessl_toggled;
			username.Changed += on_username_activate;
			password.Changed += on_password_activate;
			browser.Changed += on_browser_activate;
			interval.Changed += on_interval_activate;
			
			UpdateFromGConf();
			// shows the window
			w.ShowAll();
		}
		
		// function to grab values from gconf and update the GUI
		void UpdateFromGConf ()
		{
			try {
				GoogleUsername = (string) client.Get (USERNAME_KEY);
				GoogleUseSSL = (bool) client.Get (USESSL_KEY);
				ItemData[] keyring_items = Ring.Find (ItemType.GenericSecret, keyring_attr);
				GooglePassword = keyring_items[0].Secret;
				DefaultBrowser = (string) client.Get (BROWSER_KEY);
				FetchInterval = (string) client.Get (INTERVAL_KEY);
			} catch (GConf.NoSuchKeyException e) {
				Console.WriteLine("Error: A key with that name doesn't exist." + e.ToString ());
				// add your exception handling here
			} catch (System.InvalidCastException e) {
				Console.WriteLine("Error: Cannot typecast."+ e.ToString ());
				// add your exception handling here
			}
			try {
				username.Text = GoogleUsername;
				password.Text = GooglePassword;
				usessl.Active = GoogleUseSSL;
				browser.Text = DefaultBrowser;
				interval.Text = FetchInterval;
			} catch {
				// window is not on screen
			}
		}
		
		public void on_usessl_toggled (object o, EventArgs args)
		{
			client.Set (USESSL_KEY, usessl.Active);
			GoogleUseSSL = usessl.Active;
		}
		
		public void on_username_activate (object o, EventArgs args)
		{	
			GoogleUsername = username.Text;
		}
		
		public void on_password_activate (object o, EventArgs args)
		{
			GooglePassword = password.Text;
		}
		
		public void on_browser_activate (object o, EventArgs args)
		{
			DefaultBrowser = browser.Text;
		}
		
		public void on_interval_activate (object o, EventArgs args)
		{
			FetchInterval = interval.Text;
		}
		
		public void GConf_Changed (object sender, NotifyEventArgs args)
		{
			// sets the corresponding value in gconf
			UpdateFromGConf();
		}
	}
}