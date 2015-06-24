using System;
using System.Net;

namespace WikiaBot {
    public class CookieClient : WebClient {
        public CookieContainer CookieContainer { get; set; }

        public Uri Uri { get; set; }

        public CookieClient() : this(new CookieContainer()) {
        }

        public CookieClient(CookieContainer cookies) {
            this.CookieContainer = cookies;
        }

        protected override WebRequest GetWebRequest(Uri address) {
            WebRequest request = base.GetWebRequest(address);
            if (request is HttpWebRequest) {
                (request as HttpWebRequest).CookieContainer = this.CookieContainer;
            }
            HttpWebRequest httpRequest = (HttpWebRequest)request;
            httpRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            return httpRequest;
        }

        protected override WebResponse GetWebResponse(WebRequest request) {
            WebResponse response = base.GetWebResponse(request);
            String setCookieHeader = response.Headers[HttpResponseHeader.SetCookie];

            if (setCookieHeader != null) {
                //do something if needed to parse out the cookie.
                if (setCookieHeader != null) {
                    Cookie cookie = new Cookie(); //create cookie
                    cookie.Domain = "wikia.com";
                    cookie.Name = "wikicities";
                    this.CookieContainer.Add(cookie);
                }
            }
            return response;
        }

        public static string GetCookieDomain(string uri) {
            Uri req_uri = new Uri(uri);
            return req_uri.GetComponents(UriComponents.Host, UriFormat.Unescaped);
        }
    }
}

