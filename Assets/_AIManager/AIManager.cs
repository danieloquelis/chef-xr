using System.Collections.Generic;
using System.Threading.Tasks;
using AI.Models;
using Groq;
using Groq.Models;

namespace AI
{
    public class AIManager
    {
        private static AIManager _instance;
        public static AIManager Instance => _instance ??= new AIManager();

        private readonly GroqClient m_groqClient;
        
        private AIManager()
        {
            m_groqClient = GroqClient.Instance;    
        }

        public async Task<KitchenScannerResult> IdentifyKitchen(string imageBase64)
        {
            var userContentBlock = new List<ContentBlock>
            {
                new()
                {
                    Type = "text",
                    Text = PromptUtils.Load(PromptUtils.PromptType.KitchenScannerUser)
                },
                new()
                {
                    Type = "image_url",
                    ImageUrl = new ImageUrl
                    {
                        Url = $"data:image/jpeg;base64,{imageBase64}"
                    }
                }
            };

            var messages = new List<Message>
            {
                new()
                {
                    Role = "system",
                    Content = PromptUtils.Load(PromptUtils.PromptType.KitchenScannerSystem)
                },
                new()
                {
                    Role = "user",
                    Content = userContentBlock
                }
            };


            return await m_groqClient.CreateChatCompletionAsync<KitchenScannerResult>(messages);
        }
    }
}