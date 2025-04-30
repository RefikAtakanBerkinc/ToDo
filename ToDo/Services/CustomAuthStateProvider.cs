using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ToDo.Models.Dto;

namespace ToDo.Services
{
    public class CustomAuthStateProvider: AuthenticationStateProvider
    {

        private readonly IJSRuntime _jsRuntime;
        private readonly IConfiguration _configuration;
        private readonly IAuthService _authService;

        public CustomAuthStateProvider(
            IJSRuntime jsRuntime,
            IConfiguration configuration,
            IAuthService authService) // AuthService'i injection et
        {
            _jsRuntime = jsRuntime;
            _configuration = configuration;
            _authService = authService; // Servisi ata
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                // Token'ı güvenli bir şekilde al
                var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "accessToken");

                // Token yoksa kimliksiz durum
                if (string.IsNullOrEmpty(token))
                {
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }

                // Token doğrulama
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);

                // Token süresi kontrolü
                if (jwtToken.ValidTo < DateTime.UtcNow)
                {
                    // Token süresi dolmuşsa refresh token ile yenile
                    var refreshResult = await RefreshTokenAsync();

                    if (!refreshResult)
                    {
                        // Refresh başarısız olduysa kimliksiz durum
                        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                    }

                    // Yeniden token al
                    token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "accessToken");
                    jwtToken = tokenHandler.ReadJwtToken(token);
                }

                // Claims oluşturma
                var claims = jwtToken.Claims.ToList();

                // Eğer rol claim'i eksikse default rol ekle
                if (!claims.Any(c => c.Type == ClaimTypes.Role))
                {
                    claims.Add(new Claim(ClaimTypes.Role, "User"));
                }

                // Kimlik oluşturma
                var identity = new ClaimsIdentity(claims, "jwt");
                var user = new ClaimsPrincipal(identity);

                return new AuthenticationState(user);
            }
            catch
            {
                // Herhangi bir hata durumunda kimliksiz durum
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }
        }

        // Refresh Token Metodu
        public async Task<bool> RefreshTokenAsync()
        {
            try
            {
                // Gerekli tokenleri ve kullanıcı bilgisini al
                var refreshToken = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "refreshToken");
                var userIdString = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "userId");

                // Gerekli bilgiler eksikse false döndür
                if (string.IsNullOrEmpty(refreshToken) ||
                    string.IsNullOrEmpty(userIdString))
                {
                    return false;
                }

                // Refresh token isteği oluştur
                var refreshTokenRequest = new RefreshTokenRequestDto
                {
                    UserId = Guid.Parse(userIdString),
                    RefreshToken = refreshToken
                };

                // Token yenileme işlemi
                var tokenResponse = await _authService.RefreshTokenAsync(refreshTokenRequest);

                // Yenileme başarısız olduysa
                if (tokenResponse == null)
                {
                    return false;
                }

                // Yeni tokenları kaydet
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "accessToken", tokenResponse.AccessToken);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "refreshToken", tokenResponse.RefreshToken);

                // Authentication state değişikliğini bildir
                NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

                return true;
            }
            catch
            {
                return false;
            }
        }

        // Authentication state değişikliğini bildiren metot
        public void NotifyAuthenticationStateChanged()
        {
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }
    }
}
