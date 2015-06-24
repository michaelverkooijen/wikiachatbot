using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Specialized;
using System.Net;
using System.Numerics;
using System.Text;
using Newtonsoft.Json.Schema;

namespace WikiaBot {
	public class ConnectionManager {
		CookieContainer cookieJar;
		CookieClient client;

		public ConnectionManager (string cookieDomain, string cookieName) {
			client = new CookieClient ();
			cookieJar = new CookieContainer ();
			String domain = CookieClient.GetCookieDomain (cookieDomain);
			Console.WriteLine (domain);
			Cookie c = new Cookie ();
			c.Domain = domain;
			c.Name = cookieName;
			//cookieJar.Add(new Cookie("wikicities", "cookie_value", "/", ".wikia.com"));
			cookieJar.Add (c);
			client.Headers.Add ("user-agent", "Flightmare/chatbot");
			client.Headers.Add ("Connection", "Keep-Alive");
		}

		/// <summary>
		/// Connects to the given MediaWiki URL and posts the request.
		/// </summary>
		/// <returns>String containing the queried result, in the format specified as argument. Default: XMLFM</returns>
		/// <param name="url">Base address.</param>
		/// <param name="args">Arguments <see cref="http://elderscrolls.wikia.com/api.php"/></param>
		public string GetRequest (string url, string[] args) {
			StringBuilder post = new StringBuilder ();
			post.Append (url);
			if (args.Length > 0) {
				post.Append ("?");
			}
			foreach (string s in args) {
				post.AppendFormat ("{0}{1}", "&", s);
			}
			using (client) {
				return client.DownloadString (post.ToString ());
			}
			//return null; //can WebClient.DownloadString() fail?
		}

		/// <summary>
		/// Posts the request.
		/// </summary>
		/// <returns>The request.</returns>
		/// <param name="url">URL.</param>
		/// <param name="args">Arguments.</param>
		/// <param name="body">Body.</param>
		public string PostRequest (string url, string[] args, string body) {
			StringBuilder post = new StringBuilder ();
			post.Append (url);
			if (args.Length > 0) {
				post.Append ("?");
			}
			foreach (string s in args) {
				post.AppendFormat ("{0}{1}", "&", s);
			}
			using (client) {
				return client.UploadString (post.ToString (), body);
			}
			//return null; //can WebClient.DownloadString() fail?
		}
		//NeedToken: check cookies!
		public bool Login (string wiki, string user, string pass) {
			using (client) {
				Console.WriteLine ("Sending Login");
				String response = Encoding.ASCII.GetString (client.UploadValues ("http://" + wiki + ".wikia.com/api.php?action=login&lgname=" + user + "&lgpassword=" + pass + "&format=json", new NameValueCollection () { }));
				Console.WriteLine (response);
				var o = JObject.Parse (response);
				string result = (string)o ["login"] ["result"];
				Console.WriteLine ("result: " + result);
				if (result.Equals ("Success")) {
					Console.WriteLine ("success!");
					return true;
				}
				if (result.Equals ("NeedToken")) {
					string token = (string)o ["login"] ["token"];
					Console.WriteLine ("token: " + token);
					response = Encoding.ASCII.GetString (client.UploadValues ("http://" + wiki + ".wikia.com/api.php?action=login&lgname=" + user + "&lgpassword=" + pass + "&format=json&lgtoken=" + token, new NameValueCollection () { }));
				}
				Console.WriteLine (response);
				o = JObject.Parse (response);
				result = (string)o ["login"] ["result"];
				Console.WriteLine ("result: " + result);
				if (result.Equals ("Success")) {
					Console.WriteLine ("success!");
					return true;
				}
			}
			return false;
		}
	}
}

