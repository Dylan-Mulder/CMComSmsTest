using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMComSMSTest_DMulder
{
    public class SmsService
    {
        private readonly HttpClient _httpClient;
        public SmsService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task sendSMS(string jsonRequest, string cmApiUrl)
        {
            using (HttpClient client = _httpClient)
            {
                StringContent content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync(cmApiUrl, content);
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("SMS Sent Successfully.");
                    Console.WriteLine($"Response Content: {await response.Content.ReadAsStringAsync()}");
                }
                else
                {
                    Console.WriteLine($"Error sending SMS. Status Code: {response.StatusCode}");
                    Console.WriteLine($"Response Content: {await response.Content.ReadAsStringAsync()}");
                }
            }
        }

        public string buildSMSJsonRequest(string productToken, string recipientPhoneNr, string messageContent, int maxParts)
        {
            return $@"
        {{
                ""messages"": {{
                        ""authentication"": {{
                                ""producttoken"": ""{productToken}""
                        }},
                ""msg"": [ {{
                                ""allowedChannels"":  [""SMS""],
                                ""from"": ""Dylan"",
                                ""to"": [{{
                                        ""number"": ""{recipientPhoneNr}""
                        }}],
                        ""minimumNumberOfMessageParts"": 1,
                        ""maximumNumberOfMessageParts"": ""{maxParts}"",
                        ""body"": {{
                            ""type"": ""auto"",
                            ""content"": ""{messageContent}""
                        }}
                    }}
                ]
            }}
        }}";
        }
    }

}
