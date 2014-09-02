using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System.Threading;
using System.Diagnostics;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Numerics;
using System.Collections;
using System.Text.RegularExpressions;

namespace WikiaBot {
	public class ChatModule {
		private string wiki, user, pass, youtubeCredentials, chatKey, xhrKey, nodeHost;
		//TODO: replace with settings.json
		private string[] patterns = {"fu?ck", "f[a@]g", "cunt", "wanker", "d[i!]ck", "nigg[ae]r?", "slut", "wh[o0]re", "c[o0]ck"};
		private int roomId, nodeInstance;
		ConnectionManager cm;
		ArrayList namesBlacklist;
		private Boolean isMod, doesWelcome;

		public ChatModule (string wiki, string user, string pass, string youtubeCredentials, Boolean isMod, Boolean doesWelcome) {
			this.wiki = wiki;
			this.user = user;
			this.pass = pass;
			this.youtubeCredentials = youtubeCredentials;
			cm = new ConnectionManager ("http://" + wiki + ".wikia.com", "wikicities");
			namesBlacklist = new ArrayList ();
			namesBlacklist.Add (user); //prevent bot from talking to itself
			this.isMod = isMod;
			this.doesWelcome = doesWelcome;
		}
		//TODO: mid-session logins
		public bool start () {
			//logging in:
			try {
				if (!cm.Login (wiki, user, pass)) {
					return false;
				}
			} catch (Exception e) {
				Console.WriteLine (e.ToString ());
				return false;
			}
			//get wgchatkey
			string response = cm.GetRequest ("http://" + wiki + ".wikia.com/wikia.php", new string[] {
				"controller=Chat",
				"format=json"
			});
			var o = JObject.Parse (response);
			chatKey = (string)o ["chatkey"];
			roomId = (int)o ["roomId"];
			nodeHost = (string)o ["nodeHostname"];
			nodeInstance = (int)o ["nodeInstance"];
			Console.WriteLine ("chatKey: " + chatKey + " room: " + nodeHost + " roomId: " + roomId.ToString ());
			Console.WriteLine ("t=" + (DateTime.Now.ToUniversalTime () - new DateTime (1970, 1, 1)).TotalSeconds.ToString ());
			int failCount = 0;
			xhrKey = null;
			string lastResponse = "";
			//enter poll loop:
			//make fallback escape from loop
			while (failCount < 5) {
				Console.WriteLine ("Reading chat...");
				try {
					lastResponse = cm.GetRequest ("http://" + nodeHost + "/socket.io/", new string[] {
						"name=" + user,
						"key=" + chatKey,
						"roomId=" + roomId.ToString (),
						"serverId=" + nodeHost.ToString(),
						"EIO=2",
						"transport=polling",
						"t=" + (DateTime.Now.ToUniversalTime () - new DateTime (1970, 1, 1)).TotalSeconds
						//"sid="
					}); //read chat
					Console.WriteLine (lastResponse);
					//�433�
					string[] lines = lastResponse.Split ('\ufffd');
					foreach (string line in lines) {
						parseResponse (line);
					}
				} catch (Exception e) {
					lastResponse = "";
					failCount++;
					Console.WriteLine ("FAILED reading chat: " + e.ToString ());
				}
				/*if (xhrKey != null && !lastResponse.Equals ("")) {
					Console.WriteLine ("Reading chat...");
					try {
						lastResponse = cm.GetRequest ("http://" + nodeHost + "/socket.io/", new string[] {
							"name=" + user,
							"roomId=" + roomId.ToString (),
							"key=" + chatKey,
							//"t=" + (DateTime.Now.ToUniversalTime () - new DateTime (1970, 1, 1)).TotalSeconds
						}); //read chat
						Console.WriteLine (lastResponse);
						//�433�
						string[] lines = lastResponse.Split ('\ufffd');
						foreach (string line in lines) {
							parseResponse (line);
						}
					} catch (Exception e) {
						lastResponse = "";
						failCount++;
						Console.WriteLine ("FAILED reading chat: " + e.ToString ());
					}
				} else {
					Console.WriteLine ("Requesting xhr key...");
					try { 
						lastResponse = cm.GetRequest ("http://" + nodeHost + "/socket.io/", new string[] {
							"name=" + user,
							"roomId=" + roomId.ToString (),
							"key=" + chatKey,
							//"t=" + (DateTime.Now.ToUniversalTime () - new DateTime (1970, 1, 1)).TotalSeconds.ToString
						});
					} catch (Exception e) {
						lastResponse = "";
						failCount++;
						Console.WriteLine ("FAILED requesting key: " + e.ToString ());
					}
					int x = lastResponse.IndexOf (":");
					if (x > 0) {
						xhrKey = lastResponse.Substring (0, x);
					}
				}*/
				failCount = 0; //cycle is success, reset fail counter
				Thread.Sleep (1000);
			}
			return false;
		}

