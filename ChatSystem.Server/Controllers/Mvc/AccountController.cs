using Microsoft.AspNetCore.Mvc;

namespace ChatSystem.Server.Controllers.Mvc;

public class AccountController : Controller
{
    [HttpGet("/account/login")]
    public IActionResult Login()
    {
        return View();
    }

    [HttpGet("/account/register")]
    public IActionResult Register()
    {
        return View();
    }

    [HttpGet("/account/logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("AuthToken");
        return RedirectToAction("Index", "Home");
    }
}
