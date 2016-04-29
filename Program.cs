using System;
using System.Threading;
using Newtonsoft.Json.Linq;
using System.IO;

namespace WikiaBot {
    class MainClass {
        public static void Main(string[] args) {
			//String wiki, user, pass, youtubeCredentials;
			Boolean isMod = false, doesWelcome = false, upload = false;
			JObject settings = JObject.Parse(File.ReadAllText(@"settings.json"));
			JObject login = JObject.Parse(File.ReadAllText(@"login.json"));
			isMod = (bool)settings["isMod"];
			doesWelcome = (bool)settings["doesWelcome"];
			//enable arg overrides
			foreach (var item in args) {
				if (item.Equals("-mod")) {
					isMod = true;
				}
				if (item.Equals ("-welcome")) {
					doesWelcome = true;
				}
				if (item.Equals ("-upload")) {
					upload = true;
					//TODO: date
				}
			}
			if (isMod) {
				Console.WriteLine ("Mod flag detected.");
			}
			if (doesWelcome) {
				Console.WriteLine ("Welcome flag detected.");
			}
			if (upload) {
				Console.WriteLine ("Upload flag detected");
			}
				
			ChatModule chat = new ChatModule((string)login["wiki"], (string)login["username"], (string)login["password"], (string)login["youtubekey"], isMod, doesWelcome);
			while(true){
				//chat.start should block, if it doesn't something is wrong. ritual starts again from scratch
				chat.start();
				Thread.Sleep(60000); //wait a minute to make sure Wikia kills the session.
			}
        }
    }

	//Static string for reconnect persistence TODO: rewrite in something more OO
	public static class Global {
		public static string burstBuffer = "";
	}
}
