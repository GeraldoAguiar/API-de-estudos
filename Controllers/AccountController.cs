using System.Text.RegularExpressions;
using Blog.Data;
using Blog.Extensions;
using Blog.Models;
using Blog.Services;
using Blog.ViewModels;
using Blog.ViewModels.Accounts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureIdentity.Password;

namespace Blog.Controllers;


[ApiController]
public class AccountController : ControllerBase
{
    private readonly TokenService _tokenService;
    public AccountController(TokenService tokenService)
    {
        _tokenService = tokenService;
    }
    [HttpPost("v1/accounts/")]
    public async Task<IActionResult> Post(
        [FromBody] RegisterViewModel model,
        [FromServices] EmailService emailService,
        [FromServices] BlogDataContext context)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ResultViewModel<string>(ModelState.GetErrors()));

        var user = new User
        {
            Name = model.Name,
            Email = model.Email,
            Slug = model.Email.Replace("@", "-").Replace(".", "-"),
            Bio = model.Name

        };
        var password = PasswordGenerator.Generate(length: 25);
        user.PasswordHash = PasswordHasher.Hash(password);

        try
        {
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();

            emailService.Send(
                user.Name,
                user.Email, "Bem vindo ao blog", $"sua senha é{password}");

            return Ok(new ResultViewModel<dynamic>(new
            {
                user = user.Email,
                password
            }));

        }
        catch (DbUpdateException)
        {
            return StatusCode(400, new ResultViewModel<string>("05x99 - falha interna no servidor"));
        }
        catch
        {
            return StatusCode(500, new ResultViewModel<string>("05x04 - Falha interna no servidor"));
        }
    }
    [HttpPost("v1/accounts/login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginViewModel model,
        [FromServices] BlogDataContext context,
        [FromServices] TokenService tokenService)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ResultViewModel<string>(ModelState.GetErrors()));

        var user = await context
              .Users
              .AsNoTracking()
              .Include(x => x.Roles)
              .FirstOrDefaultAsync(x => x.Email == model.Email);

        if (user == null)
            return StatusCode(401, new ResultViewModel<string>("Usuário ou senha invállido"));

        if (!PasswordHasher.Verify(user.PasswordHash, model.Password))
            return StatusCode(401, new ResultViewModel<string>("Usuário ou senha invállido"));

        try
        {
            var token = tokenService.GenerateToken(user);
            return Ok(new ResultViewModel<string>(token, null));
        }
        catch (System.Exception)
        {
            return StatusCode(500, new ResultViewModel<string>("05x04 - Falha interna servidor"));
        }
    }

    [Authorize]
    [HttpPost("v1/accounts/upload-image")]
    public async Task<IActionResult> UploadImage(
        [FromBody] UploadImageViewModel model,
        [FromServices] BlogDataContext context)
    {
        var fileName = $"{Guid.NewGuid().ToString()}.jpg";
        var data = new Regex(@"^data:image V[a-z]+;base64,")
            .Replace(model.Base64Image, "");
        var bytes = Convert.FromBase64String(data);

        try
        {
            await System.IO.File.WriteAllBytesAsync($"wwwroot/images/{fileName}", bytes);
        }
        catch (Exception ex)
        {

            return StatusCode(500, new ResultViewModel<string>("05x04 - Falha interna no servidor"));
        }

        var user = await context
            .Users
            .FirstOrDefaultAsync(x => x.Email == User.Identity.Name);

        if (user == null)
            return NotFound(new ResultViewModel<User>("Usuário não encontrado"));

        user.Image = $"https://localhost:0000/images/{fileName}";

        try
        {
            context.Users.Update(user);
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ResultViewModel<string>("05x07 - Falha interna no Servidor"));
        }

        return Ok(new ResultViewModel<string>("Imagem alterada com sucesso", null));
    }





    [HttpDelete("v1/accounts/{id:int}")]
    public async Task<IActionResult> DeleteAsync(
    [FromRoute] int id,
     [FromServices] BlogDataContext context)
    {
        try
        {
            var user = await context
        .Users
        .FirstOrDefaultAsync(x => x.Id == id);

            if (user == null)
                return NotFound(new ResultViewModel<User>("05x06 - Conteúdo não encontrado"));

            context.Users.Remove(user);
            await context.SaveChangesAsync();

            return Ok(new ResultViewModel<User>(user));
        }
        catch (DbUpdateException ex)
        {
            return StatusCode(500, new ResultViewModel<User>("05XE7 - Não foi possivel excluir a categoria"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ResultViewModel<Category>("05X12 - Falha interna no servidor"));
        }
    }
}