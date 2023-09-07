﻿using BestStoreApi.Models;
using BestStoreApi.Models.DTO;
using BestStoreApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BestStoreApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IConfiguration configuration;
        private readonly ApplicationDbContext context;

        public AccountController(IConfiguration configuration, ApplicationDbContext context)
        {
            this.configuration = configuration;
            this.context = context;
        }

        //[HttpGet("TestToken")]
        //public IActionResult TestToken()
        //{
        //    User user = new User()
        //    {
        //        Id = 1,
        //        Role = "admin"
        //    };
        //    string jwt = CreateJWToken(user);
        //    var response = new { JWToken = jwt };
        //    return Ok(response);
        //}

        [HttpPost("Register")]
        public IActionResult Register(UserDto userDto)
        {
            //check if the email address is already user or
            var emailCount = context.Users.Count(u => u.Email == userDto.Email);
            if (emailCount > 0)
            {
                ModelState.AddModelError("Email", "This email address is already used");
                return BadRequest(ModelState);
            }

            //encrypt password
            var passwordHasher = new PasswordHasher<User>();
            var encryptedPassword = passwordHasher.HashPassword(new User(), userDto.Password);

            //create new user account
            User user = new User()
            {
                FirstName = userDto.FirstName,
                LastName = userDto.LastName,
                Email = userDto.Email,
                Phone = userDto.Phone,
                Address = userDto.Address,
                Password = encryptedPassword,
                Role = "client",
                CreatedAt = DateTime.Now
            };

            context.Users.Add(user);
            context.SaveChanges();

            var jwt = CreateJWToken(user);

            UserProfileDto userProfileDto = new UserProfileDto()
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.Phone,
                Address = user.Address,
                Role = user.Role,
                CreatedAt = DateTime.Now
            };

            var response = new
            {
                Token = jwt,
                User = userProfileDto
            };

            return Ok(response);   
        }

        [HttpPost("Login")]
        public IActionResult Login (string email, string password)
        {
            var user = context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                ModelState.AddModelError("Error", "Email or Password not valid");
                return BadRequest(ModelState);
            }

            // verify the password
            var passwordHasher = new PasswordHasher<User>();
            var result = passwordHasher.VerifyHashedPassword(new User(), user.Password, password);
            if (result == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError("Password", "Wrong password");
                return BadRequest(ModelState);
            }

            var jwt = CreateJWToken(user);

            UserProfileDto userProfileDto = new UserProfileDto()
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.Phone,
                Address = user.Address,
                Role = user.Role,
                CreatedAt = DateTime.Now
            };

            var response = new
            {
                Token = jwt,
                User = userProfileDto
            };

            return Ok(response);
        }

        [HttpPost("ForgotPassword")]
        public IActionResult ForgotPassword(string email)
        {
            var user = context.Users.FirstOrDefault(u => u.Email == email);
            if(user == null)
            {
                return NotFound();
            }

            //delete any old password reset request
            var oldPwdReset = context.PasswordResets.FirstOrDefault(r => r.Email == email);
            if(oldPwdReset != null)
            {
                //delete old passwrod reser request
                context.Remove(oldPwdReset);
            }
            // crete Password Reset Token
            string token = Guid.NewGuid().ToString() + "-" + Guid.NewGuid().ToString();
            var pwdReset = new PasswordReset()
            {
                Email = email,
                Token = token,
                CreatedAt = DateTime.Now,
            };

            context.PasswordResets.Add(pwdReset);
            context.SaveChanges();

            //send the password reset token by email to the user
            return Ok(pwdReset);
        }

        [HttpPost("ResetPassword")]
        public IActionResult ResetPassword(string token, string password)
        {
            var pwdReset = context.PasswordResets.FirstOrDefault(r => r.Token == token);
            if(pwdReset == null)
            {
                ModelState.AddModelError("Token", "Wrong or Expired Token");
                return BadRequest(ModelState);

            }

            var user = context.Users.FirstOrDefault(u => u.Email == pwdReset.Email);
            if(user == null)
            {
                ModelState.AddModelError("Token", "Wrong or Expired Token");
                return BadRequest(ModelState);
            }

            //encrypt password
            var passwordHasher = new PasswordHasher<User>();
            string encryptedPassword = passwordHasher.HashPassword(new Models.User(), password);

            //save the new encrypted password
            user.Password = encryptedPassword;

            //delete the token
            context.PasswordResets.Remove(pwdReset);
            context.SaveChanges();

            return Ok();
        }

        [Authorize]
        [HttpGet("Profile")]
        public IActionResult GetProfile()
        {
            int id = JwtReader.GetUserId(User);
            var user = context.Users.Find(id);
            if(user == null)
            {
                return Unauthorized();
            }

            var userProfileDto = new UserProfileDto()
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.Phone,
                Address = user.Address,
                Role = user.Role,
                CreatedAt = user.CreatedAt
            };

            return Ok(userProfileDto);
        }
        [Authorize]
        [HttpPut("UpdateProfile")]
        public IActionResult Updateprofile(UserProfileUpdateDto userProfileUpdateDto)
        {
            int id = JwtReader.GetUserId(User);

            var user = context.Users.Find(id);
            if(user == null)
            {
                return Unauthorized();
            }

            //update user profile
            user.FirstName = userProfileUpdateDto.FirstName;
            user.LastName = userProfileUpdateDto.LastName;
            user.Email = userProfileUpdateDto.Email;
            user.Phone = userProfileUpdateDto.Phone;
            user.Address = userProfileUpdateDto.Address;

            context.SaveChanges();

            var userProfileDto = new UserProfileDto()
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.Phone,
                Address = user.Address,
                Role = user.Role,
                CreatedAt = user.CreatedAt
            };
            return Ok(userProfileDto);


        }

        [Authorize]
        [HttpPut("UpdatePassword")]
        public IActionResult UpdatePassword(string password)
        {
            int id = JwtReader.GetUserId(User);
            var user = context.Users.Find(id);
            if(user == null)
            {
                return Unauthorized();
            }

            //encrypt password
            var passwordhasher = new PasswordHasher<User>();
            string encryptedPassword = passwordhasher.HashPassword(new User(), password);

            //update the user password
            user.Password = encryptedPassword;

            context.SaveChanges();
            return Ok();
        }

        private string CreateJWToken(User user)
        {
            List<Claim> claims = new List<Claim>()
            {
                new Claim("id","" + user.Id),
                new Claim("role", user.Role)
            };

            string secretKey = configuration["JwtSettings:Key"]!;

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);

            var token = new JwtSecurityToken(
                issuer: configuration["JwtSettings:Issuer"],
                audience: configuration["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds
                );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            return jwt;
        }

    }
}
