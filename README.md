# PicQuest

PicQuest 是一个基于 .NET 9 开发的智能图像检索与管理系统，利用先进的 AI 视觉模型和向量嵌入技术，提供高效的图像搜索和管理功能。

## 项目概述

PicQuest 通过结合现代 AI 技术与高效的向量数据库，实现了以下功能：

- 智能图像分析：自动为上传的图片生成标题和描述
- 语义检索：基于自然语言的相似度搜索
- 高性能存储：支持向量索引的快速检索
- 响应式界面：优雅的用户交互体验

## 技术栈

- **前端框架**：Blazor Server + MudBlazor
- **后端**：ASP.NET Core 9
- **数据库**：PostgreSQL + pgvector 扩展
- **AI 技术**：深度学习视觉模型 (Deepseek-VL) + 向量嵌入 (BGE-M3)
- **图像处理**：ImageSharp

## 功能特点

### 智能内容识别

系统会自动分析上传的图片内容，生成描述性标题和详细描述，无需手动输入。

### 语义搜索能力

只需输入自然语言描述，即可找到语义相关的图片，而不仅限于关键词匹配。

### 批量上传处理

支持多图片同时上传，自动生成缩略图并进行 AI 分析。

### 实时进度反馈

上传过程中提供实时进度反馈，增强用户体验。

## 安装指南

### 环境要求

- .NET 9 SDK
- PostgreSQL 14+（需安装 pgvector 扩展）
- AI API 访问密钥（默认使用 Silicon Flow API）

### 安装步骤

1. 克隆仓库
```bash
git clone https://github.com/DrizzleTime/PicQuest.git
cd PicQuest
```

2. 安装 PostgreSQL 并配置 pgvector
```bash
# 安装 pgvector 扩展
CREATE EXTENSION vector;
```

3. 配置数据库连接和 API 密钥
   编辑 `appsettings.json` 文件，设置数据库连接字符串和 AI API 密钥：
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=picquest;Username=postgres;Password=yourpassword"
  },
  "AI": {
    "ApiKey": "your-api-key",
    "Model": "deepseek-ai/deepseek-vl2"
  }
}
```

4. 运行应用
```bash
dotnet run
```

## 使用说明

### 上传图片
1. 点击主页面的"上传图片"按钮
2. 选择一张或多张图片（最大 20 张）
3. 系统会自动处理图片、生成缩略图并分析内容
4. 完成后，图片会显示在图库中

### 搜索图片
1. 在搜索框中输入描述性文本（如"猫猫"、"采茶的少女"等）
2. 点击搜索按钮或按回车
3. 系统将显示与搜索内容语义相关的图片，并按相似度排序

## 贡献指南

欢迎提交 Pull Request 或 Issue 来帮助改进 PicQuest！

