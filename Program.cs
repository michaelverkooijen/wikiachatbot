using System;
using System.Threading;

namespace WikiaBot {
    class MainClass {
        public static void Main(string[] args) {
            String wiki, user, pass;
            Console.WriteLine("Wikia subdomain: ");
            wiki = Console.ReadLine();
            Console.WriteLine("Username: ");
            user = Console.ReadLine();
            Console.WriteLine("Password: ");
            pass = Console.ReadLine();
            ChatModule chat = new ChatModule(wiki, user, pass);
            while(true){
                //chat.start should block, if it doesn't something is wrong. ritual starts again from scratch
                chat.start();
                Thread.Sleep(60000); //wait a minute to make sure Wikia kills the session.
            }
        }
    }
}
