using GD.Api.Controllers.Base;
using GD.Api.DB;
using GD.Api.DB.Models;
using GD.Shared.Request;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GD.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductController : CustomController
{
    private readonly AppDbContext _appDbContext;

    public ProductController(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = "Bearer", Policy = "admin")]
    public async Task<IActionResult> AddProduct([FromBody] ProductRequest productRequest)
    {
        var product = productRequest.Adapt<Product>();
        product.ImageValue = "";
        _appDbContext.Products.Add(product);
        await _appDbContext.SaveChangesAsync();
        return Ok(product);
    }
    
    [HttpPost("image")]
    [Authorize(AuthenticationSchemes = "Bearer", Policy = "admin")]
    public async Task<IActionResult> SetImage([FromQuery] Guid id)
    {
        var product = await _appDbContext.Products.FirstOrDefaultAsync(p => p.Id == id);

        if (product is null)
            return BadRequest("товар не найден");

        using var buffer = new MemoryStream();
        await Request.Body.CopyToAsync(buffer, Request.HttpContext.RequestAborted);
        
        var imageValue = Convert.ToBase64String(buffer.ToArray());
        product.ImageValue = imageValue;

        _appDbContext.Products.Update(product);
        await _appDbContext.SaveChangesAsync();
        return Ok();
    }
    
    [HttpDelete]
    [Authorize(AuthenticationSchemes = "Bearer", Policy = "admin")]
    public async Task<IActionResult> DeleteProduct([FromQuery] Guid id)
    {
        var product = _appDbContext.Products.FirstOrDefault(p => p.Id == id);
        if (product is null) return BadRequest("товар не найден");

        product.IsDeleted = true;

        await _appDbContext.SaveChangesAsync();
        
        return Ok(product.Name);
    }

    [HttpGet]
    public IActionResult GetAllProducts()
    {
        var response = _appDbContext.Products
            .Include(p => p.Feedbacks);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetProductById([FromRoute] Guid id)
    {
        var product = await _appDbContext.Products.Include(p => p.Feedbacks).FirstOrDefaultAsync(p => p.Id == id);
        if (product is null) return BadRequest("товар не найден");

        return Ok(product);
    }
    
    [HttpGet("image/id:guid")]
    public async Task<IActionResult> GetProductImage([FromRoute] Guid id)
    {
        var product = await _appDbContext.Products.Include(p => p.Feedbacks).FirstOrDefaultAsync(p => p.Id == id);
        if (product is null) return BadRequest("товар не найден");

        Response.ContentType = "image/jpeg";
        var bytes = Convert.FromBase64String(product.ImageValue);
        var stream = new MemoryStream(bytes);
        await stream.CopyToAsync(Response.Body);
        
        return Ok();
    }
}
