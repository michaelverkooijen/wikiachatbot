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

namespace WikiaBot {
    public class ChatModule {
        private string wiki, user, pass;
        ConnectionManager cm;
        public ChatModule(string wiki, string user, string pass) {
            this.wiki = wiki;
            this.user = user;
            this.pass = pass;
            cm = new ConnectionManager();
        }

        //TODO: mid-session logins
        public bool start(){
            //logging in:
            try {
                if (!cm.Login(wiki, user, pass)){
                    return false;
                }
            } catch (Exception e) {
				Console.WriteLine(e.ToString());
                return false;
            }
            //get wgchatkey
            string response = cm.GetRequest("http://" + wiki + ".wikia.com/wiki/Special:Chat", new string[]{});
            //Console.WriteLine(response);
            int x = response.IndexOf("wgChatKey");
            //int y = response.IndexOf("roomId");
            string wgChatKey = response.Substring(x + 12, 32);
            Console.WriteLine("wgChatKey: " + wgChatKey);
			Console.WriteLine ("t=" + (DateTime.Now.ToUniversalTime() - new DateTime (1970, 1, 1)).TotalSeconds.ToString());
            //http://chat-1-3.wikia.com/socket.io/1/xhr-polling/?name=KINMUNE&roomId=202958&key=70cc032fb13d88d1065ec496655a9111
            //TODO: unsafe room! This is hardcoded to elderscrolls. Search for roomId first.
            //"roomId":132,"wgChatMod":0,"WIKIA_NODE_HOST":"chat-1-3.wikia.com","WIKIA_NODE_PORT":"80"
            int failCount = 0;
            string xhrKey = null;
            string lastResponse = "";
            //enter poll loop:
            //make fallback escape from loop
            while(failCount < 5){
                if(xhrKey != null && !lastResponse.Equals("")){
                    Console.WriteLine("Reading chat...");
                    try {
						lastResponse = cm.GetRequest("http://chat-3-3.wikia.com/socket.io/1/xhr-polling/" + xhrKey, new string[]{"name=" + user, "roomID=244", "key=" + wgChatKey, "t=" + (DateTime.Now.ToUniversalTime() - new DateTime (1970, 1, 1)).TotalSeconds}); //read chat
                        Console.WriteLine (lastResponse);
						//�433�
						string[] lines = lastResponse.Split('\ufffd');
						foreach (string line in lines){
							parseResponse(line);
						}
						//parseResponse(lastResponse);
                    } catch (Exception e) {
                        lastResponse = "";
                        failCount++;
                        Console.WriteLine("FAILED reading chat: " + e.ToString());
                    }
                } else {
                    Console.WriteLine("Requesting xhr key...");
                    //lastResponse = cm.PostQuery("http://chat-1-3.wikia.com/socket.io/1/xhr-polling/", new string[]{"name=" + user, "roomID=132", "key=" + wgChatKey}); //get key
                    try { 
						lastResponse = cm.GetRequest("http://chat-3-3.wikia.com/socket.io/1/xhr-polling/?name=KINMUNE&roomId=244&key=" + wgChatKey + "&t=" + (DateTime.Now.ToUniversalTime() - new DateTime (1970, 1, 1)).TotalSeconds, new string[] { });
                    } catch (Exception e) {
                        lastResponse = "";
                        failCount++;
                        Console.WriteLine ("FAILED requesting key: " + e.ToString());
                    }
                    x = lastResponse.IndexOf(":");
                    if (x > 0) {
                        xhrKey = lastResponse.Substring(0, x);
                    }
                }
                failCount = 0; //cycle is success, reset fail counter
				Thread.Sleep(1000);
            }
            return false;
        }

        private bool parseResponse(string s){
            int prefix = s.IndexOf("\"");
            prefix--;
            //Console.WriteLine(prefix); //4 etc
            if (prefix > 0) {
                s = s.Substring(prefix, s.Length - prefix);
                //Console.WriteLine(s); full string without 4::
                var o = JObject.Parse(s);
                string eve = (string)o["event"];
                //Console.WriteLine(eve);
                if (eve.Equals("chat:add")){
                    var data = JObject.Parse((string)o["data"]);
                    string name = (string)data["attrs"]["name"];
                    //Console.WriteLine("Name: " + name);
                    string text = (string)data["attrs"]["text"];
                    //Console.WriteLine("Text: " + text);
                    string timestamp = (string)data["attrs"]["timeStamp"];
                    //Console.WriteLine(timestamp);
                    var dt = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(Math.Round(Convert.ToInt64(timestamp) / 1000d)).ToLocalTime();
                    string dts = dt.ToString().Replace("/","-");
                    string line = "*" + dts.Split(' ')[1] + ": [[User:" + name + "|]]: <nowiki>" + text + "</nowiki>";
                    Console.WriteLine(line);
                    string filename = dts.Split(' ')[0] + ".log";
                    Console.WriteLine(filename);
                    try {
                        using(StreamWriter file = File.AppendText(@filename)){
                            file.WriteLine(line);
                        }
                    } catch (Exception e) {
                        Console.WriteLine(e.ToString());
                    }
                }
            }
            return true;
        }
    }
}

