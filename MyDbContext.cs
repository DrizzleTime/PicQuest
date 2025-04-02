using Microsoft.EntityFrameworkCore;
using PicQuest.Models;

namespace PicQuest;

public class MyDbContext(DbContextOptions<MyDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");
        
        modelBuilder.Entity<Picture>()
            .HasIndex(p => p.Embedding)
            .HasMethod("ivfflat")  // 或者使用 "hnsw" 算法
            .HasOperators("vector_cosine_ops")  // 使用余弦距离运算符
            .HasStorageParameter("lists", 100);  // IVF参数: 列表数量
    }
    
    public DbSet<Picture> Pictures { get; set; } = null!;
}