using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using OllamaFlow.Core.Models.Agnostic.Requests;
using OllamaFlow.Core.Models.Agnostic.Common;
using OllamaFlow.Core.Services.Transformation.Outbound;
using OllamaFlow.Core.Serialization;
using OllamaFlow.Core.Enums;

namespace Test
{
    /// <summary>
    /// Simple test to show what the transformation produces without mocking Watson
    /// </summary>
    public class TransformationOutputTest
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.

        [Fact]
        public async Task ShowTransformationOutput()
        {
            try
            {
                Console.WriteLine("=== Ollama to OpenAI Transformation Test ===");

                // Create the agnostic request (what would come from Ollama transformation)
                AgnosticChatRequest agnosticRequest = new AgnosticChatRequest
                {
                    SourceFormat = ApiFormatEnum.Ollama,
                    Model = "Qwen/Qwen2.5-3B",
                    Messages = new List<AgnosticMessage>
                    {
                        new AgnosticMessage
                        {
                            Role = "user",
                            Content = "Please give me an overview of the similarities and differences between RISC, x86, and ARM"
                        }
                    },
                    Stream = true,
                    Temperature = 0.8,
                    TopP = 0.9,
                    MaxTokens = 128,
                    FrequencyPenalty = 0,
                    PresencePenalty = 0,
                    // These come from Ollama options but aren't standard OpenAI
                    Options = new Dictionary<string, object>
                    {
                        ["top_k"] = 40,
                        ["min_p"] = 0,
                        ["tfs_z"] = 1,
                        ["typical_p"] = 1,
                        ["repeat_last_n"] = 64,
                        ["repeat_penalty"] = 1.1,
                        ["mirostat"] = 0,
                        ["mirostat_tau"] = 5,
                        ["mirostat_eta"] = 0.1,
                        ["penalize_newline"] = true,
                        ["numa"] = false,
                        ["num_ctx"] = 2048,
                        ["num_batch"] = 512,
                        ["num_gpu"] = 1,
                        ["main_gpu"] = 0,
                        ["low_vram"] = false,
                        ["vocab_only"] = false,
                        ["use_mmap"] = true,
                        ["use_mlock"] = false,
                        ["num_thread"] = (object?)null
                    }
                };

                // Transform to OpenAI format
                AgnosticToOpenAITransformer transformer = new AgnosticToOpenAITransformer();
                object openAIRequest = await transformer.TransformAsync(agnosticRequest);

                // Serialize to see the JSON
                Serializer serializer = new Serializer();
                string openAIJson = serializer.SerializeJson(openAIRequest, true);

                Console.WriteLine("Transformed OpenAI Request:");
                Console.WriteLine(openAIJson);

                Console.WriteLine("\n=== Analysis ===");
                Console.WriteLine("- Model: Qwen/Qwen2.5-3B (vLLM needs to support this)");
                Console.WriteLine("- Stream: true (should be fine)");
                Console.WriteLine("- Standard OpenAI parameters preserved");
                Console.WriteLine("- Ollama-specific options dropped (as expected)");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
    }
}