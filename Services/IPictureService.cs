using Microsoft.AspNetCore.Http;
using PicQuest.Models;

namespace PicQuest.Services;

public interface IPictureService
{
    Task<PaginatedResult<PictureViewModel>> GetPicturesAsync(int page = 1, int pageSize = 8);
    
    Task<(PictureViewModel Picture, int Id)> UploadPictureAsync(string fileName, Stream fileStream, string contentType);
    
    Task<PaginatedResult<PictureViewModel>> SearchPicturesByTextAsync(string query, int page = 1, int pageSize = 8, double similarityThreshold = 0.36);
}
