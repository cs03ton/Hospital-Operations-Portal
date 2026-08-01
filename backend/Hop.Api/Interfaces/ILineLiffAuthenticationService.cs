using Hop.Api.DTOs;

namespace Hop.Api.Interfaces;

public interface ILineLiffAuthenticationService
{
    Task<VerifiedLineIdentity> VerifyIdTokenAsync(string idToken, CancellationToken cancellationToken = default);
}
