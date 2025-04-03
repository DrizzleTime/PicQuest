using System.Net.Http.Headers;
using System.Text.Json.Serialization;

namespace PicQuest.Services;

public class AiVisionService
{
    private readonly HttpClient _httpClient;
    private readonly string _model;

    public AiVisionService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        var apiKey = configuration["AI:ApiKey"] ?? "sk-vqssldevnqrmqhcxnkmxgjlktehaafsnitsifkimuqxxgyhr";
        _model = configuration["AI:Model"] ?? "deepseek-ai/deepseek-vl2";
        _httpClient.BaseAddress = new Uri("https://api.siliconflow.cn");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
    }

    public async Task<(string title, string description)> AnalyzeImageAsync(string base64Image)
    {
        try
        {
            var requestContent = new
            {
                model = _model,
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new
                            {
                                type = "image_url",
                                image_url = new
                                {
                                    url = $"data:image/jpeg;base64,{base64Image}"
                                }
                            },
                            new
                            {
                                type = "text",
                                text = "请详细分析这张图片，并提供全面的描述，以便用于向量嵌入和基于文本的图像搜索。描述需要包含：主体对象、场景环境、色彩特点、构图布局、风格特征、情绪氛围、细节特征等关键元素。请先给出一个简短有力的标题，然后提供详细描述。必须严格按照以下格式返回，不要加入任何其他文字：\n标题：[简短概括图片的核心内容]\n描述：[全面详细的描述，包含上述所有元素，使用丰富精确的词汇，避免笼统表达]"
                            }
                        }
                    }
                },
                stream = false,
                max_tokens = 800,  // 增加最大令牌数以获取更详细的描述
                temperature = 0.5,  // 降低温度以获得更精确的描述
                top_p = 0.8,
                top_k = 50
            };

            var response = await _httpClient.PostAsJsonAsync("/v1/chat/completions", requestContent);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadFromJsonAsync<AiResponse>();
            if (responseContent?.Choices == null || responseContent.Choices.Length == 0)
            {
                return ("未能获取标题", "未能获取描述");
            }

            var aiMessage = responseContent.Choices[0].Message.Content;
            return ExtractTitleAndDescription(aiMessage);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"AI分析图片时出错: {ex.Message}");
            return ("处理失败", $"AI分析过程中发生错误: {ex.Message}");
        }
    }

    private (string title, string description) ExtractTitleAndDescription(string aiResponse)
    {
        string title = "AI生成的标题";
        string description = "AI生成的描述";

        try
        {
            // 使用更强大的解析逻辑
            var titleMarker = "标题：";
            var descMarker = "描述：";

            var titleIndex = aiResponse.IndexOf(titleMarker);
            var descIndex = aiResponse.IndexOf(descMarker);

            if (titleIndex >= 0 && descIndex > titleIndex)
            {
                titleIndex += titleMarker.Length;
                var titleEndIndex = descIndex;
                title = aiResponse[titleIndex..titleEndIndex].Trim();

                descIndex += descMarker.Length;
                description = aiResponse[descIndex..].Trim();
            }
            else if (titleIndex >= 0)
            {
                // 如果只找到标题
                titleIndex += titleMarker.Length;
                title = aiResponse[titleIndex..].Trim();
            }
            else if (descIndex >= 0)
            {
                // 如果只找到描述
                descIndex += descMarker.Length;
                description = aiResponse[descIndex..].Trim();
            }
            else
            {
                // 如果都没找到，将整个响应作为描述
                description = aiResponse.Trim();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"解析AI响应时出错: {ex.Message}");
            description = $"原始AI响应: {aiResponse}";
        }

        return (title, description);
    }

    // 响应类
    private class AiResponse
    {
        [JsonPropertyName("choices")] public Choice[] Choices { get; set; } = Array.Empty<Choice>();
    }

    private class Choice
    {
        [JsonPropertyName("message")] public Message Message { get; set; } = new Message();
    }

    private class Message
    {
        [JsonPropertyName("content")] public string Content { get; set; } = string.Empty;
    }
}