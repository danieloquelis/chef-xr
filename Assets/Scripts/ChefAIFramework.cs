using System;
using System.Collections;
using System.Collections.Generic;
//using System.Text.Json;
//using System.Text.Json.Nodes;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.IO;
using System.Text;
//using GroqApiLibrary;
using Newtonsoft.Json;
using OVRSimpleJSON;
using Newtonsoft.Json.Linq;

namespace ChefMR.AI
{
    /// <summary>
    /// Unified AI Framework for Chef MR App
    /// Provides modular interface for vision, voice, and structured AI responses
    /// </summary>
    public class ChefAIFramework : MonoBehaviour
    {
        [Header("API Configuration")]
        [SerializeField] private string groqApiKey = "<key-redacted>"; //Hema's API key
        [SerializeField] private string visionModel = "meta-llama/llama-4-scout-17b-16e-instruct";
        [SerializeField] private string textModel = "mixtral-8x7b-32768";
        [SerializeField] private string whisperModel = "whisper-large-v3";

        [Header("Voice Recording Settings")]
        [SerializeField] private int maxRecordingDuration = 30;
        [SerializeField] private int sampleRate = 44100;
        [SerializeField] private float voiceActivityThreshold = 0.02f;

        [Header("Debug Settings")]
        [SerializeField] private bool enableDebugLogs = true;

        // API Endpoints
        private const string GROQ_CHAT_ENDPOINT = "https://api.groq.com/openai/v1/chat/completions";
        private const string GROQ_VISION_ENDPOINT = "https://api.groq.com/openai/v1/chat/completions";
        private const string GROQ_WHISPER_ENDPOINT = "https://api.groq.com/openai/v1/audio/transcriptions";

        // Components
        //private GroqApiClient groqApi;
        private string micDevice;
        private AudioClip currentRecording;
        private bool isRecording = false;
        private Coroutine voiceActivityCoroutine;

        //Retry logic variables
        private Texture2D lastImage;
        private string lastPrompt;
        private string lastSchema;
        private Action<JObject> lastCallback;
        private string lastValidationkey;
        private int maxRetries;

        // Singleton
        public static ChefAIFramework Instance { get; private set; }

        // Events for team modules
        public event System.Action<List<Ingredient>> OnIngredientsDetected;
        public event System.Action<List<Appliance>> OnAppliancesDetected;
        public event System.Action<string> OnVoiceTranscribed;
        public event System.Action<TimerCommand> OnTimerCommandReceived;
        public event System.Action<Recipe> OnRecipeGenerated;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeFramework();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeFramework()
        {
            //Load api keys securely
            LoadGroqKeyFromJson();

            // Initialize microphone
            InitializeMicrophone();

            DebugLog("Chef MR AI Framework initialized successfully");
        }

        private void LoadGroqKeyFromJson()
        {
            string path = Application.dataPath + "/Config/api_keys.json";
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                JObject jsonObj = JObject.Parse(json);
                groqApiKey = jsonObj["groq_api_key"]?.ToString();
                DebugLog("✅ Groq key loaded from local config.");
            }
            else
            {
                Debug.LogWarning("⚠️ Groq API key config file not found.");
            }
        }


        private void InitializeMicrophone()
        {
            if (Microphone.devices.Length > 0)
            {
                micDevice = Microphone.devices[0];
                DebugLog($"Microphone initialized: {micDevice}");
            }
            else
            {
                Debug.LogWarning("No microphone detected!");
            }
        }

        #region Core Vision Methods

        /// <summary>
        /// Generic vision analysis with custom prompt
        /// </summary>
        /// <param name="texture">Image to analyze</param>
        /// <param name="prompt">Custom analysis prompt</param>
        /// <param name="callback">Callback with raw response</param>
        public void AnalyzeImage(Texture2D texture, string prompt, Action<string> callback)
        {
            StartCoroutine(AnalyzeImageCoroutine(texture, prompt, callback));
        }

