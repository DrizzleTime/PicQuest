using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using PicQuest.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace PicQuest.Services;

public class PictureService(
    IDbContextFactory<MyDbContext> contextFactory,
    AiVisionService aiService,
    EmbeddingService embeddingService)
    : IPictureService
{
    public async Task<IEnumerable<object>> GetPicturesAsync()
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        return await dbContext.Pictures
            .OrderByDescending(p => p.CreatedAt)
            .Take(8)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Path,
                p.ThumbnailPath,
                p.Description,
                p.CreatedAt,
                p.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<(object Picture, int Id)> UploadPictureAsync(IFormFile file)
    {
        string fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        string thumbnailFileName =
            $"{Path.GetFileNameWithoutExtension(fileName)}_thumb{Path.GetExtension(fileName)}";

        // 按年月创建目录结构
        string currentDate = DateTime.Now.ToString("yyyy/MM");
        string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", currentDate);
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var filePath = Path.Combine(uploadsFolder, fileName);
        var thumbnailPath = Path.Combine(uploadsFolder, thumbnailFileName);

        // 保存原始图片
        await using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(fileStream);
        }

        // 创建并保存缩略图
        await CreateThumbnailAsync(filePath, thumbnailPath, 500);

        // 将缩略图转换为Base64并调用AI分析
        string base64Image = await ConvertImageToBase64(thumbnailPath);
        var (title, description) = await aiService.AnalyzeImageAsync(base64Image);

        // 确定最终标题和描述
        string finalTitle = !string.IsNullOrWhiteSpace(title) && title != "AI生成的标题"
            ? title
            : Path.GetFileNameWithoutExtension(file.FileName);

        string finalDescription = !string.IsNullOrWhiteSpace(description) && description != "AI生成的描述"
            ? description
            : $"Uploaded on {DateTime.UtcNow}";

        // 生成嵌入向量 - 使用标题和描述的组合
        var combinedText = $"{finalTitle}. {finalDescription}";
        var embedding = await embeddingService.GetEmbeddingAsync(combinedText);

        // 保存到数据库 - 使用按年月组织的路径
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var picture = new Picture
        {
            Name = finalTitle,
            Description = finalDescription,
            Path = $"/uploads/{currentDate}/{fileName}",
            ThumbnailPath = $"/uploads/{currentDate}/{thumbnailFileName}",
            Embedding = new Vector(embedding)
        };
        dbContext.Pictures.Add(picture);
        await dbContext.SaveChangesAsync();

        // 返回不包含Embedding的图片信息
        var pictureResponse = new
        {
            picture.Id,
            picture.Name,
            picture.Path,
            picture.ThumbnailPath,
            picture.Description,
            picture.CreatedAt,
            picture.UpdatedAt
        };

        return (pictureResponse, picture.Id);
    }

    // 添加新方法以处理从Blazor组件上传的文件
    public async Task<(object Picture, int Id)> UploadPictureAsync(string fileName, Stream fileStream,
        string contentType)
    {
        string fileExtension = Path.GetExtension(fileName);
        string newFileName = $"{Guid.NewGuid()}{fileExtension}";
        string thumbnailFileName = $"{Path.GetFileNameWithoutExtension(newFileName)}_thumb{fileExtension}";

        // 按年月创建目录结构
        string currentDate = DateTime.Now.ToString("yyyy/MM");
        string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", currentDate);
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var filePath = Path.Combine(uploadsFolder, newFileName);
        var thumbnailPath = Path.Combine(uploadsFolder, thumbnailFileName);

        // 保存原始图片
        using (var fileStreamOutput = new FileStream(filePath, FileMode.Create))
        {
            await fileStream.CopyToAsync(fileStreamOutput);
        }

        // 重置流位置以便读取
        if (fileStream.CanSeek)
            fileStream.Position = 0;

        // 创建并保存缩略图
        await CreateThumbnailAsync(filePath, thumbnailPath, 500);

        // 将缩略图转换为Base64并调用AI分析
        string base64Image = await ConvertImageToBase64(thumbnailPath);
        var (title, description) = await aiService.AnalyzeImageAsync(base64Image);

        // 确定最终标题和描述
        string finalTitle = !string.IsNullOrWhiteSpace(title) && title != "AI生成的标题"
            ? title
            : Path.GetFileNameWithoutExtension(fileName);

        string finalDescription = !string.IsNullOrWhiteSpace(description) && description != "AI生成的描述"
            ? description
            : $"Uploaded on {DateTime.UtcNow}";

        // 生成嵌入向量 - 使用标题和描述的组合
        var combinedText = $"{finalTitle}. {finalDescription}";
        var embedding = await embeddingService.GetEmbeddingAsync(combinedText);

        // 保存到数据库 - 使用按年月组织的路径
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var picture = new Picture
        {
            Name = finalTitle,
            Description = finalDescription,
            Path = $"/uploads/{currentDate}/{newFileName}",
            ThumbnailPath = $"/uploads/{currentDate}/{thumbnailFileName}",
            Embedding = new Vector(embedding)
        };
        dbContext.Pictures.Add(picture);
        await dbContext.SaveChangesAsync();

        // 返回不包含Embedding的图片信息
        var pictureResponse = new
        {
            picture.Id,
            picture.Name,
            picture.Path,
            picture.ThumbnailPath,
            picture.Description,
            picture.CreatedAt,
            picture.UpdatedAt
        };

        return (pictureResponse, picture.Id);
    }

    public async Task<IEnumerable<object>> SearchPicturesByTextAsync(string query, int limit = 8)
    {
        // 为搜索查询生成嵌入向量
        var queryEmbedding = await embeddingService.GetEmbeddingAsync(query);
        var queryVector = new Vector(queryEmbedding);

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        return await dbContext.Pictures
            .Where(p => p.Embedding != null)
            .OrderBy(p => p.Embedding!.CosineDistance(queryVector))
            .Take(limit)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Path,
                p.ThumbnailPath,
                p.Description,
                p.CreatedAt,
                p.UpdatedAt,
                Similarity = 1.0 - p.Embedding!.CosineDistance(queryVector)
            })
            .ToListAsync();
    }

    private async Task CreateThumbnailAsync(string originalPath, string thumbnailPath, int width)
    {
        using var image = await Image.LoadAsync(originalPath);

        // 保持宽高比进行调整大小
        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(width, 0), // 宽度设为指定值，高度自动计算以保持宽高比
            Mode = ResizeMode.Max
        }));

        // 保存缩略图
        await image.SaveAsync(thumbnailPath);
    }

    private async Task<string> ConvertImageToBase64(string imagePath)
    {
        byte[] imageBytes = await System.IO.File.ReadAllBytesAsync(imagePath);
        return Convert.ToBase64String(imageBytes);
    }
}