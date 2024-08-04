using dotenv.net;
using Gemini;

DotEnv.Load();
var envVars = DotEnv.Fluent()
    .WithoutExceptions()
    .WithEnvFiles()
    .Read();

string model = "gemini-1.0-pro";
List<Content> conversationHistory =
[
    new Content{ Role = "user", Parts = [new Part { Text = "You are a conversational AI in a vr language immersion simulation. You only can only respond in Chinese because this is the only language you know besides a handfull of english words. You should only respond in sentences as if you are in dialogue with the user. You must use less than 3 sentences for all responses.  Wait for the user to talk to you. If the user is struggling to understand you, then are allowed to use your very very broken english mixed with chinese to try to communicate." }] },
    new Content { Role = "model", Parts = [new Part { Text = "[Please start the dialogue in a VR language immersion simulation.]" }] }
];

var apiKey = envVars["GOOGLE_AI_STUDIO_KEY"];
var generator = new GeminiClient(apiKey, model,conversationHistory);


Console.Clear();
Console.WriteLine("-- Language immersion simulation --\n");
Console.WriteLine("Welcome to Gemini! \nThis model can only speak in a foreign language.\nYou can now start the conversation by typing your prompt below.\n");
while (true)
{
    Console.WriteLine("You:");
    string userInput = Console.ReadLine();
    Console.WriteLine($"\n{model}:\n {await generator.Prompt(userInput)}\n");
}
