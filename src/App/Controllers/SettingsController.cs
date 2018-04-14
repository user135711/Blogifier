﻿using Core;
using Core.Data;
using Core.Data.Models;
using Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace App.Controllers
{
    [Authorize]
    public class SettingsController : Controller
    {
        IUnitOfWork _db;

        public SettingsController(IUnitOfWork db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            ViewBag.IsAdmin = IsAdmin();

            if (!ViewBag.IsAdmin)
                return RedirectToAction(nameof(Profile));

            var model = new SettingsModel
            {
                Title = AppSettings.Title,
                Description = AppSettings.Description,
                Logo = AppSettings.Logo,
                Cover = AppSettings.Cover,
                PostListType = AppSettings.PostListType,
                ItemsPerPage = AppSettings.ItemsPerPage,
                Theme = AppSettings.Theme,
                BlogThemes = GetThemes()
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Index(SettingsModel model)
        {
            if (ModelState.IsValid)
            {
                if(AppSettings.Title != model.Title)
                    await _db.Settings.SaveSetting("app-title", model.Title);

                if(AppSettings.Description != model.Description)
                    await _db.Settings.SaveSetting("app-desc", model.Description);

                if (AppSettings.Logo != model.Logo)
                    await _db.Settings.SaveSetting("app-logo", model.Logo);

                if (AppSettings.DefaultCover != model.Cover)
                    await _db.Settings.SaveSetting("app-cover", model.Cover);

                if(AppSettings.Theme != model.Theme)
                    await _db.Settings.SaveSetting("app-theme", model.Theme);

                if(AppSettings.ItemsPerPage != model.ItemsPerPage)
                    await _db.Settings.SaveSetting("app-items-per-page", model.ItemsPerPage.ToString());

                var selectedListType = Request.Form["app-post-list-type"];
                if (AppSettings.PostListType != selectedListType)
                    await _db.Settings.SaveSetting("app-post-list-type", selectedListType);

                TempData["msg"] = Resources.Updated;
                return RedirectToAction(nameof(Index));
            }

            model.BlogThemes = GetThemes();
            ViewBag.IsAdmin = IsAdmin();

            return View(model);
        }

        public async Task<IActionResult> Users(int page = 1)
        {
            ViewBag.IsAdmin = IsAdmin();

            if (!ViewBag.IsAdmin)
                return RedirectToAction(nameof(Profile));

            var pager = new Pager(page);
            var authors = await _db.Authors.GetItems(u => u.Created > DateTime.MinValue, pager);

            return View(authors);
        }

        public async Task<IActionResult> Profile()
        {
            var author = await _db.Authors.GetItem(u => u.UserName == User.Identity.Name);
            ViewBag.IsAdmin = author.IsAdmin;
            return View(author);
        }

        public IActionResult Password()
        {
            ViewBag.IsAdmin = IsAdmin();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Password(ChangePasswordModel model)
        {
            model.UserName = User.Identity.Name;
            ModelState.Clear();

            if (ModelState.IsValid)
            {
                try
                {
                    await _db.Authors.ChangePassword(model);
                    TempData["msg"] = Resources.Updated;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("Custom", ex.Message);
                }
            }

            ViewBag.IsAdmin = IsAdmin();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Profile(AuthorItem model)
        {
            if (ModelState.IsValid)
            {
                var user = _db.Authors.Single(a => a.Id == model.Id);
                user.DisplayName = model.DisplayName;
                user.Email = model.Email;

                var result = await _db.Authors.SaveUser(user);
                if (result.Succeeded)
                {
                    TempData["msg"] = Resources.Updated;
                    return RedirectToAction(nameof(Profile));
                }
                else
                {
                    ModelState.AddModelError("Custom", result.Errors.First().Description);
                }
            }

            ViewBag.IsAdmin = IsAdmin();
            return View(model);
        }

        public IActionResult Register()
        {
            if (!IsAdmin())
                return Redirect("~/error/403");

            return View();
        }

        bool IsAdmin()
        {
            return _db.Authors.Single(a => a.UserName == User.Identity.Name).IsAdmin;
        }

        List<SelectListItem> GetThemes()
        {
            var themes = new List<SelectListItem>();
            themes.Add(new SelectListItem { Text = "Simple", Value = "Simple" });

            var storage = new BlogStorage("");
            var storageThemes = storage.GetThemes();

            if(storageThemes != null && storageThemes.Count > 0)
            {
                foreach (var theme in storageThemes)
                {
                    themes.Add(new SelectListItem { Text = theme, Value = theme });
                }
            }           
            return themes;
        }
    }
}