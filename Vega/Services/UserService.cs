using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vega.Data;
using Vega.Entities;
using Vega.Enums;
using Vega.Interfaces;
using Vega.Models;

namespace Vega.Services 
{
    public class UserService : IUserService
    {
        private readonly VegaContext _db;
        private readonly IMailService _mailService; 
        private readonly IMethodService _methodService;
        public UserService(
            VegaContext db,
            IMailService mailService,
            IMethodService methodService
        )
        {
            _db = db;
            _mailService = mailService;
            _methodService = methodService;
        }

        public async Task<User> Login(LoginViewModel model)
        {
            User user = await _db.Users
                .Where(x => x.PhoneNumber == model.PhoneNumber && !x.DeletedAt.HasValue)
                .FirstOrDefaultAsync();
            if (user is not null)
            {
                bool passwordControl = BCrypt.Net.BCrypt.Verify(model.Password, user.Password);
                if (passwordControl)
                {
                    return user;
                }
            }
            return null;
        }

        public async Task<bool> Register(RegisterViewModel model)
        {
            bool userExist = await _db.Users
                .Where(x => x.MailAddress == model.MailAddress || x.PhoneNumber == model.PhoneNumber)
                .AnyAsync();
            if (!userExist)
            {
                model.Password = BCrypt.Net.BCrypt.HashPassword(model.Password);
                await _db.AddAsync(new User {
                    Fullname = model.Fullname,
                    MailAddress =  model.MailAddress,
                    Password = model.Password,
                    PhoneNumber = model.PhoneNumber,
                    Role = Role.Player,
                    Money = 0,
                    CreatedAt = DateTime.Now,
                    BirthDate = model.BirthDate,
                    MailVerify = false
                });
                await _db.SaveChangesAsync();
                await _mailService.SendRegisterMail(model.MailAddress);

                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<bool> IsVerified(int Id)
        {
            User userData = await _db.Users.FirstOrDefaultAsync(x => !x.DeletedAt.HasValue && x.Id == Id);
            if (userData is not null && userData.MailVerify)
            {
                return true;
            }
            else 
            {
                return false;
            }
        }

        public async Task MailVerification(int Id)
        {
            Request request = new() 
            {
                URL = await _methodService.GenerateURL(),
                Code = _methodService.GenerateRandomNumber(),
                RequestType = RequestType.MailVerification,
                CreatedAt = DateTime.Now
            };
            await _db.AddAsync(request);
            await _db.SaveChangesAsync();
            await _mailService.SendVerificationMail(Id, request);
        }

        public async Task<bool> ControlVerfiyPage(int Id, string URL)
        {
            Request requestData = await _db.Requests.Where(x => !x.DeletedAt.HasValue && x.URL == URL && x.RequestType == RequestType.MailVerification && x.CreatedAt.Value.AddHours(1) > DateTime.Now && x.UserId == Id).FirstOrDefaultAsync();
            if(requestData is not null)
            {
                return true;
            }
            
            return false;
        }

        public async Task<bool> ControlVerifyCode(int Id, string URL, int code)
        {
            Request requestData = await _db.Requests
                .Where(x => 
                    !x.DeletedAt.HasValue &&
                    x.URL == URL &&
                    x.RequestType == RequestType.MailVerification && 
                    x.CreatedAt.Value.AddHours(1) > DateTime.Now && 
                    x.UserId == Id &&
                    x.Code == code
                ).FirstOrDefaultAsync();
            if(requestData is not null)
            {
                User userData = await _db.Users.Where(x => x.Id == Id).FirstOrDefaultAsync();
                if (userData is not null)
                {
                    userData.MailVerify = true;
                    requestData.DeletedAt = DateTime.Now;
                    await _db.SaveChangesAsync();
                    return true;
                }
            }
            return false;
        }

        public async Task<bool> ResetPasswordRequest(string mailAddress)
        {
            User userData = await _db.Users.FirstOrDefaultAsync(x => !x.DeletedAt.HasValue && x.MailAddress == mailAddress);

            if (userData is not null)
            {
                Request requestData = new() 
                {
                    UserId = userData.Id,
                    URL = await _methodService.GenerateURL(),
                    RequestType = RequestType.ResetPassword,
                    CreatedAt= DateTime.Now
                };
                await _db.AddAsync(requestData);
                await _db.SaveChangesAsync();
                await _mailService.SendResetPasswordMail(userData, requestData);
                return true;
            }
            
            return false;
        }
    }
}