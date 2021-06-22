using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Vega.Data;
using Vega.Entities;
using Vega.Enums;
using Vega.Interfaces;
using Vega.Models;

namespace Vega.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        private readonly VegaContext _db;
        private readonly IUserService _userService;
        private readonly IJwtService _jwtService;

        public UserController(
            VegaContext db,
            IUserService userService,
            IJwtService jwtService
        )
        {
            _db = db;
            _userService = userService;
            _jwtService = jwtService;
        }

        [AllowAnonymous]
        [HttpPost("/login")]
        public async Task<IActionResult> Login([FromBody] LoginViewModel loginData)
        {
            if (!string.IsNullOrEmpty(loginData.PhoneNumber) && !string.IsNullOrEmpty(loginData.Password))
            {
                User user = await _userService.Login(loginData);
                if (user is not null)
                {
                    string userJWT = _jwtService.Create(user);
                    Response.Cookies.Append("vegaJWT", userJWT, new CookieOptions {
                        HttpOnly = true
                    });
                    return Ok(userJWT);
                }
            }
            return StatusCode((int)ErrorCode.InvalidCredentials, "Invalid Credentials");
        }

        [AllowAnonymous]
        [HttpPost("/register")]
        public async Task<IActionResult> Register([FromBody] RegisterViewModel registerData)
        {
            if (!string.IsNullOrEmpty(registerData.Fullname) && !string.IsNullOrEmpty(registerData.MailAddress) && !string.IsNullOrEmpty(registerData.Password) && !string.IsNullOrEmpty(registerData.PhoneNumber) && registerData.BirthDate != null)
            {
                bool registerResult = await _userService.Register(registerData);
                if (registerResult)
                {
                    return Ok(true);
                }
                else 
                {
                    return StatusCode((int)ErrorCode.AlreadyExist, "This user already exist.");
                }
            }
            return StatusCode((int)ErrorCode.MustBeFilled, "All fields must be filled correctly.");
        }

    }
}