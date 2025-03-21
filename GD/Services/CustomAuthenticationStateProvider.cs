using Blazored.LocalStorage;
using GD.Shared.Common;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private const string jwtKey = "JWT";

    private readonly NavigationManager navigationManager;
    private readonly ILocalStorageService localStorage;
    private readonly HttpClient httpClient;

    public CustomAuthenticationStateProvider(HttpClient httpClient, NavigationManager navigationManager,
        ILocalStorageService localStorage)
    {
        this.httpClient = httpClient;
        this.navigationManager = navigationManager;
        this.localStorage = localStorage;
    }

    /// <summary>
    /// Формирует состояние на основании токена из LocalStorage
    /// </summary>
    /// <returns></returns>
    public async override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await localStorage.GetItemAsStringAsync(jwtKey);

        if (token == null)
        {
            return await LogoutState();
        }

        var claims = TryReadToken(token);
        if (claims == null)
        {
            return await LogoutState();
        };

        if (ExpirationTimeIsValid(token) == false)
        {
            return await LogoutState();
        }

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);


        var claimsIdentity = new ClaimsIdentity(claims, nameof(CustomAuthenticationStateProvider),
            GDUserClaimTypes.UserName, GDUserClaimTypes.Roles);


        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        var newAuthState = new AuthenticationState(claimsPrincipal);

        return newAuthState;
    }


    private async Task<AuthenticationState> LogoutState()
    {
        var logoutState = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        await localStorage.RemoveItemAsync(jwtKey);
        return logoutState;
    }


    /// <summary>
    /// Попытаться получить claim из токена
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    private IEnumerable<Claim>? TryReadToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        try
        {
            var jwtToken = tokenHandler.ReadJwtToken(token).Claims;
            return jwtToken;
        }
        catch (Exception)
        {
            return null;
        }
    }


    /// <summary>
    /// Проверка не истек ли время жизни токена
    /// </summary>
    /// <param name="claims"></param>
    /// <returns></returns>
    private bool ExpirationTimeIsValid(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        tokenHandler.ReadJwtToken(token);
        if (tokenHandler.TokenLifetimeInMinutes < 1)
        {
            return false;
        }

        return true;
    }
}