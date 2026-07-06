using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using Hop.Api.Configuration;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Services;

public sealed class LineUserBindingService(
    AppDbContext db,
    LineConfigurationResolver lineConfiguration,
    ILineMessagingService lineMessagingService,
    IAuditLogService auditLogService,
    HttpClient httpClient,
    ILogger<LineUserBindingService> logger) : ILineUserBindingService
{
    private static readonly TimeSpan PairingCodeLifetime = TimeSpan.FromMinutes(10);

    public async Task<LineBindingStatusResponse> GetMyBindingStatusAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var binding = await db.LineUserBindings
            .AsNoTracking()
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.BoundAt ?? item.UpdatedAt ?? item.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        var activeCode = await db.LinePairingCodes
            .AsNoTracking()
            .Where(item => item.UserId == userId && item.Status == "Active" && item.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (binding is null)
        {
            var userLineUserId = await db.Users
                .AsNoTracking()
                .Where(item => item.Id == userId && item.IsActive)
                .Select(item => item.LineUserId)
                .FirstOrDefaultAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(userLineUserId))
            {
                return new LineBindingStatusResponse(true, "Bound", MaskLineUserId(userLineUserId), null, null, null, null, activeCode?.ExpiresAt);
            }

            return new LineBindingStatusResponse(false, activeCode is null ? "NotConnected" : "PairingCodeActive", null, null, null, null, null, activeCode?.ExpiresAt);
        }

        return new LineBindingStatusResponse(
            binding.Status == "Bound",
            binding.Status,
            MaskLineUserId(binding.LineUserId),
            binding.DisplayName,
            binding.PictureUrl,
            binding.BoundAt,
            binding.UnboundAt,
            activeCode?.ExpiresAt);
    }

    public async Task<LineMeStatusResponse> GetMyLineStatusAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var status = await GetMyBindingStatusAsync(userId, cancellationToken);
        return new LineMeStatusResponse(
            status.IsBound,
            status.Status,
            status.DisplayName,
            status.PictureUrl,
            status.LineUserIdMasked,
            status.BoundAt,
            status.ExpiresAt);
    }

    public async Task<LinePairingCodeResponse> CreatePairingCodeAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(item => item.Id == userId && item.IsActive, cancellationToken);
        if (user is null)
        {
            throw new InvalidOperationException("User not found.");
        }

        var existingBound = await db.LineUserBindings.AnyAsync(item => item.UserId == userId && item.Status == "Bound", cancellationToken);
        if (!existingBound && !string.IsNullOrWhiteSpace(user.LineUserId))
        {
            existingBound = true;
        }

        if (existingBound)
        {
            throw new InvalidOperationException("บัญชีนี้เชื่อมต่อ LINE แล้ว");
        }

        var activeCodes = await db.LinePairingCodes
            .Where(item => item.UserId == userId && item.Status == "Active" && item.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(cancellationToken);
        foreach (var code in activeCodes)
        {
            code.Status = "Expired";
        }

        var pairingCode = new LinePairingCode
        {
            UserId = userId,
            Code = await GenerateUniqueCodeAsync(cancellationToken),
            Status = "Active",
            ExpiresAt = DateTime.UtcNow.Add(PairingCodeLifetime)
        };
        db.LinePairingCodes.Add(pairingCode);
        await db.SaveChangesAsync(cancellationToken);
        await auditLogService.WriteAsync(userId, "Line.PairingCodeCreated", "LinePairingCode", pairingCode.Id.ToString(), "Created LINE pairing code.", "Success");

        return new LinePairingCodeResponse(
            pairingCode.Code,
            pairingCode.ExpiresAt,
            $"กรุณาพิมพ์รหัส {pairingCode.Code} ใน LINE OA ภายใน 10 นาที");
    }

    public async Task<LineConnectTokenResponse> CreateConnectTokenAsync(Guid userId, string? ipAddress = null, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(item => item.Id == userId && item.IsActive, cancellationToken);
        if (user is null)
        {
            throw new InvalidOperationException("User not found.");
        }

        var existingBound = await db.LineUserBindings.AnyAsync(item => item.UserId == userId && item.Status == "Bound", cancellationToken);
        if (!existingBound && !string.IsNullOrWhiteSpace(user.LineUserId))
        {
            existingBound = true;
        }

        if (existingBound)
        {
            throw new InvalidOperationException("บัญชีนี้เชื่อมต่อ LINE แล้ว");
        }

        var pendingTokens = await db.LineConnectTokens
            .Where(item => item.UserId == userId && item.Status == "Pending" && item.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(cancellationToken);
        foreach (var pending in pendingTokens)
        {
            pending.Status = "Cancelled";
        }

        var shortCode = await GenerateUniqueShortCodeAsync(cancellationToken);
        var token = new LineConnectToken
        {
            UserId = userId,
            Token = await GenerateUniqueSecureTokenAsync(cancellationToken),
            ShortCode = shortCode,
            Status = "Pending",
            ExpiresAt = DateTime.UtcNow.Add(PairingCodeLifetime),
            CreatedByIp = ipAddress,
            Metadata = JsonSerializer.Serialize(new { source = "profile" })
        };
        db.LineConnectTokens.Add(token);
        await db.SaveChangesAsync(cancellationToken);
        await auditLogService.WriteAsync(userId, "Line.ConnectTokenGenerated", "LineConnectToken", token.Id.ToString(), $"Generated LINE connect token {MaskShortCode(shortCode)}.", "Success");

        return new LineConnectTokenResponse(
            token.Token,
            token.ShortCode,
            token.ExpiresAt,
            lineConfiguration.OaAddFriendUrl,
            lineConfiguration.OaAddFriendUrl ?? token.ShortCode);
    }

    public async Task<LineBindingStatusResponse> UnbindAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var binding = await db.LineUserBindings.FirstOrDefaultAsync(item => item.UserId == userId && item.Status == "Bound", cancellationToken);
        var user = await db.Users.FirstOrDefaultAsync(item => item.Id == userId, cancellationToken);
        if (user is null)
        {
            return await GetMyBindingStatusAsync(userId, cancellationToken);
        }

        if (binding is null && !string.IsNullOrWhiteSpace(user.LineUserId))
        {
            binding = await db.LineUserBindings.FirstOrDefaultAsync(item => item.LineUserId == user.LineUserId, cancellationToken);
            if (binding is null)
            {
                binding = new LineUserBinding
                {
                    LineUserId = user.LineUserId,
                    UserId = userId,
                    Status = "Bound",
                    BoundAt = user.UpdatedAt ?? user.CreatedAt,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = DateTime.UtcNow
                };
                db.LineUserBindings.Add(binding);
            }
        }

        if (binding is null)
        {
            return await GetMyBindingStatusAsync(userId, cancellationToken);
        }

        binding.Status = "Unbound";
        binding.UnboundAt = DateTime.UtcNow;
        binding.UpdatedAt = DateTime.UtcNow;
        user.LineUserId = null;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await auditLogService.WriteAsync(userId, "Line.UserUnbound", "LineUserBinding", binding.Id.ToString(), "User unbound LINE account.", "Success");

        return new LineBindingStatusResponse(false, "Unbound", MaskLineUserId(binding.LineUserId), binding.DisplayName, binding.PictureUrl, binding.BoundAt, binding.UnboundAt, null);
    }

    public async Task<LineWebhookHandleResult> HandleFollowAsync(string lineUserId, CancellationToken cancellationToken = default)
    {
        var profile = await TryGetProfileAsync(lineUserId, cancellationToken);
        var binding = await GetOrCreateBindingAsync(lineUserId, cancellationToken);
        binding.DisplayName = profile.DisplayName ?? binding.DisplayName;
        binding.PictureUrl = profile.PictureUrl ?? binding.PictureUrl;
        if (binding.Status != "Bound")
        {
            binding.Status = "Pending";
        }
        binding.LastEventType = "follow";
        binding.LastEventAt = DateTime.UtcNow;
        binding.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await auditLogService.WriteAsync(null, "Line.UserFollowed", "LineUserBinding", binding.Id.ToString(), $"LINE user followed: {MaskLineUserId(lineUserId)}", "Success");

        return new LineWebhookHandleResult("follow", MaskLineUserId(lineUserId), binding.Status, false, "LINE user follow recorded.");
    }

    public async Task<LineWebhookHandleResult> HandleUnfollowAsync(string lineUserId, CancellationToken cancellationToken = default)
    {
        var binding = await db.LineUserBindings.FirstOrDefaultAsync(item => item.LineUserId == lineUserId, cancellationToken);
        if (binding is null)
        {
            return new LineWebhookHandleResult("unfollow", MaskLineUserId(lineUserId), "Ignored", false, "Unknown LINE user.");
        }

        if (binding.UserId is Guid userId)
        {
            var user = await db.Users.FirstOrDefaultAsync(item => item.Id == userId, cancellationToken);
            if (user is not null)
            {
                user.LineUserId = null;
                user.UpdatedAt = DateTime.UtcNow;
            }
        }

        binding.Status = "Unbound";
        binding.UnboundAt = DateTime.UtcNow;
        binding.LastEventType = "unfollow";
        binding.LastEventAt = DateTime.UtcNow;
        binding.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await auditLogService.WriteAsync(null, "Line.UserUnbound", "LineUserBinding", binding.Id.ToString(), $"LINE user unfollowed: {MaskLineUserId(lineUserId)}", "Success");

        return new LineWebhookHandleResult("unfollow", MaskLineUserId(lineUserId), "Unbound", false, "LINE user unfollow recorded.");
    }

    public async Task<LineWebhookHandleResult> HandleMessageAsync(string lineUserId, string messageText, CancellationToken cancellationToken = default)
    {
        var binding = await GetOrCreateBindingAsync(lineUserId, cancellationToken);
        binding.LastEventType = "message";
        binding.LastEventAt = DateTime.UtcNow;
        binding.UpdatedAt = DateTime.UtcNow;

        var code = NormalizePairingCode(messageText);
        if (code is null)
        {
            await db.SaveChangesAsync(cancellationToken);
            return new LineWebhookHandleResult("message", MaskLineUserId(lineUserId), binding.Status, false, "Message ignored.");
        }

        var connectToken = await db.LineConnectTokens
            .Include(item => item.User)
            .FirstOrDefaultAsync(item => item.ShortCode == code && item.Status == "Pending", cancellationToken);
        if (connectToken is not null)
        {
            return await HandleConnectTokenMessageAsync(binding, connectToken, lineUserId, cancellationToken);
        }

        var pairing = await db.LinePairingCodes
            .Include(item => item.User)
            .FirstOrDefaultAsync(item => item.Code == code && item.Status == "Active", cancellationToken);
        if (pairing is null || pairing.ExpiresAt <= DateTime.UtcNow || pairing.User is null || !pairing.User.IsActive)
        {
            binding.Status = binding.Status == "Bound" ? "Bound" : "Pending";
            await db.SaveChangesAsync(cancellationToken);
            await auditLogService.WriteAsync(null, "Line.BindingFailed", "LineUserBinding", binding.Id.ToString(), $"Invalid or expired pairing code for {MaskLineUserId(lineUserId)}.", "Denied");
            await lineMessagingService.SendTestMessageAsync(lineUserId, "ไม่พบรหัสเชื่อมต่อ กรุณาตรวจสอบรหัสอีกครั้ง", "Line.BindingFailed", cancellationToken);
            return new LineWebhookHandleResult("message", MaskLineUserId(lineUserId), binding.Status, false, "Invalid or expired pairing code.");
        }

        if (pairing.ExpiresAt <= DateTime.UtcNow)
        {
            await lineMessagingService.SendTestMessageAsync(lineUserId, "รหัสเชื่อมต่อหมดอายุแล้ว กรุณาสร้างรหัสใหม่ใน HOP", "Line.BindingFailed", cancellationToken);
        }

        var lineAlreadyBound = await db.LineUserBindings.AnyAsync(item =>
            item.LineUserId == lineUserId &&
            item.UserId != null &&
            item.UserId != pairing.UserId &&
            item.Status == "Bound",
            cancellationToken);
        var userAlreadyBound = await db.LineUserBindings.AnyAsync(item =>
            item.UserId == pairing.UserId &&
            item.LineUserId != lineUserId &&
            item.Status == "Bound",
            cancellationToken);
        if (!userAlreadyBound && !string.IsNullOrWhiteSpace(pairing.User.LineUserId) && pairing.User.LineUserId != lineUserId)
        {
            userAlreadyBound = true;
        }

        if (lineAlreadyBound || userAlreadyBound)
        {
            await db.SaveChangesAsync(cancellationToken);
            await auditLogService.WriteAsync(pairing.UserId, "Line.BindingFailed", "LineUserBinding", binding.Id.ToString(), "Duplicate LINE binding attempt.", "Denied");
            return new LineWebhookHandleResult("message", MaskLineUserId(lineUserId), binding.Status, false, "LINE account or HOP user is already bound.");
        }

        var profile = await TryGetProfileAsync(lineUserId, cancellationToken);
        binding.DisplayName = profile.DisplayName ?? binding.DisplayName;
        binding.PictureUrl = profile.PictureUrl ?? binding.PictureUrl;
        binding.UserId = pairing.UserId;
        binding.Status = "Bound";
        binding.BoundAt = DateTime.UtcNow;
        binding.UnboundAt = null;
        binding.UpdatedAt = DateTime.UtcNow;
        pairing.Status = "Used";
        pairing.UsedAt = DateTime.UtcNow;
        pairing.User.LineUserId = lineUserId;
        pairing.User.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await auditLogService.WriteAsync(pairing.UserId, "Line.UserBound", "LineUserBinding", binding.Id.ToString(), $"Bound LINE user {MaskLineUserId(lineUserId)}.", "Success");

        await lineMessagingService.SendTestMessageAsync(lineUserId, "เชื่อมต่อ LINE กับ HOP สำเร็จแล้ว", "Line.UserBound", cancellationToken);

        return new LineWebhookHandleResult("message", MaskLineUserId(lineUserId), "Bound", true, "LINE account bound successfully.");
    }

    private async Task<LineWebhookHandleResult> HandleConnectTokenMessageAsync(LineUserBinding binding, LineConnectToken token, string lineUserId, CancellationToken cancellationToken)
    {
        if (token.ExpiresAt <= DateTime.UtcNow || token.User is null || !token.User.IsActive)
        {
            token.Status = "Expired";
            binding.Status = binding.Status == "Bound" ? "Bound" : "Pending";
            await db.SaveChangesAsync(cancellationToken);
            await auditLogService.WriteAsync(token.UserId, "Line.BindingFailed", "LineConnectToken", token.Id.ToString(), "Expired LINE connect token.", "Denied");
            await lineMessagingService.SendTestMessageAsync(lineUserId, "รหัสเชื่อมต่อหมดอายุแล้ว กรุณาสร้างรหัสใหม่ใน HOP", "Line.BindingFailed", cancellationToken);
            return new LineWebhookHandleResult("message", MaskLineUserId(lineUserId), binding.Status, false, "Expired LINE connect token.");
        }

        var lineAlreadyBound = await db.LineUserBindings.AnyAsync(item =>
            item.LineUserId == lineUserId &&
            item.UserId != null &&
            item.UserId != token.UserId &&
            item.Status == "Bound",
            cancellationToken);
        var userAlreadyBound = await db.LineUserBindings.AnyAsync(item =>
            item.UserId == token.UserId &&
            item.LineUserId != lineUserId &&
            item.Status == "Bound",
            cancellationToken);
        if (!userAlreadyBound && !string.IsNullOrWhiteSpace(token.User.LineUserId) && token.User.LineUserId != lineUserId)
        {
            userAlreadyBound = true;
        }

        if (lineAlreadyBound || userAlreadyBound)
        {
            await db.SaveChangesAsync(cancellationToken);
            await auditLogService.WriteAsync(token.UserId, "Line.BindingFailed", "LineConnectToken", token.Id.ToString(), "Duplicate LINE connect token binding attempt.", "Denied");
            await lineMessagingService.SendTestMessageAsync(lineUserId, "บัญชี LINE หรือบัญชี HOP นี้ถูกเชื่อมต่ออยู่แล้ว", "Line.BindingFailed", cancellationToken);
            return new LineWebhookHandleResult("message", MaskLineUserId(lineUserId), binding.Status, false, "LINE account or HOP user is already bound.");
        }

        var profile = await TryGetProfileAsync(lineUserId, cancellationToken);
        binding.DisplayName = profile.DisplayName ?? binding.DisplayName;
        binding.PictureUrl = profile.PictureUrl ?? binding.PictureUrl;
        binding.UserId = token.UserId;
        binding.Status = "Bound";
        binding.BoundAt = DateTime.UtcNow;
        binding.UnboundAt = null;
        binding.UpdatedAt = DateTime.UtcNow;
        token.Status = "Used";
        token.UsedAt = DateTime.UtcNow;
        token.LineUserId = lineUserId;
        token.User.LineUserId = lineUserId;
        token.User.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await auditLogService.WriteAsync(token.UserId, "Line.UserBound", "LineConnectToken", token.Id.ToString(), $"Bound LINE user {MaskLineUserId(lineUserId)} via one-time connect token.", "Success");

        await lineMessagingService.SendTestMessageAsync(lineUserId, "เชื่อมต่อ LINE กับ HOP สำเร็จแล้ว", "Line.UserBound", cancellationToken);

        return new LineWebhookHandleResult("message", MaskLineUserId(lineUserId), "Bound", true, "LINE account bound successfully.");
    }

    private async Task<LineUserBinding> GetOrCreateBindingAsync(string lineUserId, CancellationToken cancellationToken)
    {
        var binding = await db.LineUserBindings.FirstOrDefaultAsync(item => item.LineUserId == lineUserId, cancellationToken);
        if (binding is not null)
        {
            return binding;
        }

        binding = new LineUserBinding
        {
            LineUserId = lineUserId,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };
        db.LineUserBindings.Add(binding);
        return binding;
    }

    private async Task<string> GenerateUniqueCodeAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var number = RandomNumberGenerator.GetInt32(100000, 999999);
            var code = $"HOP-{number}";
            if (!await db.LinePairingCodes.AnyAsync(item => item.Code == code && item.Status == "Active" && item.ExpiresAt > DateTime.UtcNow, cancellationToken))
            {
                return code;
            }
        }

        throw new InvalidOperationException("Unable to generate LINE pairing code.");
    }

    private async Task<string> GenerateUniqueShortCodeAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var number = RandomNumberGenerator.GetInt32(100000, 1000000);
            var code = $"HOP-{number}";
            var exists = await db.LineConnectTokens.AnyAsync(item => item.ShortCode == code && item.Status == "Pending" && item.ExpiresAt > DateTime.UtcNow, cancellationToken) ||
                await db.LinePairingCodes.AnyAsync(item => item.Code == code && item.Status == "Active" && item.ExpiresAt > DateTime.UtcNow, cancellationToken);
            if (!exists)
            {
                return code;
            }
        }

        throw new InvalidOperationException("Unable to generate LINE connect short code.");
    }

    private async Task<string> GenerateUniqueSecureTokenAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            var token = Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            if (!await db.LineConnectTokens.AnyAsync(item => item.Token == token, cancellationToken))
            {
                return token;
            }
        }

        throw new InvalidOperationException("Unable to generate LINE connect token.");
    }

    private async Task<(string? DisplayName, string? PictureUrl)> TryGetProfileAsync(string lineUserId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(lineConfiguration.AccessToken))
        {
            return (null, null);
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.line.me/v2/bot/profile/{Uri.EscapeDataString(lineUserId)}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", lineConfiguration.AccessToken);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return (null, null);
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(json);
            var displayName = document.RootElement.TryGetProperty("displayName", out var displayNameProperty)
                ? displayNameProperty.GetString()
                : null;
            var pictureUrl = document.RootElement.TryGetProperty("pictureUrl", out var pictureUrlProperty)
                ? pictureUrlProperty.GetString()
                : null;
            return (displayName, pictureUrl);
        }
        catch (Exception ex)
        {
            logger.LogInformation(ex, "Unable to fetch LINE profile for masked user {LineUserId}.", MaskLineUserId(lineUserId));
            return (null, null);
        }
    }

    private static string? NormalizePairingCode(string message)
    {
        var text = message.Trim().ToUpperInvariant();
        return text.StartsWith("HOP-", StringComparison.Ordinal) && text.Length is >= 10 and <= 12
            ? text
            : null;
    }

    public static string MaskLineUserId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "-";
        }

        return value.Length <= 10 ? "U********" : $"{value[..5]}...{value[^4..]}";
    }

    private static string MaskShortCode(string value)
    {
        return value.Length <= 4 ? "****" : $"{value[..4]}***";
    }
}
