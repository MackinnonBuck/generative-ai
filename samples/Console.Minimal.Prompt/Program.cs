using Microsoft.Extensions.Configuration;
using Mscc.GenerativeAI;

// Get the API key from the configuration
var Configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile("appsettings.user.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();
var apiKey = Configuration["Gemini:Credentials:ApiKey"];
if (string.IsNullOrEmpty(apiKey))
{
    apiKey = Environment.GetEnvironmentVariable("GOOGLE_API_KEY");
}

// Create a new instance of the GenerativeModel class.
var model = new GenerativeModel(apiKey);

// Create a loop to keep the program running until the user exits entering the Escape key.
var hint = " (Press Escape to exit)";
while (true)
{
    // Prompt the user for a prompt
    Console.Write($"Enter a prompt{hint}: ");
    string request = Console.ReadLine();

    // check input for Escape key and exit if found
    if (request == "\x1b")
    {
        break;
    }
    hint = string.Empty;

    // Send the prompt to Gemini (using Google AI with API key)
    var response = await model.GenerateContent(request);

    // Display the response
    Console.WriteLine($"Response: {response.Text}");
    Console.WriteLine();
}
