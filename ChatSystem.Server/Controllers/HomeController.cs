using ChatSystem.Server.Models;
using Microsoft.AspNetCore.Mvc;

namespace ChatSystem.Server.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return Redirect("/account/login");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
