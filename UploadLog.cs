using System;
using Newtonsoft.Json.Linq;
using System.Net;
using System.IO;

namespace WikiaBot {
	public class UploadLog {
		ConnectionManager cm;
		private string wiki, user;
		public UploadLog (string wiki, string user) {
			this.wiki = wiki;
			this.user = user;
			//cm = new ConnectionManager ("https://" + wiki + ".wikia.com", "wikicities"); //FIXME: breaks session make singleton connectionmanager
			cm = ConnectionManager.getConnection("https://" + wiki + ".wikia.com", "wikicities");
		}

		public string getEditToken() {
			string response = cm.GetRequest ("https://" + wiki + ".wikia.com/api.php", new string[] {
				"action=query",
				"prop=info",
				"titles=User:"+user,
				"intoken=edit",
				"format=json"
			});
			Console.WriteLine (response);
			var o = JObject.Parse (response);
			return (string)o["query"]["pages"]["556436"]["edittoken"];//FIXME: string selector to index
		}

		public bool upload(string date, string buffer) {
			Console.WriteLine ("Entering upload module");
			Console.WriteLine ("Requesting token");
			string token = getEditToken ();
			Console.WriteLine ("Token: " + token);
			token = WebUtility.UrlEncode (token);
			Console.WriteLine ("Encoded token " + token);

			buffer = WebUtility.UrlEncode (buffer);

			//string page = "{{../}}\n" + File.ReadAllText(@""+date+".log");

			//upload the log
			//TODO: converto to multipart/form-data
			string res;
			res = cm.PostRequest ("https://" + wiki + ".wikia.com/api.php", new string[] {
				"action=edit",
				"title=User:"+user+"/chatlog/" + date,
				"appendtext=" + buffer,
				"summary=Adding%20log",
				"bot=1",
				"token=" + token
			}, "");
			Console.WriteLine (res);
			return true;
		}
	}
}

