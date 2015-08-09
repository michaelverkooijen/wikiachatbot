using System;
using Newtonsoft.Json.Linq;
using System.Net;
using System.IO;

namespace WikiaBot {
	public class UploadLog {
		ConnectionManager cm;
		private string wiki, user, pass;
		public UploadLog (string wiki, string user, string pass) {
			this.wiki = wiki;
			this.user = user;
			this.pass = pass;
			cm = new ConnectionManager ("http://" + wiki + ".wikia.com", "wikicities");
		}

		public string getEditToken() {
			string response = cm.GetRequest ("http://" + wiki + ".wikia.com/api.php", new string[] {
				"action=query",
				"prop=info",
				"titles=User:"+user+"/chatlog/list",
				"intoken=edit",
				"format=json"
			});
			Console.WriteLine (response);
			var o = JObject.Parse (response);
			return (string)o["query"]["pages"]["866883"]["edittoken"];
		}

		public bool upload(string date) {
			Console.WriteLine ("Entering upload module");
			//logging in:
			try {
				if (!cm.Login (wiki, user, pass)) {
					return false;
				}
			} catch (Exception e) {
				Console.WriteLine (e.ToString ());
				return false;
			}

			Console.WriteLine ("Requesting token");
			string token = getEditToken ();
			Console.WriteLine ("Token: " + token);
			token = WebUtility.UrlEncode (token);
			Console.WriteLine ("Encoded token " + token);


			/*string page = cm.GetRequest ("http://" + wiki + ".wikia.com/wiki/User:" + user + "/chatlog/list", new string[] {
				"action=raw"
			});*/

			//update log list
			cm.PostRequest ("http://" + wiki + ".wikia.com/api.php", new string[] {
				"action=edit",
				"title=User:"+user+"/chatlog/list",
				"prependtext=*[[User:KINMUNE/chatlog/" + date + "|" + date + "]]\n",
				"summary=Adding log",
				"bot=1",
				"token=" + token
			}, "Hi");

			string page = "{{../}}\n" + File.ReadAllText(@""+date+".log");

			//upload the log
			//TODO: converto to multipart/form-data
			cm.PostRequest ("http://" + wiki + ".wikia.com/api.php", new string[] {
				"action=edit",
				"title=User:"+user+"/chatlog/" + date,
				"text=" + page,
				"summary=Adding log",
				"bot=1",
				"token=" + token
			}, "Hi");

			return true;
		}
	}
}

