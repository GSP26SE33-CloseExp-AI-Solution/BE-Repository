namespace CloseExpAISolution.Application.Services.Interface;

public interface IR2StorageService
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType);
    string GeneratePreSignedUrl(string bucket, string key, TimeSpan expiry);
    Task<object?> GetFileInfoAsync(int id);
    Task<Stream> DownloadFileAsync(string fileName);
    Task<Stream> DownloadFileByKeyAsync(string key);
    Task<(byte[] FileBytes, string FileName)> DownloadLatestFileAsync();
}