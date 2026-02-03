namespace CloseExpAISolution.Domain.Enums;

/// <summary>
/// File upload states (for cloud storage)
/// </summary>
public enum UploadState
{
    /// <summary>
    /// Upload initiated, waiting for processing
    /// </summary>
    Pending,

    /// <summary>
    /// File successfully uploaded to cloud storage
    /// </summary>
    Uploaded,

    /// <summary>
    /// Upload failed
    /// </summary>
    Failed
}
