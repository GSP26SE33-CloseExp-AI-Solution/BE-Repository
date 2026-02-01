namespace CloseExpAISolution.Domain.Entities;

/// <summary>
/// Refresh Token entity for JWT token refresh flow
/// </summary>
public class RefreshToken
{
    public Guid RefreshTokenId { get; set; }

    /// <summary>User who owns this refresh token</summary>
    public Guid UserId { get; set; }
    public User? User { get; set; }

    /// <summary>The refresh token string (hashed or plain)</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>When the token expires</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>When the token was created</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>When the token was revoked (null if still active)</summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>If rotated, the new token that replaced this one</summary>
    public string? ReplacedByToken { get; set; }

    /// <summary>Device/browser info for tracking sessions</summary>
    public string? DeviceInfo { get; set; }

    /// <summary>IP address when token was created</summary>
    public string? IpAddress { get; set; }

    // Computed properties
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt != null;
    public bool IsActive => !IsRevoked && !IsExpired;
}
