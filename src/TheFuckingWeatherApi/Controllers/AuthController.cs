using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TheFuckingWeatherApi.Controllers
{
    public class AuthController : Controller
    {
        private static readonly string ClientId = ConfigurationManager.AppSettings["ClientId"];
        private static readonly string ClientSecret = ConfigurationManager.AppSettings["ClientSecret"];
        private static readonly KeyValuePair<string, string> ClientIdKeyValuePair = new KeyValuePair<string, string>("client_id", ClientId);
        private static readonly KeyValuePair<string, string> ClientSecretKeyValuePair = new KeyValuePair<string, string>("client_secret", ClientSecret);
        
        public async Task<ActionResult> Authorize(string code, string state)
        {
            List<KeyValuePair<string, string>> formValues = new List<KeyValuePair<string, string>>
            {
                ClientIdKeyValuePair,
                ClientSecretKeyValuePair,
                new KeyValuePair<string, string>("code", code)
            };


            var client = new HttpClient();
            var message = new HttpRequestMessage(HttpMethod.Post, "https://slack.com/api/oauth.access");
            message.Content = new FormUrlEncodedContent(formValues);

            var result = await client.SendAsync(message);

            /*
            var resultContent = result.Content.ReadAsStringAsync();
            var resultJson = JObject.Parse(await resultContent);
            try
            {
                
                return Redirect(redirectUrl);
            }            
            catch (Exception ex)
            {
                return Content(ex.ToString().Replace(Environment.NewLine, "<br />") + "<br />" + resultJson);
            }
            */

            return Redirect("http://google.com");

        }
    }
}
