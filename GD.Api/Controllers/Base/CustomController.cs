using GD.Shared.Common;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GD.Api.Controllers.Base
{
    public class CustomController: ControllerBase
    {
        internal Guid ContextUserId
        {
            get
            {
                var id = User.FindFirst(GDUserClaimTypes.Id);
                Console.WriteLine("==============JWT ID " + id);
                return Guid.Parse(id.Value);
            }
        }
        internal string? CurrentIp => HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString();

        public override BadRequestObjectResult BadRequest(object? error)
        {
            var notValid = new Res<bool>(error?.ToString() ?? "");
            return base.BadRequest(notValid.ErrorList);
        }
    }
}
