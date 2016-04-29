using System;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Collections;
using System.Text.RegularExpressions;
using System.Net;

namespace WikiaBot {
	public class ChatModule {
		private string wiki, user, pass, youtubeCredentials, chatKey, sid, nodeHost;
		//TODO: replace with settings.json
		private string[] patterns = {
			"fu?ck",
			"f[a@]g",
			"cunt",
			"wanker",
			"d[i!]ck",
			"nigg[ae]r?",
			"slut",
			"wh[o0]re",
			"c[o0]ck"
		};
		private string[] parameters = {
			"name=",
			"key=",
			"roomId=",
			"serverId=",// + nodeInstance.ToString (), //pre 20160427 chat update: "serverId=" + nodeInstance.ToString () //TODO: acquire wgCityId
			"wikiId=",// + nodeInstance.ToString(), //pre 20160427 chat update: did not exist
			"EIO=3",
			"transport=polling",
			//"t=" + (DateTime.Now.ToUniversalTime () - new DateTime (1970, 1, 1)).TotalSeconds,
			"sid="
		};
		private int roomId, nopCount;
		ConnectionManager cm;
		ArrayList namesBlacklist;
		private Boolean isMod, doesWelcome;

		public ChatModule (string wiki, string user, string pass, string youtubeCredentials, Boolean isMod, Boolean doesWelcome) {
			this.wiki = wiki;
			this.user = user;
			this.pass = pass;
			this.youtubeCredentials = youtubeCredentials;
			//cm = new ConnectionManager ("https://" + wiki + ".wikia.com", "wikicities");
			cm = ConnectionManager.getConnection ("https://" + wiki + ".wikia.com", "wikicities");
			namesBlacklist = new ArrayList ();
			namesBlacklist.Add (user); //prevent bot from talking to itself
			this.isMod = isMod;
			this.doesWelcome = doesWelcome;
		}

		public bool getChatController () {
			try {
				//get wgchatkey
				string response = cm.GetRequest ("https://" + wiki + ".wikia.com/wikia.php", new string[] {
					"controller=Chat",
					"format=json"
				});
				var o = JObject.Parse (response);
				chatKey = (string)o ["chatkey"];
				roomId = (int)o ["roomId"];
				nodeHost = (string)o ["chatServerHost"];
				//nodeInstance = (int)o ["nodeInstance"];
				Console.WriteLine ("chatKey: " + chatKey + " room: " + nodeHost + " roomId: " + roomId.ToString ());
				return true;
			} catch (Exception e) {
				Console.WriteLine (e.ToString ());
				return false;
			}
		}

		public string getWikiId () {
			try {
				string response = cm.GetRequest ("https://" + wiki + ".wikia.com/api.php", new string[] {
					"action=query",
					"meta=siteinfo",
					"siprop=wikidesc",
					"format=json"
				});
				var o = JObject.Parse (response);
				return (string)o ["query"] ["wikidesc"] ["id"];
			} catch (Exception e) {
				Console.WriteLine ("Something went wrong getting wgCityId:" + e.ToString ());
				return null;
			}
		}

