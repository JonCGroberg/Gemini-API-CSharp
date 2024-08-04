using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gemini
{
    public class GeminiClient(string apiKey, string model, List<Content>? context = null)
    {
        private readonly HttpClient _httpClient = new();
        public List<Content> initialContext = context ?? [];
        public List<Content> context = context ?? [];

        public void GiveContext(List<Content> context)
        {
            this.context = context;
        }

        public async Task<string> Prompt(string prompt, float temperature = 0.9f, int maxOutputTokens = 600)
        {
            // Add the current user input to the context.
            context.Add(new Content { Role = "user", Parts = [new Part { Text = prompt }] });

            // Create the request with the context.
            var requestData = new GeminiRequest(context, temperature, maxOutputTokens);
            var jsonContent = JsonSerializer.Serialize(requestData);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri($"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}"),
                Content = content
            };

            // Try to send the request and return the response
            try
            {
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                // Parse the response
                var responseContent = await response.Content.ReadAsStringAsync();
                var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseContent);
                return geminiResponse!.Candidates.First().Content.Parts.First().Text;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Error: {e.Message}");
                return null;
            }
        }
    }
    public class GenerationConfig
    {
        [JsonPropertyName("temperature")]
        public float Temperature { get; set; }

        [JsonPropertyName("topP")]
        public float TopP { get; set; }

        [JsonPropertyName("maxOutputTokens")]
        public int MaxOutputTokens { get; set; }

        [JsonPropertyName("responseMimeType")]
        public string? ResponseMimeType { get; set; }
    }
    public class GeminiRequest
    {
        [JsonPropertyName("contents")]
        public List<Content> Contents { get; set; }

        [JsonPropertyName("generationConfig")]
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
    public class GeminiResponse
    {
        [JsonPropertyName("candidates")]
        public required Candidate[] Candidates { get; set; }
        [JsonPropertyName("usageMetadata")]
        public required UsageMetadata UsageMetadata { get; set; }
    }
    public class Candidate
    {
        [JsonPropertyName("content")]
        public required Content Content { get; set; }
        [JsonPropertyName("finishReason")]
        public required string FinishReason { get; set; }
        [JsonPropertyName("index")]
        public int Index { get; set; }
        [JsonPropertyName("metadata")]
        public SafetyRating[]? SafetyRatings { get; set; }
    }
    public class Content
    {
        [JsonPropertyName("role")]
        public required string Role { get; set; }

        [JsonPropertyName("parts")]
        public required Part[] Parts { get; set; }
    }
    public class Part
    {
        [JsonPropertyName("text")]
        public required string Text { get; set; }
    }
    public class UsageMetadata
    {
        [JsonPropertyName("promptTokenCount")]
        public int PromptTokenCount { get; set; }
        [JsonPropertyName("candidatesTokenCount")]
        public int CandidatesTokenCount { get; set; }
        [JsonPropertyName("totalTokenCount")]
        public int TotalTokenCount { get; set; }
    }
    public class SafetyRating
    {
        [JsonPropertyName("category")]
        public required string Category { get; set; }
        [JsonPropertyName("probability")]
        public required string Probability { get; set; }
    }
}
