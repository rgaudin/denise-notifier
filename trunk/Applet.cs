//
// Jb Evain  <jb@nurv.fr>
// Renaud Gaudin  <reg@nurv.fr>
//
// Licensed under the GNU GPL.
// See COPYING.
//

namespace Denise {

	using System;
	using System.IO;
	using System.Net;
	using System.Reflection;
	using System.Security;
	using System.Security.Cryptography.X509Certificates;
	using System.Diagnostics;
	using System.Threading;
	using Gtk;
	
	public sealed class Applet {

		uint m_timeout;
		const string base_mail_url = "https://mail.google.com";
		const string gmail_domain_path = "/mail";
		const string inbox_path = "/?shva=1";
		const string compose_path = "/?view=cm&fs=1&tf=1&to=&su=&body=&shva=1";
		const string mail_feed_path = "/feed/atom";
		string domain = (Preferences.Instance.GoogleDomain == "gmail.com") ? "/mail" : "/a/"+ Preferences.Instance.GoogleDomain;
		
		DateTime last_check = DateTime.Now;

		Egg.TrayIcon m_icon;
		Tooltips m_tt;
		EventBox m_eb;
		Gdk.Pixbuf m_zeroMail;
		Gdk.Pixbuf m_haveMails;
		Image m_current;
		Item [] items;
		Label nb_unread;
		HBox cont;
		
		Menu popupMenu = new Menu ();

		Applet ()
		{
			m_icon = new Egg.TrayIcon ("Denise");
			
			m_eb = new EventBox ();
			m_eb.ButtonPressEvent += new ButtonPressEventHandler (this.OnImageClick);
			m_icon.Add (m_eb);
			
			cont = new HBox();
			nb_unread = new Label("");
			nb_unread.UseMarkup = true;
			cont.Add (nb_unread);
			
			m_eb.Add(cont);
			
			m_tt = new Tooltips ();
			m_zeroMail = new Gdk.Pixbuf (Assembly.GetExecutingAssembly (), "nounread.png");
			m_haveMails = new Gdk.Pixbuf (Assembly.GetExecutingAssembly (), "unread.png");

			Request ();
			
			int interval = 5;
			try {
				interval = int.Parse (Preferences.Instance.FetchInterval);
			} catch {}
			m_timeout = (uint) interval * 60000;
			GLib.Timeout.Add (m_timeout, new GLib.TimeoutHandler (Request));
		}

		Item [] AskGMail ()
		{
			try {
				WebRequest req = WebRequest.Create (base_mail_url+domain+mail_feed_path);
				req.Credentials = new NetworkCredential (Preferences.Instance.GoogleUsername, Preferences.Instance.GooglePassword);
				last_check = DateTime.Now;
				return ItemFactory.Instance.GetItemsFromStream (req.GetResponse ().GetResponseStream ());
			} catch {
				return new Item [0];
			}
		}