		private bool parseResponse (string s) {
			int prefix = s.IndexOf ("\"");
			prefix--;
			//Console.WriteLine(prefix); //4 etc
			if (prefix > 0) {
				s = s.Substring (prefix, s.Length - prefix);
				//Console.WriteLine(s); full string without 4::
				var o = JObject.Parse (s);
				string eve = (string)o ["event"];
				//Console.WriteLine(eve);
				switch (eve) {
				case "chat:add":
					{
						var data = JObject.Parse ((string)o ["data"]);
						string name = (string)data ["attrs"] ["name"];
						//Console.WriteLine("Name: " + name);
						string text = (string)data ["attrs"] ["text"];
						//Console.WriteLine("Text: " + text);
						string timestamp = (string)data ["attrs"] ["timeStamp"];
						//Console.WriteLine(timestamp);
						//var dt = new DateTime (1970, 1, 1, 0, 0, 0, 0).AddSeconds (Math.Round (Convert.ToInt64 (timestamp) / 1000d)).ToLocalTime ();
						var dt = new DateTime (1970, 1, 1, 0, 0, 0, 0).AddSeconds (Math.Round (Convert.ToInt64 (timestamp) / 1000d)).ToString ("yyyyMMdd HH:mm:ss");
						string line = "*" + dt.Split (' ') [1] + ": [[User:" + name + "|]]: <nowiki>" + text + "</nowiki>";
						Console.WriteLine (line);
						string filename = dt.Split (' ') [0] + ".log";
						Console.WriteLine (filename);
						try {
							using (StreamWriter file = File.AppendText (@filename)) {
								file.WriteLine (line);
							}
						} catch (Exception e) {
							Console.WriteLine (e.ToString ());
						}
						if (isMod && containsBadLanguage (text)) {
							Random r = new Random ();
							switch (r.Next (6)) {
							case 0:
								speak (name + ", please watch your language.");
								break;
							case 1:
								speak ("Please refrain from using bad words");
								break;
							}
						}
						if (name.Equals("Flightmare") && text.Equals("!tm")) { 
							isMod = !isMod;
							speak ("Moderate chat: " + isMod.ToString());
						}
						if (name.Equals("Flightmare") && text.Equals("!tw")) { 
							doesWelcome = !doesWelcome;
							speak ("Welcome users: " + doesWelcome.ToString());
						}
						/*if (text.Contains ("youtube")) { //TODO: regex: youtu.?be
						string[] words = text.Split(' ');
						foreach (string word in words){
							string[] args = word.Split ('?');
							foreach (string argument in args) {
								string[] subargs = argument.Split ('&');
								foreach (string subarg in subargs) {
									if (subarg.Contains("v=")) {
										string videoId = subarg.Replace ("v=", "");
										Console.WriteLine ("Found Video ID: " + videoId);
										string videoTitle = YoutubeModule.GetVideoTitle (videoId, youtubeCredentials);
										Console.WriteLine ("Video Title: " + videoTitle);
										if (!videoTitle.Equals("")) {
											speak (videoTitle);
										}
									}
								}
							}
						}
					}*/
					}
					break;
				case "join":
					{
						if (doesWelcome) {
							var data = JObject.Parse ((string)o ["data"]);
							string name = (string)data ["attrs"] ["name"];
							if (!namesBlacklist.Contains (name)) {
								speak ("Hello there, " + name + "!");
								namesBlacklist.Add (name);
							}
						}
					}
					break;
					//case "part" //TODO: check json, remove user from warnlist
				case "kick":
					{
						var data = JObject.Parse ((string)o ["data"]);
						string kickedName = (string)data ["attrs"] ["kickedUserName"];
						string modName = (string)data ["attrs"] ["moderatorName"];
						//string reason = (string)data ["attrs"] ["reason"];
						//TODO: test for correct timezone settings (should be UTC)
						string filename = DateTime.Now.ToUniversalTime ().ToString ("yyyyMMdd") + ".log";
						string line = "**[[User:" + modName + "|]] kicked [[User:" + kickedName + "|]]";
						Console.WriteLine (line);
						Console.WriteLine (DateTime.Now.ToUniversalTime ().ToString ("HH:mm:ss"));
						Console.WriteLine ("kick-filename: "+filename);
						try {
							using (StreamWriter file = File.AppendText (@filename)) {
								file.WriteLine (line);
							}
						} catch (Exception e) {
							Console.WriteLine (e.ToString ());
						}
					}
					break;
				case "ban":
					{
						var data = JObject.Parse ((string)o ["data"]);
						string kickedName = (string)data ["attrs"] ["kickedUserName"];
						string modName = (string)data ["attrs"] ["moderatorName"];
						string reason = (string)data ["attrs"] ["reason"];
						//int time = (int)data ["attrs"] ["time"];
						//TODO: test for correct timezone settings (should be UTC)
						string filename = DateTime.Now.ToUniversalTime ().ToString ("yyyyMMdd") + ".log";
						string line = "**[[User:" + modName + "|]] banned [[User:" + kickedName + "|]] with reason: " + reason;
						Console.WriteLine (line);
						Console.WriteLine (DateTime.Now.ToUniversalTime ().ToString ("HH:mm:ss"));
						Console.WriteLine ("kick-filename: "+filename);
						try {
							using (StreamWriter file = File.AppendText (@filename)) {
								file.WriteLine (line);
							}
						} catch (Exception e) {
							Console.WriteLine (e.ToString ());
						}
					}
					break;
				}
			}
			return true;
		}

