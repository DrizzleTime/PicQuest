using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using PicQuest.Components.Dialogs;
using PicQuest.Models;

namespace PicQuest.Components.Pages;

public partial class Home : ComponentBase
{
    private string _searchQuery = "";
    private PaginatedResult<PictureViewModel>? _paginatedPictures;
    private bool _isUploading;
    private bool _isSearching;
    private bool _isSearchMode;
    private int _uploadProgress;
    private int _currentFileIndex;
    private int _totalFilesToUpload;
    private int _currentPage = 1;
    private readonly int _pageSize = 8;
    private const int MaxAllowedFiles = 20;
    private const long MaxFileSize = 10485760;
    private double _similarityThreshold = 0.36;
    private const double DefaultSimilarityThreshold = 0.36;

    protected override async Task OnInitializedAsync()
    {
        await LoadPictures(_currentPage, _pageSize);
    }

    private async Task LoadPictures(int page = 1, int pageSize = 8)
    {
        try
        {
            _isSearching = true;
            StateHasChanged();

            var result = await PictureService.GetPicturesAsync(page, pageSize);
            _paginatedPictures = result;
            _isSearchMode = false;
            _currentPage = page;
        }
        catch (Exception ex)
        {
            Snackbar.Add($"加载图片失败: {ex.Message}", Severity.Error);
            _paginatedPictures = new PaginatedResult<PictureViewModel>
            {
                Items = new List<PictureViewModel>(),
                Page = page,
                PageSize = pageSize,
                TotalCount = 0
            };
        }
        finally
        {
            _isSearching = false;
        }
    }

    private async Task OnFilesSelected(InputFileChangeEventArgs e)
    {
        try
        {
            _isUploading = true;
            var files = e.GetMultipleFiles(MaxAllowedFiles);
            _totalFilesToUpload = files.Count;

            if (files.Count == 0) return;

            _currentFileIndex = 0;
            _uploadProgress = 0;

            int successCount = 0;
            long totalBytes = files.Sum(f => f.Size);
            long uploadedBytes = 0;

            foreach (var file in files)
            {
                try
                {
                    if (file.Size > MaxFileSize)
                    {
                        Snackbar.Add($"文件过大：{file.Name}", Severity.Warning);
                        continue;
                    }

                    await using var stream = file.OpenReadStream(maxAllowedSize: MaxFileSize);
                    using var memoryStream = new MemoryStream();
                    var buffer = new byte[16 * 1024];
                    int bytesRead;

                    while ((bytesRead = await stream.ReadAsync(buffer)) != 0)
                    {
                        await memoryStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                        // 更新当前文件和总体进度
                        uploadedBytes += bytesRead;

                        // 计算总体进度百分比
                        _uploadProgress = (int)((double)uploadedBytes / totalBytes * 100);
                        // 通知UI更新
                        StateHasChanged();
                        await Task.Delay(10);
                    }

                    memoryStream.Position = 0;
                    await PictureService.UploadPictureAsync(file.Name, memoryStream, file.ContentType);
                    successCount++;
                }
                catch (Exception ex)
                {
                    Snackbar.Add($"上传失败：{file.Name} - {ex.Message}", Severity.Error);
                }

                _currentFileIndex++;
                _uploadProgress = (int)((double)_currentFileIndex / _totalFilesToUpload * 100);
                StateHasChanged();
            }

            await LoadPictures();

            if (successCount > 0)
            {
                Snackbar.Add($"成功上传 {successCount} 个图片", Severity.Success);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"上传过程中发生错误: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isUploading = false;
            _uploadProgress = 0;
        }
    }

    private async Task SearchPictures(int page = 1)
    {
        if (string.IsNullOrWhiteSpace(_searchQuery))
        {
            await LoadPictures();
            return;
        }

        try
        {
            _isSearching = true;
            StateHasChanged();

            await Task.Delay(300);

            var result = await PictureService.SearchPicturesByTextAsync(_searchQuery, page, _pageSize, _similarityThreshold);
            _paginatedPictures = result;
            _isSearchMode = true;
            _currentPage = page;

            if (!_paginatedPictures.Items.Any() && page == 1)
            {
                Snackbar.Add($"未找到与\"{_searchQuery}\"相关的图片", Severity.Info);
            }
            else if (page == 1)
            {
                Snackbar.Add($"找到 {_paginatedPictures.TotalCount} 个相关图片", Severity.Success);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"搜索失败: {ex.Message}", Severity.Error);
            _paginatedPictures = new PaginatedResult<PictureViewModel>
            {
                Items = new List<PictureViewModel>(),
                Page = page,
                PageSize = _pageSize,
                TotalCount = 0
            };
        }
        finally
        {
            _isSearching = false;
        }
    }

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !_isSearching)
        {
            await SearchPictures();
        }
    }

    private async Task ShowImageDetails(PictureViewModel picture)
    {
        var parameters = new DialogParameters
        {
            ["Picture"] = picture,
            ["IsSearchResult"] = _isSearchMode
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.ExtraLarge,
            FullWidth = true,
        };

        await DialogService.ShowAsync<PictureDetailDialog>("图片详情", parameters, options);
    }

    private void ResetSimilarityThreshold()
    {
        _similarityThreshold = DefaultSimilarityThreshold;
    }

    private async Task OnPageChanged(int page)
    {
        if (page == _currentPage) return;

        if (_isSearchMode)
        {
            await SearchPictures(page);
        }
        else
        {
            await LoadPictures(page, _pageSize);
        }
    }

    private async Task ResetSearch()
    {
        _searchQuery = "";
        await LoadPictures(1, _pageSize);
    }

}