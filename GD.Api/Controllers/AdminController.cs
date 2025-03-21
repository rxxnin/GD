using GD.Api.Controllers.Base;
using GD.Api.DB;
using GD.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GD.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = "Bearer", Policy = "admin")]
public class AdminController : CustomController
{
    private readonly AppDbContext _appDbContext;

    public AdminController(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetAllClients()
    {
        return Ok(await (from user in _appDbContext.Users
            join userRole in _appDbContext.UserRoles on user.Id equals userRole.UserId
            join role in _appDbContext.Roles on userRole.RoleId equals role.Id
            where role.Name == GDUserRoles.Client
            select new
            {
                user.Id,
                user.Email,
                user.Balance,
                user.PosLati,
                user.PosLong
            }).ToListAsync());
    }

    [HttpGet("allusers")]
    public async Task<IActionResult> GetAllUsers()
    {
        var r =  await (from user in _appDbContext.Users
            join userRole in _appDbContext.UserRoles on user.Id equals userRole.UserId
            join role in _appDbContext.Roles on userRole.RoleId equals role.Id
            select new
            {
                user.Id,
                user.Email,
                user.Balance,
                user.PosLati,
                user.PosLong,
                Role = role.Name
            }).ToListAsync();

        return Ok(r);
    }
}
