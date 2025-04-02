using System.ComponentModel.DataAnnotations.Schema;
using Pgvector;

namespace PicQuest.Models;

public class Picture : BaseModel
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string ThumbnailPath { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    [Column(TypeName = "vector(1024)")]
    public Vector? Embedding { get; set; }
}