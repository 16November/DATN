using Azure.Core;
using DoAnTotNghiep.Model;
using DoAnTotNghiep.Services.IService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using DoAnTotNghiep.Dto.Request;
using SequentialGuid;
using DoAnTotNghiep.Dto.Response;
using Microsoft.AspNetCore.Identity.Data;

namespace DoAnTotNghiep.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ITokenService tokenService;
        private readonly UserManager<User> userManager;

        public AuthController(ITokenService tokenService, UserManager<User> userManager)
        {
            this.tokenService = tokenService;
            this.userManager = userManager;
        }

        [HttpPost]
        [Route("register")]
        public async Task<IActionResult> Register([FromBody] Dto.Request.RegisterRequest registerRequest)
        {
            var user = new User()
            {
                UserId = (Guid)SequentialSqlGuidGenerator.Instance.NewSqlGuid(),
                UserName = registerRequest.UserName,
            };
            var result = await userManager.CreateAsync(user, registerRequest.Password);
            if (result.Succeeded)
            {
                if (string.IsNullOrEmpty(registerRequest.Role))
                {
                    registerRequest.Role = "Student";
                    result = await userManager.AddToRoleAsync(user, registerRequest.Role);
                    if (result.Succeeded)
                    {
                        return Ok(registerRequest);
                    }
                    else
                    {
                        await userManager.DeleteAsync(user);
                        return StatusCode(500, "Failed to assign role. Registration rolled back.");
                    }
                }
                else
                {
                    result = await userManager.AddToRoleAsync(user, registerRequest.Role);
                    if (result.Succeeded)
                    {
                        return Ok(registerRequest);
                    }
                    else
                    {
                        await userManager.DeleteAsync(user);
                        return StatusCode(500, "Failed to assign role. Registration rolled back.");
                    }
                }
            }
            return BadRequest("Request can't fulfill ");
        }

        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login([FromBody] Dto.Request.LoginRequest loginRequest)
        {
            if (string.IsNullOrEmpty(loginRequest.Username))
            {
                return BadRequest("Don't have Username");
            }

            var userResult = await userManager.FindByNameAsync(loginRequest.Username);
            if(userResult != null)
            {
                var result = await userManager.CheckPasswordAsync(userResult,loginRequest.Password);
                if (result)
                {
                    var roles = await userManager.GetRolesAsync(userResult);
                    if (roles != null)
                    {
                        var JwtToken = tokenService.CreateJwtToken(userResult, roles.ToList());
                        var response = new LoginResponse()
                        {
                            UserId = userResult.UserId,
                            JwtToken = JwtToken,
                            Role = roles.FirstOrDefault() ?? string.Empty
                        };
                        return Ok (response);
                    }
                }
            }
            return BadRequest("Cant't login ");
        }

        //[HttpPost]
        //[Route("ResetPassword")]
        //public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest resetPasswordRequest)
        //{
        //    var user = await userManager.FindByNameAsync(resetPasswordRequest.UserName);
        //    if (user == null)
        //    {
        //        return NotFound("Invalid UserName");
        //    }
        //    var result = await userManager.ResetPasswordAsync(user, resetPasswordRequest.Token, resetPasswordRequest.Password);
        //    if (result.Succeeded)
        //    {
        //        return Ok("Reset successful");
        //    }
        //    foreach (var error in result.Errors)
        //    {
        //        ModelState.AddModelError(string.Empty, error.Description);
        //    }
        //    return BadRequest(ModelState);
        //}

    }
}
