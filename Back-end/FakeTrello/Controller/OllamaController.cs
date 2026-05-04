using FakeTrello.Model.AI;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace FakeTrello.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class OllamaController : ControllerBase
    {
        private readonly HttpClient _httpClient;

        public OllamaController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        [HttpPost("ask")]
        public async Task<IActionResult> AskModel([FromBody] PromptRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Prompt))
                return BadRequest("Prompt is required.");

            var ollamaRequest = new
            {
                model = "llama3.2",
                prompt = $"Please do not include pretext or post text, just do the task I am asking you to do. Please give me a summary of this description: {request.Prompt}",
                stream = false
            };

            var response = await _httpClient.PostAsJsonAsync("http://localhost:11434/api/generate", ollamaRequest);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, $"Ollama Error: {error}");
            }

            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            string modelResponse = doc.RootElement.GetProperty("response").GetString();

            return Ok(new { response = modelResponse });
        }
    }
}
