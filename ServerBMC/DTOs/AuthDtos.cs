namespace ServerBMC.DTOs;

public record LoginRequest(string Email, string Password);

public record RegisterRequest(string Email, string Password, string FullName, string? Phone, List<string> RoleCodes);

public record AuthResponse(string AccessToken, DateTime ExpiresAt, UserInfo User);

public record UserInfo(int Id, string Email, string FullName, string? Phone, string? Avatar, List<string> Roles);