using DoAnTotNghiep.Model;
using DoAnTotNghiep.Services.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DoAnTotNghiep.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserInfoController : ControllerBase
    {
        private readonly UserInfoService userInfoService;

        public UserInfoController(UserInfoService userInfoService)
        {
            this.userInfoService = userInfoService;
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddUserInfo([FromBody] UserInfo userInfo)
        {
            await userInfoService.AddUserInfo(userInfo);
            return Ok("User info added.");
        }

        [HttpPut]
        [Route("update")]
        public async Task<IActionResult> UpdateUserInfo([FromQuery]Guid userId, [FromBody] UserInfo userInfo)
        {
            await userInfoService.UpdateUserInfo(userId, userInfo);
            return Ok("User info updated.");
        }

        [HttpDelete]
        [Route("delete")]
        public async Task<IActionResult> DeleteUserInfo([FromQuery]Guid userId)
        {
            await userInfoService.DeleteUserInfo(userId);
            return Ok("User info deleted.");
        }

        [HttpGet]
        [Route("getDetail")]
        public async Task<ActionResult<UserInfo>> GetDetailUserInfo(Guid userId)
        {
            var userInfo = await userInfoService.GetDetailUserInfo(userId);
            
            return Ok(userInfo);
        }
    }
}
