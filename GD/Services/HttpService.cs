
using GD.Shared.Common;
using GD.Shared.Response;
using MudBlazor;
using System.Net;
using System.Net.Http.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace GD.Services
{
    public struct Unit
    {
    }

    public class HttpService
    {
        private readonly HttpClient httpClient;
        private readonly ISnackbar snack;
        private readonly ILogger<HttpService> logger;


        public HttpService(HttpClient httpClient, ILogger<HttpService> logger, ISnackbar snackbar)
        {
            this.httpClient = httpClient;
            this.logger = logger;
            this.snack = snackbar;
        }

        public async Task<Res<TResponse>> PostAsync<TData, TResponse>(string url, TData data)
        {
            HttpResponseMessage httpResponseMessage;
            httpResponseMessage = await httpClient.PostAsJsonAsync(url, data);


            switch (httpResponseMessage.StatusCode)
            {
                case HttpStatusCode.BadRequest:
                    return await HandleBadRequest<TResponse>(url, httpResponseMessage);
                case HttpStatusCode.InternalServerError:
                    return HandleInternalServerError<TResponse>(url);
                case HttpStatusCode.Unauthorized:
                    return HandleUnauthorized<TResponse>(url);
                case HttpStatusCode.Forbidden:
                    return await HandleForbidden<TResponse>(url, httpResponseMessage);
                default:
                    return await HandleSuccessStatusCode<TResponse>(url, httpResponseMessage);
            }
        }


        public async Task<Res<TResponse>> GetAsync<TResponse>(string url)
        {
            HttpResponseMessage httpResponseMessage;
            httpResponseMessage = await httpClient.GetAsync(url);
            switch (httpResponseMessage.StatusCode)
            {
                case HttpStatusCode.BadRequest:
                    return await HandleBadRequest<TResponse>(url, httpResponseMessage);
                case HttpStatusCode.InternalServerError:
                    return HandleInternalServerError<TResponse>(url);
                case HttpStatusCode.Unauthorized:
                    return HandleUnauthorized<TResponse>(url);
                case HttpStatusCode.Forbidden:
                    return await HandleForbidden<TResponse>(url, httpResponseMessage);
                default:
                    return await HandleSuccessStatusCode<TResponse>(url, httpResponseMessage);
            }
        }

        private async Task<Res<TResponse>> HandleBadRequest<TResponse>(string url, HttpResponseMessage httpResponseMessage)
        {
            ErrorResponse? errorResponse;
            try
            {
                errorResponse = await httpResponseMessage.Content.ReadFromJsonAsync<ErrorResponse>();
            }
            catch (Exception ex)
            {
                var errorMsg = $"Не удалось прочитать ошибки c сервера | URL: {url}";
                logger.LogError($"{errorMsg}\n{ex.Message}\nСервер должен отправить - {typeof(ErrorResponse).FullName}");
                snack.Add(errorMsg, Severity.Error);
                return new Res<TResponse>(errorText: errorMsg);
            }

            if (errorResponse == null || errorResponse.Messages == null || errorResponse.Messages.Count == 0)
            {
                var txt = $"Сервер не отправил список ошибок | URL: {url}";
                snack.Add(txt, Severity.Error);
                return new Res<TResponse>(errorText: txt);
            }
            snack.Add(errorResponse.Messages.First(), Severity.Error);
            return new Res<TResponse>(errorTexts: errorResponse.Messages);
        }


        private async Task<Res<TResponse>> HandleSuccessStatusCode<TResponse>(string url, HttpResponseMessage httpResponseMessage)
        {
            TResponse? successRusult = default(TResponse);
            try
            {
                successRusult = await httpResponseMessage.Content.ReadFromJsonAsync<TResponse>();
            }
            catch (Exception ex)
            {
                if (typeof(TResponse) != typeof(Unit))
                {
                    var json = await httpResponseMessage.Content.ReadAsStringAsync();
                    var errTxt = $"Ошибка десериализации :\n{json}\n=>\n{typeof(TResponse).FullName}\nURL: {url}";
                    logger.LogError(errTxt);
                    snack.Add(errTxt, Severity.Error);
                    return new Res<TResponse>(errorText: $"Не удалось преоброзвать полученный JSON в " + typeof(TResponse).Name + $" | URL: {url}");
                }
            }

            return new Res<TResponse>(data: successRusult!);
        }


        private Res<TResponse> HandleInternalServerError<TResponse>(string url)
        {
            var errorMsg = $"Ошибка на стороне сервера | URL: {url}";
            logger.LogError($"{errorMsg}");
            snack.Add(errorMsg, Severity.Error);
            return new Res<TResponse>(errorText: errorMsg);
        }

        private Res<TResponse> HandleUnauthorized<TResponse>(string url)
        {
            var errorMsg = $"Вы не авторизованы | URL: {url}";
            logger.LogError($"{errorMsg}");

            //TODO: обновить состояние авторизации, перенаправить в /login
            snack.Add(errorMsg, Severity.Error);
            return new Res<TResponse>(errorText: errorMsg);
        }

        private async Task<Res<TResponse>> HandleForbidden<TResponse>(string url, HttpResponseMessage httpResponseMessage)
        {
            var res = await HandleBadRequest<TResponse>(url, httpResponseMessage);

            var errorMsg = $"Недостаточно прав: {res.ErrorList.Messages.First()}";
            logger.LogError($"{errorMsg}");
            snack.Add(errorMsg, Severity.Error);
            return new Res<TResponse>(errorText: errorMsg);
        }
    }
}