		private void speak (string s) {
			string body = "5:::{\"name\":\"message\",\"args\":[\"{\\\"id\\\":null,\\\"cid\\\":\\\"c31\\\",\\\"attrs\\\":{\\\"msgType\\\":\\\"chat\\\",\\\"roomId\\\":" + roomId.ToString () + ",\\\"name\\\":\\\"" + user + "\\\",\\\"text\\\":\\\"" + s + "\\\",\\\"avatarSrc\\\":\\\"\\\",\\\"timeStamp\\\":\\\"\\\",\\\"continued\\\":false,\\\"temp\\\":false}}\"]}";
			cm.PostRequest ("http://" + nodeHost + "/socket.io/", new string[] {
				"name=" + user,
				"key=" + chatKey,
				"roomId=" + roomId.ToString (),
				"serverId=" + nodeHost.ToString(),
				"EIO=2",
				"transport=polling",
				"t=" + (DateTime.Now.ToUniversalTime () - new DateTime (1970, 1, 1)).TotalSeconds
				//"sid="
			}, body);
		}

		private Boolean containsBadLanguage (string s) {
			foreach (string pattern in patterns) {
				if (Regex.IsMatch (s, pattern, RegexOptions.IgnoreCase)) {
					return true;
				}
			}
			return false;
		}
	}
}

