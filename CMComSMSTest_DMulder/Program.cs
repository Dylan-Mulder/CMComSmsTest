using System.Text.Json;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Text;
using CMComSMSTest_DMulder;
using System.Numerics;

class Program
{
    static async Task Main(string[] args)
    {
        while (true)
        {
            var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            //Football-API
            string footballApiKey = config["Settings:FOOTBALL_API_KEY"];
            string footballApiUrl = config["Settings:FOOTBALL_API_URL"];
            int leagueId = int.Parse(config["Settings:FOOTBALL_LEAGUE_ID"]);
            string todayDate = DateTime.UtcNow.ToString("yyyy-MM-dd");

            //CM.com
            string cmApiUrl = config["Settings:CM_API_URL"];
            string productToken = config["Settings:PRODUCT_TOKEN"];
            string recipientPhoneNr = config["Settings:PHONE_NR"];

            //Get Results
            List<string> matchResults = await getFootballResults(footballApiUrl, footballApiKey, leagueId, todayDate);
            if (matchResults != null)
            {   
                using (HttpClient client = new HttpClient())
                {
                    //Build JSON
                    SmsService smsService = new SmsService(client);
                    string messageContent = string.Join(", ", matchResults);
                    string smsJsonRequest = smsService.buildSMSJsonRequest(productToken, recipientPhoneNr, messageContent, 2);

                    //Send SMS
                    Console.WriteLine("Sending SMS!  " + smsJsonRequest);
                    await smsService.sendSMS(smsJsonRequest, cmApiUrl);
                }
            }
            else
            {
                Console.WriteLine("No match results available for the specified date. No SMS today.");
            }

            //24 uur delay
            await Task.Delay(TimeSpan.FromHours(24));
        }

    }
    static async Task<List<string>> getFootballResults(string apiHost, string apiKey, int leagueId, string date)
    {
        using (HttpClient client = new HttpClient())
        {
            string year = date.Substring(0,4);
            string apiUrl = $"https://{apiHost}/fixtures?league={leagueId}&season={year}&date={date}&timezone=Europe/London";

            client.DefaultRequestHeaders.Add("x-rapidapi-key", apiKey);
            client.DefaultRequestHeaders.Add("x-rapidapi-host", apiHost);
            HttpResponseMessage response = await client.GetAsync(apiUrl);

            if (response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                var results = JsonSerializer.Deserialize<MatchResults>(responseContent);

                //Als er geen matches zijn vandaag return null
                if (results?.response != null && results.response.Count > 0)
                {
                    return results.getScores();
                }
                else
                {
                    Console.WriteLine("No match results available for the specified date.");
                    return null;
                }

            }
            else
            {
                throw new HttpRequestException($"Failed to get match results. Status Code: {response.StatusCode}");
            }
        }
    }

    //Supporting classes voor JSON Deserialization
    class MatchResults
    {
        public List<Result> response { get; set; }
        public List<string> getScores()
        {
            List<string> scores = new List<string>();
            foreach(var result in response)
            {
                string scoreFixture = $"{result.teams?.home?.name} {result.goals?.home} - {result.teams?.away?.name} {result.goals?.away}";
                scores.Add(scoreFixture);
            }
            return scores;
        }
    }

    class Result
    {
        public Teams? teams { get; set; }
        public Goals? goals { get; set; }
    }

    class Teams
    {
        public TeamInfo? home { get; set; }
        public TeamInfo? away { get; set; }
    }

    class TeamInfo
    {
        public string? name { get; set; }
    }

    class Goals
    {
        public int? home { get; set; }
        public int? away { get; set; }
    }
}
