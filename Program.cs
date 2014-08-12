using System;
using System.Threading;

namespace WikiaBot {
    class MainClass {
        public static void Main(string[] args) {
			String wiki, user, pass, youtubeCredentials;
			Boolean isMod = false, doesWelcome = false;
			foreach (var item in args) {
				if (item.Equals("-mod")) {
					isMod = true;
				}
				if (item.Equals ("-welcome")) {
					doesWelcome = true;
				}
			}
			if (isMod) {
				Console.WriteLine ("Mod flag detected.");
			}
			if (doesWelcome) {
				Console.WriteLine ("Welcome flag detected.");
			}
            Console.WriteLine("Wikia subdomain: ");
            wiki = Console.ReadLine();
            Console.WriteLine("Username: ");
            user = Console.ReadLine();
            Console.WriteLine("Password: ");
            pass = Console.ReadLine();
			Console.WriteLine("Youtube API Key (optional): ");
			youtubeCredentials = Console.ReadLine();
			ChatModule chat = new ChatModule(wiki, user, pass, youtubeCredentials, isMod, doesWelcome);
            while(true){
                //chat.start should block, if it doesn't something is wrong. ritual starts again from scratch
                chat.start();
                Thread.Sleep(60000); //wait a minute to make sure Wikia kills the session.
            }
        }
    }
}
