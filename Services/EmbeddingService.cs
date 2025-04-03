using System.Net.Http.Headers;
using System.Text.Json.Serialization;

namespace PicQuest.Services;

public class EmbeddingService
{
    private readonly HttpClient _httpClient;

    public EmbeddingService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        var apiKey = configuration["AI:ApiKey"] ?? "sk-vqssldevnqrmqhcxnkmxgjlktehaafsnitsifkimuqxxgyhr";

        _httpClient.BaseAddress = new Uri("https://api.siliconflow.cn");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
    }

    public async Task<float[]> GetEmbeddingAsync(string text)
    {
        try
        {
            var requestContent = new
            {
                model = "Pro/BAAI/bge-m3",
                input = text,
                encoding_format = "float"
            };

            var response = await _httpClient.PostAsJsonAsync("/v1/embeddings", requestContent);
            response.EnsureSuccessStatusCode();

            var embedResult = await response.Content.ReadFromJsonAsync<EmbeddingResponse>();
            if (embedResult?.Data == null || embedResult.Data.Length == 0)
            {
                Console.WriteLine("嵌入向量API返回空结果");
                return [];
            }

            return embedResult.Data[0].Embedding;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"获取嵌入向量时出错: {ex.Message}");
            return [];
        }
    }

    private record EmbeddingResponse
    {
        [JsonPropertyName("data")] public EmbeddingData[] Data { get; set; } = [];
    }

    private record EmbeddingData
    {
        [JsonPropertyName("embedding")] public float[] Embedding { get; set; } = [];
    }
}