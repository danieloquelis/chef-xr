using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Groq.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Groq.Utils
{
    public static class HttpUtils
    {
        public static HttpRequestMessage BuildPostRequest<T>(string uri, T body)
        {
            var json = JsonConvert.SerializeObject(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            return new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = content
            };
        }

        public static async Task<HttpResponseMessage> SendRequestAsync(HttpClient client, HttpRequestMessage request)
        {
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"HTTP request failed: {response.StatusCode} - {errorText}");
            }

            return response;
        }

        public static T DeserializeEmbeddedContent<T>(string json)
        {
            var response = JsonConvert.DeserializeObject<ChatCompletionResponse>(json);
            var content = response?.Choices?[0]?.Message?.Content;

            if (string.IsNullOrEmpty(content))
                throw new JsonException("No content found in response.");

            var cleanedJson = StripJsonFromMarkdown(content);
            return JsonConvert.DeserializeObject<T>(cleanedJson);
        }
        
        private static string StripJsonFromMarkdown(string input)
        {
            input = input.Trim();

            // Remove ```json or ``` if present
            if (!input.StartsWith("```")) return input.Trim();
            var firstNewline = input.IndexOf('\n');
            if (firstNewline >= 0)
                input = input.Substring(firstNewline + 1);

            var lastBackticks = input.LastIndexOf("```", StringComparison.Ordinal);
            if (lastBackticks >= 0)
                input = input.Substring(0, lastBackticks);

            return input.Trim();
        }

    }
}