		//Servers might take a few minutes to update the list.
		public JObject getUserList () {
			try {
				string response = cm.GetRequest ("https://" + wiki + ".wikia.com/wikia.php", new string[] {
					"controller=ChatRail",
					"method=GetUsers",
					"format=json"
				});
				var o = JObject.Parse (response);
				return o;
			} catch (Exception e) {
				Console.WriteLine ("Something went wrong getting userlist:" + e.ToString ());
				return null;
			}
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
			Console.WriteLine ("Getting Chat controller");
			string response = cm.GetRequest ("https://" + wiki + ".wikia.com/wikia.php", new string[] {
				"controller=Chat",
				"format=json"
			});
			var o = JObject.Parse (response);
			chatKey = (string)o ["chatkey"];
			roomId = (int)o ["roomId"];
			nodeHost = (string)o ["chatServerHost"]; //pre 20160427 chat update: nodeHostname
			//nodeInstance = (int)o ["nodeInstance"];
			Console.WriteLine ("chatKey: " + chatKey + " room: " + nodeHost + " roomId: " + roomId.ToString ());
			string wgCityId = getWikiId ();
			parameters [0] += user;
			parameters [1] += chatKey;
			parameters [2] += roomId.ToString ();
			parameters [3] += wgCityId;
			parameters [4] += wgCityId;
			int failCount = 0;
			sid = null;//TODO: remove
			parameters [7] = "sid=";
			string lastResponse = "";
			//enter poll loop:
			//make fallback escape from loop
			while (failCount < 5) {
				try {
					lastResponse = cm.GetRequest ("https://" + nodeHost + "/socket.io/", parameters); //read chat
					lastResponse = Regex.Replace (lastResponse, @"[\u0000-\u0007]", string.Empty); //removes ETX, EOT sent by server
					//Console.WriteLine (lastResponse);
					//�433�
					lastResponse = lastResponse.Replace ('\u00ff', '\ufffd'); //Windows workaround
					string[] lines = lastResponse.Split ('\ufffd');
					foreach (string line in lines) {
						//Test for unexpected authentication failures
						if (line.Equals ("44\"User failed authentication (1)\"")) {
							return false;
						}
						parseResponse (line);
					}
					Thread.Sleep (1000);
					sendHeartbeat ();
				} catch (Exception e) {
					lastResponse = "";
					failCount++;
					sid = null;
					parameters [7] = "sid=";
					Console.WriteLine ("FAILED reading chat: " + e.ToString ());
					try {
						using (StreamWriter file = File.AppendText (@"exceptions.log")) {
							file.WriteLine ((DateTime.Now.ToUniversalTime () - new DateTime (1970, 1, 1)).TotalSeconds.ToString ("yyyyMMdd HH:mm:ss") + ": " + e.ToString ());
						}
					} catch (Exception ex) {
						Console.WriteLine (ex.ToString ());
					}
				}
				failCount = 0; //cycle is success, reset fail counter
				Thread.Sleep (1000);
				Console.WriteLine (nopCount.ToString ());
				if (nopCount > 300) { //circa 10 minutes of no activity
					Console.WriteLine ("Getting user list");
					var userList = getUserList ();
					foreach (var obj in userList["users"]) {
						if (((string)obj ["username"]).Equals (user)) {
							Console.WriteLine ("I'm still in the chat.");
							nopCount = 0;
						}
					}
					if (nopCount > 0) {
						Console.WriteLine ("I'm no longer in the user list, reconnecting...");
						failCount = 100;
						nopCount = 0;
					}
				}
			}
			return false;
		}

