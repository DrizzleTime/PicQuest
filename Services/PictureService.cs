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
    public async Task<PaginatedResult<PictureViewModel>> GetPicturesAsync(int page = 1, int pageSize = 8)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 8;

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var totalCount = await dbContext.Pictures.CountAsync();

        var pictures = await dbContext.Pictures
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PictureViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Path = p.Path,
                ThumbnailPath = p.ThumbnailPath,
                Description = p.Description,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .ToListAsync();

        return new PaginatedResult<PictureViewModel>
        {
            Items = pictures,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    // 保留这个统一的上传方法
    public async Task<(PictureViewModel Picture, int Id)> UploadPictureAsync(string fileName, Stream fileStream,
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
        await using (var fileStreamOutput = new FileStream(filePath, FileMode.Create))
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

        // 返回使用 PictureViewModel 的图片信息
        var pictureResponse = new PictureViewModel
        {
            Id = picture.Id,
            Name = picture.Name,
            Path = picture.Path,
            ThumbnailPath = picture.ThumbnailPath,
            Description = picture.Description,
            CreatedAt = picture.CreatedAt,
            UpdatedAt = picture.UpdatedAt
        };

        return (pictureResponse, picture.Id);
    }

    public async Task<PaginatedResult<PictureViewModel>> SearchPicturesByTextAsync(string query, int page = 1,
        int pageSize = 8, double similarityThreshold = 0.36)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 8;

        // 为搜索查询生成嵌入向量
        var queryEmbedding = await embeddingService.GetEmbeddingAsync(query);
        var queryVector = new Vector(queryEmbedding);

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        // 获取所有相似度高于阈值的图片
        var allResults = await dbContext.Pictures
            .Where(p => p.Embedding != null)
            .Select(p => new
            {
                Picture = p,
                Similarity = 1.0 - p.Embedding!.CosineDistance(queryVector)
            })
            .Where(p => p.Similarity >= similarityThreshold)
            .OrderByDescending(p => p.Similarity)
            .ToListAsync();

        // 计算总数并分页
        var totalCount = allResults.Count;
        var paginatedResults = allResults
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select<dynamic, PictureViewModel>(r => new PictureViewModel
            {
                Id = r.Picture.Id,
                Name = r.Picture.Name,
                Path = r.Picture.Path,
                ThumbnailPath = r.Picture.ThumbnailPath,
                Description = r.Picture.Description,
                CreatedAt = r.Picture.CreatedAt,
                UpdatedAt = r.Picture.UpdatedAt,
                Similarity = r.Similarity
            })
            .ToList();

        return new PaginatedResult<PictureViewModel>
        {
            Items = paginatedResults,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
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
        byte[] imageBytes = await File.ReadAllBytesAsync(imagePath);
        return Convert.ToBase64String(imageBytes);
    }
}