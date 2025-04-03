using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Pgvector;

namespace PicQuest.Models;

public class Picture : BaseModel
{
    [StringLength(255)] public string Name { get; init; } = string.Empty;

    [StringLength(1024)] public string Path { get; init; } = string.Empty;

    [StringLength(1024)] public string ThumbnailPath { get; init; } = string.Empty;

    [StringLength(2000)] public string Description { get; init; } = string.Empty;

    [Column(TypeName = "vector(1024)")] public Vector? Embedding { get; init; }
}