		void ShowMails (Item [] items)
		{
			if (m_current != null)
				cont.Remove (m_current);
				cont.Remove (nb_unread);
			if (items.Length > 0) {
				m_current = new Image (m_haveMails);
				m_tt.SetTip (m_eb, string.Format ("{0} unread mail{1}", items.Length, items.Length > 1 ? "s" : string.Empty), null);
			} else {
				m_current = new Image (m_zeroMail);
				m_tt.SetTip (m_eb, "no unread mail", null);
			}
			cont.PackStart (m_current);
			cont.PackStart (nb_unread);
			m_icon.ShowAll ();
			
			// menu generation
			
			popupMenu = new Menu ();
			
			string inboxmsg = "Go to Inbox";
			if (items.Length > 0) 
				inboxmsg += " - " + items.Length + " unread";
			MenuItem menuInbox = new MenuItem (inboxmsg);
			popupMenu.Add (menuInbox);
			
			MenuItem menuCompose = new MenuItem ("Compose Mail");
			popupMenu.Add (menuCompose);
			
			string checkmsg = "Check Mail";
			TimeSpan duration = DateTime.Now - last_check;
			if (duration.Minutes < 1)
				checkmsg += " - checked less than a minute ago";
			else
				checkmsg += " - checked " + duration.Minutes + " minutes ago";
			MenuItem menuCheck = new MenuItem (checkmsg);
			popupMenu.Add (menuCheck);
			
			//list of messages
			if (items.Length > 0) {
				nb_unread.Markup = "<span weight=\"bold\" size=\"large\" foreground=\"#333333\"> " + items.Length + "</span>";
				
				MenuItem mseparator = new MenuItem();
				popupMenu.Add(mseparator);
				
				foreach (Item anItem in items) {
					MenuItem mit = new MenuItem (anItem.AuthorName+" - "+anItem.Title);
					mit.Activated += new EventHandler (new MenuItemAction (this, anItem.Link).Activate);
					popupMenu.Add (mit);
				}
				
			} else {
				nb_unread.Markup = "";
			}
			//"<span weight=\"bold\">"+anItem.AuthorName+"</span> - <span foreground=\"#333333\">"+anItem.Title+"</span>");
			
			MenuItem separator = new MenuItem ();
			popupMenu.Add (separator);
			
			MenuItem menuOptions = new MenuItem ("Preferences");
			popupMenu.Add (menuOptions);
			
			MenuItem menuQuit = new MenuItem ("Quit Denise");
			popupMenu.Add (menuQuit);

			menuInbox.Activated += new EventHandler (this.OnInboxClick);
			menuCompose.Activated += new EventHandler (this.OnComposeClick);
			menuCheck.Activated += new EventHandler (this.OnCheckClick);
			menuOptions.Activated += new EventHandler (this.OnOptionsClick);
			menuQuit.Activated += new EventHandler (this.OnQuitClick);
			
		}
		
		class MenuItemAction {
			
			Applet _applet;
			string _link;

			public MenuItemAction (Applet applet, string link)
			{
				_applet = applet;
				_link = link;
			}

			public void Activate (object sender, EventArgs e)
			{
				_applet.BrowseURL (_link);
				Thread t = new Thread (_applet.DelayedRequest);
				t.Start ();
			}
		}

		bool Request ()
		{
			items = AskGMail ();
			ShowMails (items);
			return true;
		}
		
		void DelayedRequest ()
		{
			Thread.Sleep (10000);
			Request ();
		}

		void OnImageClick (object o, ButtonPressEventArgs args)
		{
			ShowMails (items);
			popupMenu.ShowAll ();
			popupMenu.Popup (null, null, null, IntPtr.Zero, args.Event.Button, args.Event.Time);
		}
		
		void OnInboxClick(object o, EventArgs args)
		{
			string inbox_url = base_mail_url + domain + inbox_path;
			BrowseURL ( inbox_url );
		}
		
		void OnComposeClick(object o, EventArgs args)
		{
			string compose_url = base_mail_url + domain + compose_path;
			BrowseURL ( compose_url );
		}
		
		void OnCheckClick(object o, EventArgs args)
		{
			Request();
		}
		
		void OnOptionsClick(object o, EventArgs args)
		{
			Preferences.Instance.Display ();
		}
		
		void OnQuitClick(object o, EventArgs args)
		{
			Application.Quit();
		}

		public static void Main ()
		{
			ServicePointManager.CertificatePolicy = new AppletCertificatePolicy ();

			Application.Init ();
			new Applet ();
			Application.Run ();
		}
		
		public void BrowseURL (string url)
		{
			Process browser = new Process();
			browser.StartInfo.FileName   = Preferences.Instance.DefaultBrowser;
			browser.StartInfo.Arguments = url;
			browser.Start();
		}
	}

	class AppletCertificatePolicy : ICertificatePolicy {

		public bool CheckValidationResult (ServicePoint sp, X509Certificate cert,
										  WebRequest request, int problem)
		{
			return true; // for the moment
		}
	}
}
