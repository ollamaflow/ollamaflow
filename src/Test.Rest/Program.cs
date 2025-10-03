namespace Test.Rest
{
    using System;
    using System.Net.Http;
    using System.Text;
    using RestWrapper;

    public static class Program
    {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.

        private static string _Url = "http://astra:11434/api/chat";
        private static HttpMethod _Method = HttpMethod.Post;
        private static string _ContentType = "application/json";

        /*
        // use with URL /api/chat
        private static string _Body = @"{
          ""model"": ""gemma3:4b"",
          ""stream"": false,
          ""messages"": [
            {
              ""role"": ""system"",
              ""content"": ""you are a helpful AI assistant.  be nice""
            },
            {
              ""role"": ""user"",
              ""content"": ""what can you tell me about botox""
            }
          ],
          ""options"": {
            ""num_keep"": 5,
            ""seed"": 42,
            ""num_predict"": 100,
            ""top_k"": 20,
            ""top_p"": 0.9,
            ""min_p"": 0,
            ""tfs_z"": 0.5,
            ""typical_p"": 0.7,
            ""repeat_last_n"": 33,
            ""temperature"": 0.8,
            ""repeat_penalty"": 1.2,
            ""presence_penalty"": 1.5,
            ""frequency_penalty"": 1,
            ""mirostat"": 1,
            ""mirostat_tau"": 0.8,
            ""mirostat_eta"": 0.6,
            ""penalize_newline"": true,
            ""numa"": false,
            ""num_ctx"": 1024,
            ""num_batch"": 2,
            ""num_gpu"": 1,
            ""main_gpu"": 0,
            ""low_vram"": false,
            ""f16_kv"": true,
            ""vocab_only"": false,
            ""use_mmap"": true,
            ""use_mlock"": false,
            ""num_thread"": 8
          }
        }";
        */

        // use with /api/generate
        private static string _Body = @"{
            ""model"": ""gemma:2b"",
            ""prompt"": ""system: you are a helpful AI assistant, always be nice.\nuser: give me a very long overview of the C programming language.\nassistant:"",
            ""stream"": false,
            ""options"": {
            ""num_predict"": 1000,
            ""temperature"": 0.8,
            ""top_p"": 0.9,
            ""repeat_penalty"": 1.1,
            ""stop"": [""\nuser:""],
            ""num_ctx"": 2048,
            ""num_batch"": 512
            }
        }";

        public static async Task Main(string[] args)
        {
            RestResponse restResponse = null;

            try
            {
                using (RestRequest restRequest = new RestRequest(_Url, _Method))
                {
                    restRequest.Logger = Console.WriteLine;

                    if (!String.IsNullOrEmpty(_Body))
                    {
                        Console.WriteLine($"| Attaching content-type {_ContentType} to request to {_Method} {_Url}");
                        restRequest.ContentType = _ContentType;
                    }

                    if (!String.IsNullOrEmpty(_Body))
                    {
                        Console.WriteLine($"| Sending request with body of length {_Body.Length} bytes to {_Method} {_Url}");
                        restResponse = await restRequest.SendAsync(_Body).ConfigureAwait(false);
                    }
                    else
                    {
                        Console.WriteLine($"| Sending request with no body to {_Method} {_Url}");
                        restResponse = await restRequest.SendAsync().ConfigureAwait(false);
                    }

                    Console.WriteLine($"| Request sent to {_Method} {_Url}");

                    if (restResponse == null)
                    {
                        Console.WriteLine($"| No response received from {_Method.ToString()} {_Method} {_Url}");
                        return;
                    }

                    Console.WriteLine($"| Response with status {restResponse.StatusCode} received from {_Method} {_Url}");

                    if (restResponse.StatusCode >= 500)
                    {
                        Console.WriteLine($"| Non-success server error status {restResponse.StatusCode} received from {_Method} {_Url}");
                        return;
                    }

                    Console.WriteLine($"| Status code        : {restResponse.StatusCode}");
                    Console.WriteLine($"| Content type       : {restResponse.ContentType}");
                    Console.WriteLine($"| Content length     : {restResponse.ContentLength}");
                    Console.WriteLine($"| Headers            : {restResponse.Headers.Count}");
                    foreach (string key in restResponse.Headers)
                        Console.WriteLine($"  | {key}: {restResponse.Headers[key]}");

                    Console.WriteLine($"| Server sent events : {restResponse.ServerSentEvents}");
                    Console.WriteLine($"| Chunked transfer   : {restResponse.ChunkedTransferEncoding}");

                    if (restResponse.ServerSentEvents)
                    {
                        Console.WriteLine($"| SSE response received for {_Method} {_Url}");

                        while (true)
                        {
                            ServerSentEvent sse = await restResponse.ReadEventAsync().ConfigureAwait(false);
                            if (sse == null)
                            {
                                Console.WriteLine("  | End of event stream");
                                break;
                            }
                            else
                            {
                                Console.WriteLine($"  | Data: {sse.Data} Event: {sse.Event}");
                            }
                        }
                    }
                    else if (restResponse.ChunkedTransferEncoding)
                    {
                        Console.WriteLine($"| Chunked response received for {_Method} {_Url}");

                        while (true)
                        {
                            ChunkData chunk = await restResponse.ReadChunkAsync().ConfigureAwait(false);
                            if (chunk == null)
                            {
                                Console.WriteLine("  | End of chunk stream (null chunk)");
                                break;
                            }
                            else
                            {
                                Console.WriteLine($"  | Data: {Encoding.UTF8.GetString(chunk.Data)}");
                                if (chunk.IsFinal)
                                {
                                    Console.WriteLine("  | End of chunk stream (chunk marked final)");
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (restResponse.ContentLength > 0)
                        {
                            Console.WriteLine($"  | Data:{Environment.NewLine}{restResponse.DataAsString}");
                        }
                        else
                        {
                            Console.WriteLine($"  | Data: (no data)");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"| Exception:{Environment.NewLine}{e.ToString()}");
            }
            finally
            {
                if (restResponse != null) restResponse.Dispose();
            }
        }

#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
    }
}