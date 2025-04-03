using System;
using System.Collections.Generic;

namespace PicQuest.Models;

public class PictureViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string ThumbnailPath { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public double? Similarity { get; set; }
    
    public string ImagePath => Path;
}
