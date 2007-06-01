//
// Jb Evain  <jb@nurv.fr>
// Renaud Gaudin  <reg@nurv.fr>
//
// Licensed under the GNU GPL.
// See COPYING.
//

namespace GMailTray {

	using System;
	using System.IO;
	using System.Xml;
	using System.Xml.XPath;

	public struct Item {

		public string Title;
		public string Summary;
		public string AuthorName;
		public string AuthorMail;
		public string Link;
	}

	public sealed class ItemFactory {

		public static readonly ItemFactory Instance =
			new ItemFactory ();

		ItemFactory ()
		{
		}

		public Item [] GetItemsFromStream (Stream stream)
		{
			XPathDocument doc = new XPathDocument (stream);

			XPathNavigator nav = doc.CreateNavigator ();
			nav.MoveToRoot ();

			XPathNodeIterator it = Query (nav, "//atom:entry");

			Item [] items = new Item [it.Count];
			for (int i = 0; it.MoveNext(); i++) {
				Item item = new Item ();

				item.Title = NodeValue (it.Current, "atom:title");
				item.Summary = NodeValue (it.Current, "atom:summary");
				item.AuthorName = NodeValue (it.Current, "atom:author/atom:name");
				item.AuthorMail = NodeValue (it.Current, "atom:author/atom:email");
				item.Link =  NodeValue (it.Current, "atom:link/@href" );

				items [i] = item;
			}

			return items;
		}

		string NodeValue (XPathNavigator nav, string xpath)
		{
			XPathNodeIterator it = Query (nav, xpath);
			it.MoveNext ();
			return it.Current.Value;
		}

		XPathNodeIterator Query (XPathNavigator nav, string xpath)
		{
			XmlNamespaceManager nsm = new XmlNamespaceManager (nav.NameTable);
			nsm.AddNamespace ("atom", "http://purl.org/atom/ns#");

			XPathExpression expr = nav.Compile (xpath);
			expr.SetContext (nsm);

			return nav.Select (expr);
		}
	}
}