        /// <summary>
        /// Vision analysis with structured JSON response
        /// </summary>
        /// <param name="texture">Image to analyze</param>
        /// <param name="prompt">Analysis prompt</param>
        /// <param name="jsonSchema">Expected JSON schema</param>
        /// <param name="callback">Callback with parsed JSON</param>
        public void AnalyzeImageWithJSON(Texture2D texture, string prompt, string jsonSchema, string validationKey, Action<JObject> callback)
        {
            // Save for possible retry
            lastImage = texture;
            lastPrompt = prompt;
            lastSchema = jsonSchema;
            lastCallback = callback;
            lastValidationkey = validationKey;

            string 
            enhancedPrompt = $"{prompt}";
            //enhancedPrompt = $"{prompt}\n\nRespond ONLY with valid JSON in this exact format:\n{jsonSchema}\n\nDo not include any text outside the JSON structure.";

            //StartCoroutine(AnalyzeImageForJSONCoroutine(texture, enhancedPrompt, callback));

            StartCoroutine(AnalyzeImageForJSONCoroutine(texture, prompt, response =>
            {
                TryParseWithRetry(response, maxRetries, callback, validationKey); // Retry up to 1 time
            }));

        }

        private IEnumerator AnalyzeImageCoroutine(Texture2D texture, string prompt, Action<string> callback)
        {
            //byte[] imageBytes = texture.EncodeToJPG();
            Texture2D safeTex = MakeTextureReadable(texture);
            byte[] imageBytes = safeTex.EncodeToJPG();
            string base64Image = Convert.ToBase64String(imageBytes);

            var requestData = new
            {
                model = visionModel,
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new { type = "text", text = prompt },
                            new { type = "image_url", image_url = new { url = $"data:image/jpeg;base64,{base64Image}" } }
                        }
                    }
                },
                max_tokens = 1000
            };

            yield return StartCoroutine(SendGroqRequest(GROQ_VISION_ENDPOINT, requestData, (response) =>
            {
                if (response != null)
                {
                    try
                    {
                        JObject jsonResponse = JObject.Parse(response);
                        string content = jsonResponse["choices"]?[0]?["message"]?["content"]?.ToString();
                        callback?.Invoke(content);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Vision API response parsing error: {e.Message}");
                        callback?.Invoke($"Error: {e.Message}");
                    }
                }
                else
                {
                    callback?.Invoke("Error: No response received");
                }
            }));
        }

        public static Texture2D MakeTextureReadable(Texture texture)
        {
            RenderTexture rt = RenderTexture.GetTemporary(texture.width, texture.height);
            Graphics.Blit(texture, rt);
            RenderTexture.active = rt;

            Texture2D readableTex = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);
            readableTex.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            readableTex.Apply();

            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);
            return readableTex;
        }

        private IEnumerator AnalyzeImageForJSONCoroutine(Texture2D texture, string prompt, Action<string> callback)
        {
            yield return StartCoroutine(AnalyzeImageCoroutine(texture, prompt, (response) =>
            {
                if (string.IsNullOrEmpty(response) || response.StartsWith("Error:"))
                {
                    callback?.Invoke(null);
                    return;
                }

                try
                {
                    callback?.Invoke(response);
                }
                catch (Exception e)
                {
                    Debug.LogError($"JSON parsing error: {e.Message}");
                    Debug.LogError($"Raw response: {response}");
                    callback?.Invoke(null);
                }
            }));
        }

        private void TryParseWithRetry(string response, int retriesLeft, Action<JObject> callback, string validationKey)
        {
            try
            {
                JObject json = JObject.Parse(response);

                // Check if the key exists and is a non-empty array
                if (json[validationKey] is JArray itemArray && itemArray.Count == 0)
                {
                    Debug.LogWarning($"⚠️ JSON is valid but '{validationKey}' array is empty. Retrying...");

                    if (retriesLeft > 0)
                    {
                        StartCoroutine(AnalyzeImageForJSONCoroutine(lastImage, lastPrompt, retryResponse =>
                        {
                            TryParseWithRetry(retryResponse, retriesLeft - 1, lastCallback, validationKey);
                        }));
                        return;
                    }
                    else
                    {
                        Debug.LogError($"🚫 Retry limit reached. No '{validationKey}' detected.");
                        callback?.Invoke(json);
                        return;
                    }
                }

                callback?.Invoke(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ JSON parsing error: {e.Message}");
                Debug.LogError($"Raw response: {response}");

                if (retriesLeft > 0)
                {
                    Debug.LogWarning($"🔁 Retrying JSON parse... attempts left: {retriesLeft}");
                    StartCoroutine(AnalyzeImageForJSONCoroutine(lastImage, lastPrompt, retryResponse =>
                    {
                        TryParseWithRetry(retryResponse, retriesLeft - 1, lastCallback, validationKey);
                    }));
                }
                else
                {
                    Debug.LogError("🛑 All retries failed.");
                    callback?.Invoke(null);
                }
            }
        }

        #endregion

        #region Ingredient Recognition Module

        /// <summary>
        /// Recognize ingredients in kitchen image
        /// </summary>
        /// <param name="texture">Kitchen image</param>
        /// <param name="callback">Callback with ingredient list</param>
        public void RecognizeIngredients(Texture2D texture, Action<List<Ingredient>> callback = null)
        {
            string prompt = "Identify all visible ingredients in this kitchen image. Look for fruits, vegetables, meats, dairy products, spices, and other cooking ingredients. Assess their freshness and quantity.";
            string jsonSchema = @"{
                ""ingredients"": [
                    {
                        ""name"": ""ingredient_name"",
                        ""category"": ""fruits/vegetables/meat/dairy/spices/grains/other"",
                        ""quantity_estimate"": ""estimated_amount"",
                        ""freshness"": ""fresh/good/needs_attention/spoiled"",
                        ""confidence"": 0.95
                    }
                ]
            }";

            AnalyzeImageWithJSON(texture, prompt, jsonSchema, "ingredients", (jsonResponse) =>
            {
                var ingredients = ParseIngredientsFromJSON(jsonResponse);
                OnIngredientsDetected?.Invoke(ingredients);
                callback?.Invoke(ingredients);
            });
        }

        /// <summary>
        /// Custom ingredient analysis with specific requirements
        /// </summary>
        /// <param name="texture">Kitchen image</param>
        /// <param name="customPrompt">Custom analysis prompt</param>
        /// <param name="callback">Callback with ingredient list</param>
        public void RecognizeIngredientsCustom(Texture2D texture, string customPrompt, Action<List<Ingredient>> callback = null)
        {
            string jsonSchema = @"{
                ""ingredients"": [
                    {
                        ""name"": ""ingredient_name"",
                        ""category"": ""category_type"",
                        ""quantity_estimate"": ""estimated_amount"",
                        ""freshness"": ""condition_assessment"",
                        ""confidence"": 0.95
                    }
                ]
            }";

            AnalyzeImageWithJSON(texture, customPrompt, jsonSchema, "ingredients", (jsonResponse) =>
            {
                var ingredients = ParseIngredientsFromJSON(jsonResponse);
                OnIngredientsDetected?.Invoke(ingredients);
                callback?.Invoke(ingredients);
            });
        }

        #endregion

        #region Appliance Recognition Module

        /// <summary>
        /// Recognize kitchen appliances in image
        /// </summary>
        /// <param name="texture">Kitchen image</param>
        /// <param name="callback">Callback with appliance list</param>
        public void RecognizeAppliances(Texture2D texture, Action<List<Appliance>> callback = null)
        {
            string prompt = "Identify all visible kitchen appliances in this image. Look for ovens, stoves, microwaves, blenders, food processors, refrigerators, etc. Assess their status and capabilities.";
            string jsonSchema = @"{
                ""appliances"": [
                    {
                        ""name"": ""appliance_name"",
                        ""type"": ""oven/stove/microwave/blender/processor/refrigerator/other"",
                        ""status"": ""on/off/standby/unknown"",
                        ""capabilities"": [""baking"", ""heating"", ""mixing""],
                        ""confidence"": 0.95
                    }
                ]
            }";

            AnalyzeImageWithJSON(texture, prompt, jsonSchema, "appliances", (jsonResponse) =>
            {
                var appliances = ParseAppliancesFromJSON(jsonResponse);
                OnAppliancesDetected?.Invoke(appliances);
                callback?.Invoke(appliances);
            });
        }

        /// <summary>
        /// Custom appliance analysis with safety checks
        /// </summary>
        /// <param name="texture">Kitchen image</param>
        /// <param name="includeSafetyCheck">Include safety status in analysis</param>
        /// <param name="callback">Callback with appliance list</param>
        public void RecognizeAppliancesWithSafety(Texture2D texture, bool includeSafetyCheck, Action<List<Appliance>> callback = null)
        {
            string prompt = includeSafetyCheck ?
                "Identify all kitchen appliances and assess their safety status. Check for any visible hazards, proper usage, or maintenance needs." :
                "Identify all visible kitchen appliances and their current status.";

            string jsonSchema = @"{
                ""appliances"": [
                    {
                        ""name"": ""appliance_name"",
                        ""type"": ""appliance_type"",
                        ""status"": ""on/off/standby/unknown"",
                        ""capabilities"": [""function1"", ""function2""],
                        ""safety_status"": ""safe/caution/unsafe"",
                        ""confidence"": 0.95
                    }
                ]
            }";

            AnalyzeImageWithJSON(texture, prompt, jsonSchema, "appliances", (jsonResponse) =>
            {
                var appliances = ParseAppliancesFromJSON(jsonResponse);
                OnAppliancesDetected?.Invoke(appliances);
                callback?.Invoke(appliances);
            });
        }

        #endregion

        #region Voice Recognition Module

        /// <summary>
        /// Start voice recording with automatic voice activity detection
        /// </summary>
        public void StartVoiceRecording()
        {
            if (string.IsNullOrEmpty(micDevice))
            {
                Debug.LogError("No microphone available!");
                return;
            }

            if (isRecording)
            {
                DebugLog("Already recording!");
                return;
            }

            currentRecording = Microphone.Start(micDevice, false, maxRecordingDuration, sampleRate);
            isRecording = true;
            DebugLog("Voice recording started...");

            // Start voice activity detection
            voiceActivityCoroutine = StartCoroutine(MonitorVoiceActivity());
        }

        /// <summary>
        /// Stop voice recording and transcribe
        /// </summary>
        /// <param name="callback">Callback with transcribed text</param>
        public void StopVoiceRecording(Action<string> callback = null)
        {
            if (!isRecording)
            {
                DebugLog("Not currently recording!");
                return;
            }

            Microphone.End(micDevice);
            isRecording = false;

            if (voiceActivityCoroutine != null)
            {
                StopCoroutine(voiceActivityCoroutine);
                voiceActivityCoroutine = null;
            }

            DebugLog("Voice recording stopped.");
            StartCoroutine(TranscribeAudioCoroutine(currentRecording, callback));
        }

        /// <summary>
        /// Simplified voice transcription - record for specified duration
        /// </summary>
        /// <param name="duration">Recording duration in seconds</param>
        /// <param name="callback">Callback with transcribed text</param>
        public void RecordAndTranscribe(float duration, Action<string> callback = null)
        {
            StartCoroutine(RecordAndTranscribeCoroutine(duration, callback));
        }

        private IEnumerator RecordAndTranscribeCoroutine(float duration, Action<string> callback)
        {
            StartVoiceRecording();
            yield return new WaitForSeconds(duration);
            StopVoiceRecording(callback);
        }

        private IEnumerator MonitorVoiceActivity()
        {
            float silenceTime = 0f;
            const float maxSilence = 3f; // Stop recording after 3 seconds of silence

            while (isRecording)
            {
                float[] samples = new float[128];
                int micPosition = Microphone.GetPosition(micDevice);

                if (micPosition > 0)
                {
                    currentRecording.GetData(samples, micPosition - 128);

                    float volume = 0f;
                    foreach (float sample in samples)
                    {
                        volume += Mathf.Abs(sample);
                    }
                    volume /= samples.Length;

                    if (volume < voiceActivityThreshold)
                    {
                        silenceTime += Time.deltaTime;
                        if (silenceTime >= maxSilence)
                        {
                            DebugLog("Voice activity timeout - stopping recording");
                            StopVoiceRecording();
                            break;
                        }
                    }
                    else
                    {
                        silenceTime = 0f;
                    }
                }

                yield return new WaitForSeconds(0.1f);
            }
        }

        private IEnumerator TranscribeAudioCoroutine(AudioClip audioClip, Action<string> callback)
        {
            // Convert AudioClip to WAV bytes
            byte[] wavData = ConvertAudioClipToWav(audioClip);

            // Create form data
            WWWForm form = new WWWForm();
            form.AddField("model", whisperModel);
            form.AddField("language", "en");
            form.AddBinaryData("file", wavData, "audio.wav", "audio/wav");

            using (UnityWebRequest request = UnityWebRequest.Post(GROQ_WHISPER_ENDPOINT, form))
            {
                request.SetRequestHeader("Authorization", $"Bearer {groqApiKey}");

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Whisper transcription failed: {request.error}");
                    callback?.Invoke("");
                }
                else
                {
                    try
                    {
                        var jsonResponse = JObject.Parse(request.downloadHandler.text);
                        string transcription = jsonResponse["text"]?.ToString() ?? "";
                        DebugLog($"Transcription: {transcription}");
                        OnVoiceTranscribed?.Invoke(transcription);
                        callback?.Invoke(transcription);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Transcription parsing error: {e.Message}");
                        callback?.Invoke("");
                    }
                }
            }
        }

        #endregion

        #region Timer Module

        /// <summary>
        /// Process voice command for timer operations
        /// </summary>
        /// <param name="voiceInput">Voice input (if null, will record new input)</param>
        /// <param name="callback">Callback with timer command</param>
        public void ProcessTimerCommand(string voiceInput = null, Action<TimerCommand> callback = null)
        {
            if (string.IsNullOrEmpty(voiceInput))
            {
                // Record new voice input
                RecordAndTranscribe(5f, (transcription) =>
                {
                    if (!string.IsNullOrEmpty(transcription))
                    {
                        ParseTimerCommand(transcription, callback);
                    }
                    else
                    {
                        callback?.Invoke(null);
                    }
                });
            }
            else
            {
                // Use provided voice input
                ParseTimerCommand(voiceInput, callback);
            }
        }

        private void ParseTimerCommand(string transcription, Action<TimerCommand> callback)
        {
            string prompt = $"Parse this voice command for timer operations: '{transcription}'\n\nExtract the timer action, duration, and any timer name mentioned.";
            string jsonSchema = @"{
                ""action"": ""start/stop/pause/cancel/resume"",
                ""duration_minutes"": 0,
                ""timer_name"": ""optional_timer_name"",
                ""confidence"": 0.95
            }";

            ProcessTextWithJSON(prompt, jsonSchema, (jsonResponse) =>
            {
                var timerCommand = ParseTimerCommandFromJSON(jsonResponse);
                OnTimerCommandReceived?.Invoke(timerCommand);
                callback?.Invoke(timerCommand);
            });
        }

        #endregion

        #region Recipe Generation Module

        /// <summary>
        /// Generate recipe from available ingredients and appliances
        /// </summary>
        /// <param name="ingredients">Available ingredients</param>
        /// <param name="appliances">Available appliances</param>
        /// <param name="callback">Callback with generated recipe</param>
        public void GenerateRecipe(List<Ingredient> ingredients, List<Appliance> appliances, Action<Recipe> callback = null)
        {
            string ingredientList = string.Join(", ", ingredients.ConvertAll(i => i.name));
            string applianceList = string.Join(", ", appliances.ConvertAll(a => a.name));

            string prompt = $"Create a recipe using these ingredients: {ingredientList}\nUsing these appliances: {applianceList}\n\nProvide detailed step-by-step cooking instructions with timing.";
            string jsonSchema = @"{
                ""recipe_name"": ""recipe_title"",
                ""prep_time_minutes"": 0,
                ""cook_time_minutes"": 0,
                ""difficulty"": ""easy/medium/hard"",
                ""ingredients_needed"": [
                    {
                        ""name"": ""ingredient_name"",
                        ""amount"": ""quantity_with_unit""
                    }
                ],
                ""instructions"": [
                    {
                        ""step"": 1,
                        ""description"": ""step_description"",
                        ""duration_minutes"": 0,
                        ""appliance_needed"": ""appliance_name""
                    }
                ],
                ""tips"": [""cooking_tip1"", ""cooking_tip2""]
            }";

            ProcessTextWithJSON(prompt, jsonSchema, (jsonResponse) =>
            {
                var recipe = ParseRecipeFromJSON(jsonResponse);
                OnRecipeGenerated?.Invoke(recipe);
                callback?.Invoke(recipe);
            });
        }

        /// <summary>
        /// Generate recipe with dietary restrictions
        /// </summary>
        /// <param name="ingredients">Available ingredients</param>
        /// <param name="appliances">Available appliances</param>
        /// <param name="dietaryRestrictions">Dietary restrictions (e.g., "vegan", "gluten-free")</param>
        /// <param name="callback">Callback with generated recipe</param>
        public void GenerateRecipeWithDiet(List<Ingredient> ingredients, List<Appliance> appliances,
            List<string> dietaryRestrictions, Action<Recipe> callback = null)
        {
            string ingredientList = string.Join(", ", ingredients.ConvertAll(i => i.name));
            string applianceList = string.Join(", ", appliances.ConvertAll(a => a.name));
            string dietList = string.Join(", ", dietaryRestrictions);

            string prompt = $"Create a {dietList} recipe using these ingredients: {ingredientList}\nUsing these appliances: {applianceList}\n\nEnsure the recipe adheres to all dietary restrictions mentioned.";

            GenerateRecipe(ingredients, appliances, callback);
        }

        #endregion

        #region Text Processing

        /// <summary>
        /// Process text with structured JSON response
        /// </summary>
        /// <param name="prompt">Text prompt</param>
        /// <param name="jsonSchema">Expected JSON schema</param>
        /// <param name="callback">Callback with parsed JSON</param>
        public void ProcessTextWithJSON(string prompt, string jsonSchema, Action<JObject> callback)
        {
            string enhancedPrompt = $"{prompt}\n\nRespond ONLY with valid JSON in this exact format:\n{jsonSchema}\n\nDo not include any text outside the JSON structure.";
            StartCoroutine(ProcessTextCoroutine(enhancedPrompt, callback));
        }

        private IEnumerator ProcessTextCoroutine(string prompt, Action<JObject> callback)
        {
            var requestData = new
            {
                model = textModel,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                max_tokens = 2000,
                temperature = 0.7
            };

            yield return StartCoroutine(SendGroqRequest(GROQ_CHAT_ENDPOINT, requestData, (response) =>
            {
                if (response != null)
                {
                    try
                    {
                        var jsonResponse = JObject.Parse(response);
                        string content = jsonResponse["choices"]?[0]?["message"]?["content"]?.ToString();
                        var parsedContent = JObject.Parse(content);
                        callback?.Invoke(parsedContent);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Text processing error: {e.Message}");
                        callback?.Invoke(null);
                    }
                }
                else
                {
                    callback?.Invoke(null);
                }
            }));
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Capture texture from RawImage component
        /// </summary>
        /// <param name="rawImage">RawImage component</param>
        /// <returns>Texture2D for analysis</returns>
        public Texture2D CaptureFromRawImage(RawImage rawImage)
        {
            Texture sourceTexture = rawImage.texture;

            if (sourceTexture == null)
            {
                Debug.LogError("RawImage has no texture.");
                return null;
            }

            if (sourceTexture is Texture2D t2d)
            {
                return t2d;
            }
            else if (sourceTexture is WebCamTexture webcamTexture)
            {
                Texture2D texture2D = new Texture2D(webcamTexture.width, webcamTexture.height, TextureFormat.RGB24, false);
                texture2D.SetPixels32(webcamTexture.GetPixels32());
                texture2D.Apply();
                return texture2D;
            }
            else
            {
                Debug.LogError($"Unsupported texture type: {sourceTexture.GetType()}");
                return null;
            }
        }

        /// <summary>
        /// Convert AudioClip to WAV format bytes
        /// </summary>
        private byte[] ConvertAudioClipToWav(AudioClip clip)
        {
            float[] samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);

            byte[] wavData = new byte[44 + samples.Length * 2];

            // WAV header
            Encoding.ASCII.GetBytes("RIFF").CopyTo(wavData, 0);
            BitConverter.GetBytes(wavData.Length - 8).CopyTo(wavData, 4);
            Encoding.ASCII.GetBytes("WAVE").CopyTo(wavData, 8);
            Encoding.ASCII.GetBytes("fmt ").CopyTo(wavData, 12);
            BitConverter.GetBytes(16).CopyTo(wavData, 16);
            BitConverter.GetBytes((short)1).CopyTo(wavData, 20);
            BitConverter.GetBytes((short)clip.channels).CopyTo(wavData, 22);
            BitConverter.GetBytes(clip.frequency).CopyTo(wavData, 24);
            BitConverter.GetBytes(clip.frequency * clip.channels * 2).CopyTo(wavData, 28);
            BitConverter.GetBytes((short)(clip.channels * 2)).CopyTo(wavData, 32);
            BitConverter.GetBytes((short)16).CopyTo(wavData, 34);
            Encoding.ASCII.GetBytes("data").CopyTo(wavData, 36);
            BitConverter.GetBytes(samples.Length * 2).CopyTo(wavData, 40);

            // Audio data
            for (int i = 0; i < samples.Length; i++)
            {
                short sample = (short)(samples[i] * 32767);
                byte[] sampleBytes = BitConverter.GetBytes(sample);
                sampleBytes.CopyTo(wavData, 44 + i * 2);
            }

            return wavData;
        }

        /// <summary>
        /// Generic HTTP request to Groq API
        /// </summary>
        private IEnumerator SendGroqRequest(string endpoint, object requestData, Action<string> callback)
        {
            string jsonData = JsonConvert.SerializeObject(requestData);

            using (UnityWebRequest request = new UnityWebRequest(endpoint, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", $"Bearer {groqApiKey}");

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Groq API request failed: {request.error}");
                    callback?.Invoke(null);
                }
                else
                {
                    callback?.Invoke(request.downloadHandler.text);
                }
            }
        }

        private void DebugLog(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[ChefMR AI] {message}");
            }
        }

        #endregion

        #region JSON Parsing Methods

        public List<Ingredient> ParseIngredientsFromJSON(JObject jsonResponse)
        {
            var ingredients = new List<Ingredient>();

            if (jsonResponse == null) return ingredients;

            try
            {
                var ingredientArray = jsonResponse["ingredients"] as JArray;
                if (ingredientArray != null)
                {
                    foreach (var item in ingredientArray)
                    {
                        ingredients.Add(new Ingredient
                        {
                            name = item["name"]?.ToString() ?? "",
                            category = item["category"]?.ToString() ?? "",
                            quantityEstimate = item["quantity_estimate"]?.ToString() ?? "",
                            freshness = item["freshness"]?.ToString() ?? "",
                            confidence = (float)(item["confidence"] ?? 0.0)
                        });
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error parsing ingredients: {e.Message}");
            }
            return ingredients;
        }

        public List<Appliance> ParseAppliancesFromJSON(JObject jsonResponse)
        {
            var appliances = new List<Appliance>();

            if (jsonResponse == null) return appliances;

            try
            {
                var applianceArray = jsonResponse["appliances"] as JArray;
                if (applianceArray != null)
                {
                    foreach (var item in applianceArray)
                    {
                        var capabilities = new List<string>();
                        var capArray = item["capabilities"] as JArray;
                        if (capArray != null)
                        {
                            foreach (var cap in capArray)
                            {
                                capabilities.Add(cap?.ToString() ?? "");
                            }
                        }

                        appliances.Add(new Appliance
                        {
                            name = item["name"]?.ToString() ?? "",
                            type = item["type"]?.ToString() ?? "",
                            status = item["status"]?.ToString() ?? "",
                            capabilities = capabilities,
                            confidence = (float)(item["confidence"] ?? 0.0)
                        });
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error parsing appliances: {e.Message}");
            }
            return appliances;
        }

        private TimerCommand ParseTimerCommandFromJSON(JObject jsonResponse)
        {
            try
            {
                return new TimerCommand
                {
                    action = jsonResponse["action"]?.ToString() ?? "",
                    durationMinutes = (int)(jsonResponse["duration_minutes"] ?? 0),
                    timerName = jsonResponse["timer_name"]?.ToString() ?? "",
                    confidence = (float)(jsonResponse["confidence"] ?? 0.0)
                };
            }
            catch (Exception e)
            {
                Debug.LogError("Error parsing timer command: " + e.Message);
                return null;
            }
        }

        private Recipe ParseRecipeFromJSON(JObject jsonResponse)
        {
            try
            {
                var recipe = new Recipe
                {
                    name = jsonResponse["recipe_name"]?.ToString() ?? "",
                    prepTimeMinutes = (int)(jsonResponse.Value<int?>("prep_time_minutes") ?? 0),
                    cookTimeMinutes = (int)(jsonResponse["cook_time_minutes"] ?? 0),
                    difficulty = jsonResponse["difficulty"]?.ToString() ?? "",
                    ingredientsNeeded = new List<RecipeIngredient>(),
                    instructions = new List<CookingStep>(),
                    tips = new List<string>()
                };

                // Parse ingredients
                var ingredientArray = jsonResponse["ingredients_needed"] as JArray;
                if (ingredientArray != null)
                {
                    foreach (var item in ingredientArray)
                    {
                        recipe.ingredientsNeeded.Add(new RecipeIngredient
                        {
                            name = item["name"]?.ToString() ?? "",
                            amount = item["amount"]?.ToString() ?? ""
                        });
                    }
                }

                // Parse instructions
                var instructionArray = jsonResponse["instructions"] as JArray;
                if (instructionArray != null)
                {
                    foreach (var item in instructionArray)
                    {
                        recipe.instructions.Add(new CookingStep
                        {
                            step = (int)(item["step"]?? 0),
                            description = item["description"]?.ToString() ?? "",
                            durationMinutes = (int)(item["duration_minutes"] ?? 0),
                            applianceNeeded = item["appliance_needed"]?.ToString() ?? ""
                        });
                    }
                }

                // Parse tips
                var tipArray = jsonResponse["tips"] as JArray;
                if (tipArray != null)
                {
                    foreach (var tip in tipArray)
                    {
                        recipe.tips.Add(tip?.ToString() ?? "");
                    }
                }

                return recipe;
            }
            catch (Exception e)
            {
                Debug.LogError("Error parsing recipe: " + e.Message);
                return null;
            }
        }

        #endregion
    }
}