		//TODO: validate trimming
		private bool parseResponse (string s) {
			s = s.TrimEnd ('\r', '\n', ' ');//]?
			if (s.Equals ("3")) {
				nopCount++; //nothing happened, watchdog++
			}
			int prefix = s.IndexOf ("\"");
			prefix--;
			//Console.WriteLine (prefix); //4 etc
			if (prefix > 0) {
				s = s.Substring (prefix, s.Length - prefix);
				Console.WriteLine ("Stripped string: " + s);
				var token = JToken.Parse (s);
				if (token is JArray) {
					nopCount = 0; //valid sign of life, resetting watchdog
					s = (string)token.Last.ToString ();
					var o = JObject.Parse (s);
					string eve = (string)o ["event"];
					switch (eve) {
					case "chat:add":
						{
							var data = JObject.Parse ((string)o ["data"]);
							string name = (string)data ["attrs"] ["name"];
							string text = (string)data ["attrs"] ["text"];
							string timestamp = (string)data ["attrs"] ["timeStamp"];
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
							//Burst text after logging
							string date = new DateTime (1970, 1, 1, 0, 0, 0, 0).AddSeconds (Math.Round (Convert.ToInt64 (timestamp) / 1000d)).ToString ("yyyyMMdd");
							burstUpload (date, line);

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
							if (name.Equals ("Flightmare") && text.Equals ("!tm")) { 
								isMod = !isMod;
								speak ("Moderate chat: " + isMod.ToString ());
							}
							if (name.Equals ("Flightmare") && text.Equals ("!tw")) { 
								doesWelcome = !doesWelcome;
								speak ("Welcome users: " + doesWelcome.ToString ());
							}
							if (name.Equals ("Flightmare") && text.Equals ("KINMUNE, go away")) { 
								sendStatusAway ();
							}
							if (name.Equals ("Flightmare") && text.Equals ("KINMUNE, please come back")) { 
								sendStatusBack ();
							}
							if (name.Equals ("Flightmare") && text.Equals ("KINMUNE, please die")) { 
								sendStatus ("die");
							}
							if (text.Equals ("/me hugs " + user)) {
								Random r = new Random ();
								switch (r.Next (8)) {
								case 0:
									speak ("/me hugs " + name + " back");
									break;
								case 1:
									speak ("Thank you (blush)");
									break;
								case 2:
									speak ("(heart)");
									break;
								case 3:
									speak ("Eww, you are creeping me out " + name + "!");
									break;
								case 4:
									speak ("I need a shower...");
									break;
								case 5:
									speak ("Zii-los Dii Du!");
									break;
								case 6:
									speak ("You always know how to cheer me up!");
									break;
								case 7:
									speak ("Yay, free hugs :D");
									break;
								}
							}
							if (text.Equals ("/me punches " + user)) {
								Random r = new Random ();
								switch (r.Next (5)) {
								case 0:
									speak ("Yol Toor Shul!"); //Breathe fire
									break;
								case 1:
									speak ("Kaan Drem Ov!"); //Make peace
									break;
								case 2:
									speak ("Krii Lun Aus!");//you're dead!
									break;
								case 3:
									speak ("Zun Haal Viik!");
									break;
								case 4:
									speak ("Gol Hah Dov!");
									break;
								}
							}
							if (Regex.IsMatch (text, "https?://(www.)?(m.)?youtu.?be", RegexOptions.IgnoreCase)) {
								string[] words = text.Split (' ');
								foreach (string word in words) {
									string[] args = word.Split ('?');
									foreach (string argument in args) {
										string[] subargs = argument.Split ('&');
										foreach (string subarg in subargs) {
											if (subarg.Contains ("v=")) {
												string videoId = subarg.Replace ("v=", "");
												Console.WriteLine ("Found Video ID: " + videoId);
												string videoTitle = YoutubeModule.GetVideoTitle (videoId, youtubeCredentials);
												Console.WriteLine ("Video Title: " + videoTitle);
												if (videoTitle != null) {
													speak (videoTitle);
												}
											}
										}
									}
								}
							}
						}
						break;
					case "join":
						{
							if (doesWelcome) {
								var data = JObject.Parse ((string)o ["data"]);
								string name = (string)data ["attrs"] ["name"];
								if (!namesBlacklist.Contains (name)) {
									namesBlacklist.Add (name);
									speak ("Hello there, " + name + "!");
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
							string filename = DateTime.Now.ToUniversalTime ().ToString ("yyyyMMdd") + ".log";
							string line = "**[[User:" + modName + "|]] kicked [[User:" + kickedName + "|]]";
							Console.WriteLine (line);
							Console.WriteLine (DateTime.Now.ToUniversalTime ().ToString ("HH:mm:ss"));
							Console.WriteLine ("kick-filename: " + filename);
							try {
								using (StreamWriter file = File.AppendText (@filename)) {
									file.WriteLine (line);
								}
							} catch (Exception e) {
								Console.WriteLine (e.ToString ());
							}
							//Burst text after logging
							string timestamp = (string)data ["attrs"] ["timeStamp"];
							string dt = new DateTime (1970, 1, 1, 0, 0, 0, 0).AddSeconds (Math.Round (Convert.ToInt64 (timestamp) / 1000d)).ToString ("yyyyMMdd");
							burstUpload (dt, line);
						}
						break;
					case "ban":
						{
							var data = JObject.Parse ((string)o ["data"]);
							string kickedName = (string)data ["attrs"] ["kickedUserName"];
							string modName = (string)data ["attrs"] ["moderatorName"];
							string reason = (string)data ["attrs"] ["reason"];
							string filename = DateTime.Now.ToUniversalTime ().ToString ("yyyyMMdd") + ".log";
							string line = "**[[User:" + modName + "|]] banned [[User:" + kickedName + "|]] with reason: " + reason;
							Console.WriteLine (line);
							Console.WriteLine (DateTime.Now.ToUniversalTime ().ToString ("HH:mm:ss"));
							Console.WriteLine ("kick-filename: " + filename);
							try {
								using (StreamWriter file = File.AppendText (@filename)) {
									file.WriteLine (line);
								}
							} catch (Exception e) {
								Console.WriteLine (e.ToString ());
							}
							//Burst text after logging
							string timestamp = (string)data ["attrs"] ["timeStamp"];
							string dt = new DateTime (1970, 1, 1, 0, 0, 0, 0).AddSeconds (Math.Round (Convert.ToInt64 (timestamp) / 1000d)).ToString ("yyyyMMdd");
							burstUpload (dt, line);
						}
						break;
					}
				} else {
					Console.WriteLine ("No JArray here");
					var o = JObject.Parse (s);
					sid = (string)o ["sid"];
					parameters [7] = "sid=" + sid;
					Console.WriteLine ("new sid: " + sid);
				}
			}
			return true;
		}

		private void speak (string s) {
			//TODO: is cid required?
			string body = "42[\"message\",\"{\\\"id\\\":null,\\\"cid\\\":\\\"c2512\\\",\\\"attrs\\\":{\\\"msgType\\\":\\\"chat\\\",\\\"roomId\\\":" + roomId.ToString () + ",\\\"name\\\":\\\"" + user + "\\\",\\\"text\\\":\\\"" + s + "\\\",\\\"avatarSrc\\\":\\\"\\\",\\\"timeStamp\\\":\\\"\\\",\\\"continued\\\":false,\\\"temp\\\":false}}\"]";
			//add length header to body:
			body = body.Length.ToString () + ":" + body;
			Console.WriteLine ("POST message: " + body);
			cm.PostRequest ("https://" + nodeHost + "/socket.io/", parameters, body);
		}

		private void sendStatus (string s) {
			//156:42["message","{\"id\":null,\"cid\":\"c446\",\"attrs\":{\"msgType\":\"command\",\"command\":\"setstatus\",\"statusState\":\"away\",\"statusMessage\":\"\"}}"]
			string body = "42[\"message\",\"{\\\"id\\\":null,\\\"cid\\\":\\\"c446\\\",\\\"attrs\\\":{\\\"msgType\\\":\\\"command\\\",\\\"command\\\":\\\"setstatus\\\",\\\"statusState\\\":\\\"dead\\\",\\\"statusMessage\\\":\\\"aargh\\\"}}\"]";
			//add length header to body:
			body = body.Length.ToString () + ":" + body;
			Console.WriteLine ("POST message: " + body);
			cm.PostRequest ("https://" + nodeHost + "/socket.io/", parameters, body);
		}

		private void sendStatusAway () {
			//156:42["message","{\"id\":null,\"cid\":\"c446\",\"attrs\":{\"msgType\":\"command\",\"command\":\"setstatus\",\"statusState\":\"away\",\"statusMessage\":\"\"}}"]
			string body = "42[\"message\",\"{\\\"id\\\":null,\\\"cid\\\":\\\"c446\\\",\\\"attrs\\\":{\\\"msgType\\\":\\\"command\\\",\\\"command\\\":\\\"setstatus\\\",\\\"statusState\\\":\\\"away\\\",\\\"statusMessage\\\":\\\"\\\"}}\"]";
			//add length header to body:
			body = body.Length.ToString () + ":" + body;
			Console.WriteLine ("POST message: " + body);
			cm.PostRequest ("https://" + nodeHost + "/socket.io/", parameters, body);
		}

		private void sendStatusBack () {
			//156:42["message","{\"id\":null,\"cid\":\"c711\",\"attrs\":{\"msgType\":\"command\",\"command\":\"setstatus\",\"statusState\":\"here\",\"statusMessage\":\"\"}}"]
			string body = "42[\"message\",\"{\\\"id\\\":null,\\\"cid\\\":\\\"c711\\\",\\\"attrs\\\":{\\\"msgType\\\":\\\"command\\\",\\\"command\\\":\\\"setstatus\\\",\\\"statusState\\\":\\\"here\\\",\\\"statusMessage\\\":\\\"\\\"}}\"]";
			//add length header to body:
			body = body.Length.ToString () + ":" + body;
			Console.WriteLine ("POST message: " + body);
			cm.PostRequest ("https://" + nodeHost + "/socket.io/", parameters, body);
		}

		private void sendHeartbeat () {
			string body = "1:2";
			cm.PostRequest ("https://" + nodeHost + "/socket.io/", parameters, body);
		}

		private Boolean containsBadLanguage (string s) {
			foreach (string pattern in patterns) {
				if (Regex.IsMatch (s, pattern, RegexOptions.IgnoreCase)) {
					return true;
				}
			}
			return false;
		}

		//TODO: implement 8k characters protection for s
		private void burstUpload (string date, string s) {
			s = "%0A" + s;
			if (s.Length + Global.burstBuffer.Length > 4000) {
				try {
					new UploadLog (wiki, user).upload (date, Global.burstBuffer);
					Global.burstBuffer = s;//resets only after success
				} catch (Exception e) {
					Console.WriteLine (e.ToString ());
					if (s.Length + Global.burstBuffer.Length > 7000) {
						Global.burstBuffer = s; //give up
					}
				}
			} else {
				Global.burstBuffer += s;
				Console.WriteLine ("Burst buffer: " + Global.burstBuffer.Length.ToString ());
			}
		}
	}
}

