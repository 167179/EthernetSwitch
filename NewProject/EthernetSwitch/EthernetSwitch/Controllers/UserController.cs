﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using EthernetSwitch.Infrastructure;
using EthernetSwitch.Models;
using EthernetSwitch.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EthernetSwitch.Controllers
{
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public class UserController : Controller
    {
        private readonly IUserService _userService;
        private readonly ISettingsRepository _settingsRepository;

        public UserController(IUserService userService, ISettingsRepository settingsRepository)
        {
            _userService = userService;
            _settingsRepository = settingsRepository;
        }

        [AllowAnonymous]
        public IActionResult Login()
        {
            var settings = _settingsRepository.GetSettings();

            return View("Login", new LoginViewModel {CanRegister = settings.AllowRegistration});
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model, string ReturnUrl)
        {
            if (ModelState.IsValid)
            {
                ReturnUrl ??= Url.Content("~/");

                if (model.Type == LoginType.Register)
                {
                    var settings = _settingsRepository.GetSettings();
                    if (settings.AllowRegistration)
                    {
                        var role = settings.RequireConfirmation ? UserRole.NotConfirmed : UserRole.User;
                        _userService.Register(model.UserName, model.Password, role);
                    }
                }

                var user = _userService.Login(model.UserName, model.Password);
                
                if (user == null)
                {
                    model.Password = String.Empty;
                    model.Message = "Wrong password or username";

                    return View("Login", model);
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim(ClaimTypes.Role, user.Role.ToString()),
                };

                var claimsIdentity = new ClaimsIdentity(
                    claims, CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    AllowRefresh = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(10),
                    IssuedUtc = DateTimeOffset.Now,
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity), authProperties);

                return Redirect(ReturnUrl);
            }

            return View(model);
        }

        public IActionResult ChangePassword()
        {
            return View("ChangePassword", new ChangePasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangePassword(ChangePasswordViewModel model)
        {
            var username = User?.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.Name)?.Value;
            
            if (ModelState.IsValid)
            {
                _userService.ChangePassword(username, model.NewPassword);

                return RedirectToAction("Index","Home");
            }

            return View(model);
        }
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Login");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Roles = "Admin")]
        public IActionResult RegisterUsers(string[] users)
        {
            _userService.RegisterUsers(users);


            return RedirectToAction("Settings", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Roles = "Admin")]
        public IActionResult RemoveUsers(string[] users)
        {
            _userService.RemoveUsers(users);


            return RedirectToAction("Settings", "Home");
        }
    }
}