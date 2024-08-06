using UnityEngine;
using UnityEngine.UI;
using TMPro; // Import TextMesh Pro namespace
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.Text;

public class GeminiImmersionSimulation : MonoBehaviour
{
    public string model = "gemini-1.0-pro";
    public string apiKey = "AIzaSyBSnvY6TI60wdPs8x6tZKjCQEqZG8x4yCY";

    public float temperature = 0.9f;

    public int maxOutputTokens = 600;
    public string prePrompt = "You are a conversational AI in a vr language immersion simulation. You only can only respond in Chinese because this is the only language you know besides a handful of english words. You should only respond in sentences as if you are in dialogue with the user. You must use less than 3 sentences for all responses.  Wait for the user to talk to you. If the user is struggling to understand you, then are allowed to use your very very broken english mixed with chinese to try to communicate.";

    private List<Content> conversationHistory = new List<Content>
    {
        new Content
        {
            Role = "user",
            Parts = new List<Part>
            {
                new Part
                {
                    Text = ""
                }
            }
        },
        new Content
        {
            Role = "model",
            Parts = new List<Part>
            {
                new Part
                {
                    Text = "[Please start the dialogue in a VR language immersion simulation.]"
                }
            }
        }
    };
    private GeminiClient generator;
    [SerializeField] // Allow the user to assign this in the inspector
    private TMP_InputField userInputField;
    [SerializeField] // Allow the user to assign this in the inspector
    private TextMeshProUGUI conversationText;

    // TEMPORARY STUFF HERE
    private void Start()
    {
        if (userInputField == null || conversationText == null)
        {
            Debug.LogError("Please assign the UserInputField and ConversationText in the inspector.");
            return;
        }

        conversationHistory[0].Parts[0].Text = prePrompt;
        generator = new GeminiClient(apiKey, model, conversationHistory);
        conversationText.text = "-- Language immersion simulation --\n";
        conversationText.text += "Welcome to Gemini! \nThis model can only speak in a foreign language.\nYou can now start the conversation by typing a prompt.\n";
    }
    public void OnUserInputSubmit()
    {
        Debug.Log("User input submitted: " + userInputField.text);
        string userInput = userInputField.text;
        userInputField.text = "";
        StartCoroutine(generator.Prompt(userInput, (responseText) =>
        {
            if (responseText != null)
            {
                conversationText.text += "\nYou: " + userInput + "\n\n";
                conversationText.text += model + ": " + responseText + "\n";
            }
            else
            {
                Debug.LogError("Error occurred");
            }
        }, temperature, maxOutputTokens));
    }

    public class GeminiClient
    {
        private string apiKey;
        private string model;
        private List<Content> initialContext = new List<Content>();
        private List<Content> context = new List<Content>();

        public GeminiClient(string apiKey, string model, List<Content> context)
        {
            this.apiKey = apiKey;
            this.model = model;
            if (context != null)
            {
                this.initialContext = context;
            }
        }

        public void GiveContext(List<Content> context)
        {
            this.initialContext = context;
        }
        public delegate void ResponseCallback(string responseText);

        public IEnumerator Prompt(string prompt, ResponseCallback callback, float temperature = 0.9f, int maxOutputTokens = 600)
        {
            // Add the current user input to the context.
            context = initialContext;
            context.Add(new Content
            {
                Role = "user",
                Parts = new List<Part> { new Part { Text = prompt } }
            });

            // Create the request with the context.
            var requestData = new GeminiRequest(context, temperature, maxOutputTokens);
            var jsonContent = JsonConvert.SerializeObject(requestData); // Use JsonConvert instead of JsonUtility

            // Make the HTTP request.
            var request = new UnityWebRequest($"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}", "POST");
            request.SetRequestHeader("Content-Type", "application/json");
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonContent));
            request.downloadHandler = new DownloadHandlerBuffer();

            yield return request.SendWebRequest(); // Use yield return here

            // Check if the request was successful.
            if (request.result == UnityWebRequest.Result.Success)
            {
                // Parse the response.
                var responseContent = request.downloadHandler.text;
                var geminiResponse = JsonConvert.DeserializeObject<GeminiResponse>(responseContent); // Use JsonConvert instead of JsonUtility

                // Process the response here
                string responseText = geminiResponse.Candidates[0].Content.Parts[0].Text;

                // Call the callback with the response text
                callback(responseText);
            }
            else
            {
                Debug.LogError($"Error: {JsonConvert.SerializeObject(request)}");
                callback(null); // Call the callback with null to indicate an error
            }
        }
    }

    [System.Serializable]
    public class GenerationConfig
    {
        [JsonProperty("temperature")]
        public float Temperature { get; set; }

        [JsonProperty("topP")]
        public float TopP { get; set; }

        [JsonProperty("maxOutputTokens")]
        public int MaxOutputTokens { get; set; }

        [JsonProperty("responseMimeType")]
        public string ResponseMimeType { get; set; }
    }

    [System.Serializable]
    public class GeminiRequest
    {
        [JsonProperty("contents")]
        public List<Content> Contents { get; set; }

        [JsonProperty("generationConfig")]
        public GenerationConfig GenerationConfig { get; set; }

        public GeminiRequest(List<Content> Context, float temperature = 0.9f, int maxOutputTokens = 600)
        {
            Contents = Context;
            GenerationConfig = new GenerationConfig
            {
                Temperature = temperature,
                TopP = 1.0f,
                MaxOutputTokens = maxOutputTokens,
                ResponseMimeType = "text/plain"
            };
        }
    }

    [System.Serializable]
    public class GeminiResponse
    {
        [JsonProperty("candidates")]
        public Candidate[] Candidates { get; set; }

        [JsonProperty("usageMetadata")]
        public UsageMetadata UsageMetadata { get; set; }
    }

    [System.Serializable]
    public class Candidate
    {
        [JsonProperty("content")]
        public Content Content { get; set; }

        [JsonProperty("finishReason")]
        public string FinishReason { get; set; }

        [JsonProperty("index")]
        public int Index { get; set; }

        [JsonProperty("metadata")]
        public SafetyRating[] SafetyRatings { get; set; }
    }

    [System.Serializable]
    public class Content
    {
        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("parts")]
        public List<Part> Parts { get; set; }
    }

    [System.Serializable]
    public class Part
    {
        [JsonProperty("text")]
        public string Text { get; set; }
    }

    [System.Serializable]
    public class UsageMetadata
    {
        [JsonProperty("promptTokenCount")]
        public int PromptTokenCount { get; set; }

        [JsonProperty("candidatesTokenCount")]
        public int CandidatesTokenCount { get; set; }

        [JsonProperty("totalTokenCount")]
        public int TotalTokenCount { get; set; }
    }

    [System.Serializable]
    public class SafetyRating
    {
        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("probability")]
        public string Probability { get; set; }
    }
}
