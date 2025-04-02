using Microsoft.AspNetCore.Http;
using PicQuest.Models;

namespace PicQuest.Services;

public interface IPictureService
{
    Task<IEnumerable<object>> GetPicturesAsync();
    Task<(object Picture, int Id)> UploadPictureAsync(IFormFile file);
    
    // 添加一个新方法处理从Blazor组件上传的文件
    Task<(object Picture, int Id)> UploadPictureAsync(string fileName, Stream fileStream, string contentType);
    
    Task<IEnumerable<object>> SearchPicturesByTextAsync(string query, int limit = 8);
}
