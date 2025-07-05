using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Groq.Models;
using Groq.Rest;
using Groq.Utils;
using UnityEngine;

namespace Groq
{
    public class GroqClient
    {
        private static GroqClient _instance;
        public static GroqClient Instance => _instance ??= new GroqClient();

        private readonly HttpClient m_httpClient;

        private readonly string m_baseUrl;
        private readonly string m_model;
        private readonly float m_temperature;
        private readonly int m_maxCompletionTokens;
        private readonly bool m_stream;
        private readonly bool m_jsonMode;
        
        private const string ChatCompletionsEndpoint = "/chat/completions";
        private const int MaxImageSizeMb = 20;
        private const int MaxBase64SizeMb = 4;

        private GroqClient()
        {
            var config = Resources.Load<GroqConfig>("GroqConfig");

            if (!config || string.IsNullOrWhiteSpace(config.apiKey))
            {
                Debug.LogError("GroqClient: Missing or invalid GroqConfig.");
                return;
            }

            var apiKey = config.apiKey;
            m_baseUrl = config.apiBaseUrl;
            m_model = config.model;
            m_temperature = config.temperature;
            m_maxCompletionTokens = config.maxCompletionTokens;
            m_stream = config.stream;
            m_jsonMode = config.jsonMode;
            
            m_httpClient = new HttpClient();
            m_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            
            Debug.Log("Init done");

        }

        private ChatCompletionRequest BuildChatCompletionRequest(List<Message> messages)
        {
            return new ChatCompletionRequest
            {
                Messages = messages,
                Model = m_model,
                Temperature = m_temperature,
                MaxCompletionTokens = m_maxCompletionTokens,
                Stream = m_stream,
                ResponseFormat = m_jsonMode
                    ? new ResponseFormat
                    {
                        Type = "json_object"
                    }
                    : null
            };
        }
        
        public async Task<T> CreateChatCompletionAsync<T>(List<Message> messages)
        {
            var request = BuildChatCompletionRequest(messages);
            var uri = m_baseUrl + ChatCompletionsEndpoint;

            var httpRequest = HttpUtils.BuildPostRequest(uri, request);
            var httpResponse = await HttpUtils.SendRequestAsync(m_httpClient, httpRequest);
            var response = await httpResponse.Content.ReadAsStringAsync();

            Debug.Log($"Groq response: {response}");

            return HttpUtils.DeserializeEmbeddedContent<T>(response);
        }
        
        private void ValidateBase64Size(string base64String)
        {
            var sizeInMb = (base64String.Length * 3.0 / 4.0) / (1024 * 1024);
            if (sizeInMb > MaxBase64SizeMb)
                throw new ArgumentException($"Base64 encoded image exceeds maximum size of {MaxBase64SizeMb}MB");
        }
        
        public void Dispose()
        {
            m_httpClient.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
