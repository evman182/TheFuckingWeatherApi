using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Http;

namespace TheFuckingWeatherApi.Controllers
{
    public class TfwController : ApiController
    {
        private static readonly ConcurrentDictionary<string, CachedWeather> WeatherCache = new ConcurrentDictionary<string, CachedWeather>();
        private static readonly Regex LocationRegex = new Regex(".*locationDisplay\" class=\"small\">([^<]+).*", RegexOptions.Compiled);
        private static readonly Regex TemperatureRegex = new Regex(".*tempf=\"([0-9]+)\".*id=\"degree\">([^<]+).*", RegexOptions.Compiled);
        private static readonly Regex RemarkRegex = new Regex(".*jsRemark\">([^<]+).*", RegexOptions.Compiled);
        private static readonly List<string> Tokens = ConfigurationManager.AppSettings["Tokens"].Split(';').ToList();

        public async Task<HttpResponseMessage> Get(string text, string token)
        {
            return await ValidateAndProcessWeatherRequest(text, token);
        }

        public async Task<HttpResponseMessage> Post([FromBody]string text, [FromBody]string token)
        {
            return await ValidateAndProcessWeatherRequest(text, token);
        }

        private async Task<HttpResponseMessage> ValidateAndProcessWeatherRequest(string text, string token)
        {
            if (!IsTokenValed(token))
                return Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Bad Token");

            var responseText = await GetWeatherForZip(text);
            return Request.CreateResponse(HttpStatusCode.OK, new ApiResponse {text = responseText});
        }


        private static async Task<string> GetWeatherForZip(string id)
        {
            CachedWeather cachedWeather;
            var isCached = WeatherCache.TryGetValue(id, out cachedWeather);

            if (isCached && IsCachedValueFresh(cachedWeather))
            {
                return cachedWeather.Weather;
            }

            var html = await GetFuckingWeatherHtml(id);
            var parsedWeatherText = GetParsedTextFromHtml(html);

            CacheNewText(id, parsedWeatherText);

            return parsedWeatherText;
        }

        private static void CacheNewText(string id, string finalText)
        {
            var newCachedWeather = new CachedWeather
            {
                CacheDateTime = DateTime.Now,
                Weather = finalText
            };

            WeatherCache.AddOrUpdate(id, newCachedWeather, (key, weather) => newCachedWeather);
        }

        private static string GetParsedTextFromHtml(string html)
        {
            var locationMatch = LocationRegex.Match(html);
            var temperatureMatch = TemperatureRegex.Match(html);
            var remarkMatch = RemarkRegex.Match(html);

            if (!locationMatch.Success || !temperatureMatch.Success || !remarkMatch.Success)
                return "No weather found";

            var location = WebUtility.HtmlDecode(locationMatch.Groups[1].Value);
            var temperature = temperatureMatch.Groups[1];
            var temperatureExclamation = temperatureMatch.Groups[2].Value;
            var remark = WebUtility.HtmlDecode(remarkMatch.Groups[1].Value);

            return location + ": " + temperature + "°" + temperatureExclamation + " " + remark;

        }

        private static async Task<string> GetFuckingWeatherHtml(string id)
        {
            var client = new HttpClient();
            var uri = string.Format("http://thefuckingweather.com/Where/{0}", id);
            var message = new HttpRequestMessage(HttpMethod.Get, uri);
            message.Headers.UserAgent.Add(new ProductInfoHeaderValue("Mozilla", "5.0"));

            var response = await client.SendAsync(message);
            var responseBody = await response.Content.ReadAsStringAsync();
            return responseBody;
        }

        private static bool IsCachedValueFresh(CachedWeather cachedWeather)
        {
            return (DateTime.Now - cachedWeather.CacheDateTime).TotalMinutes < 15;
        }

        private static bool IsTokenValed(string token)
        {
            return Tokens.Contains(token);
        }

        public class ApiResponse
        {
            public string response_type => "in_channel";
            public string text;
        }

        private struct CachedWeather
        {
            public DateTime CacheDateTime;
            public string Weather;
        }

    }
}
