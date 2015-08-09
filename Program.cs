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
            /*Console.WriteLine("Wikia subdomain: ");
            wiki = Console.ReadLine();
            Console.WriteLine("Username: ");
            user = Console.ReadLine();
            Console.WriteLine("Password: ");
            pass = Console.ReadLine();
			Console.WriteLine("Youtube API Key (optional): ");
			youtubeCredentials = Console.ReadLine();*/

			if (upload) {
				new UploadLog ((string)login["wiki"], (string)login["username"], (string)login["password"]).upload ("20140731");
			} else {
				//ChatModule chat = new ChatModule(wiki, user, pass, youtubeCredentials, isMod, doesWelcome);
				ChatModule chat = new ChatModule((string)login["wiki"], (string)login["username"], (string)login["password"], (string)login["youtubekey"], isMod, doesWelcome);
				while(true){
					//chat.start should block, if it doesn't something is wrong. ritual starts again from scratch
					chat.start();
					Thread.Sleep(60000); //wait a minute to make sure Wikia kills the session.
				}
			}
        }
    }
}
