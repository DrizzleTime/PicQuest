using Microsoft.AspNetCore.Mvc;
using PicQuest.Services;

namespace PicQuest.Controllers;

[ApiController]
[Route("[controller]")]
public class PictureController(IPictureService pictureService) : ControllerBase
{
    [HttpGet("pictures")]
    public async Task<IActionResult> GetPictures()
    {
        var pictures = await pictureService.GetPicturesAsync();
        return Ok(pictures);
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadPicture(IFormFile file)
    {
        if (file.Length == 0)
            return BadRequest("No file was uploaded.");
            
        try
        {
            var (picture, id) = await pictureService.UploadPictureAsync(file);
            return CreatedAtAction(nameof(GetPictures), new { id }, picture);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchPicturesByText([FromQuery] string query, [FromQuery] int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest("搜索查询不能为空");

        try
        {
            var neighbors = await pictureService.SearchPicturesByTextAsync(query, limit);
            return Ok(neighbors);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"搜索图片时发生错误: {ex.Message}");
        }
    }
}