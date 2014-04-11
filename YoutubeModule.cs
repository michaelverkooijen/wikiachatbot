using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace WikiaBot {
	public class YoutubeModule {
		public YoutubeModule () {
		}

		public static string GetVideoTitle(string videoId, string credentials){
			string url = "https://www.googleapis.com/youtube/v3/videos";
			string[] args = { "id=" + videoId, "key=" + credentials, "part=snippet" };
			string result = new ConnectionManager().GetRequest(url, args);
			return getJsonTitle(result);
		}

		private static string getJsonTitle(string s){
			var o = JObject.Parse(s);
			string title = (string)o["items"][0]["snippet"]["title"];
			Console.WriteLine ("Youtube Video Title: " + title);
			return title;
		}
	}
}

