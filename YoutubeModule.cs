using System;
using Newtonsoft.Json.Linq;

namespace WikiaBot {
	public class YoutubeModule {
		public YoutubeModule () {
		}

		public static string GetVideoTitle(string videoId, string credentials){
			string url = "https://www.googleapis.com/youtube/v3/videos";
			string[] args = { "id=" + videoId, "key=" + credentials, "part=snippet" };
			Console.WriteLine ("Opening YouTube connection");
			string result = new ConnectionManager().GetRequest(url, args);
			Console.WriteLine ("YouTube connection opened: result = " + result);
			return getJsonTitle(result);
		}

		private static string getJsonTitle(string s){
			var o = JObject.Parse(s);
			if ((int)o ["pageInfo"] ["totalResults"] == 0) {
				Console.WriteLine ("Not a valid video");
				return null;
			}
			string title = (string)o["items"][0]["snippet"]["title"];
			Console.WriteLine ("Youtube Video Title: " + title);
			return title;
		}
	}
}

