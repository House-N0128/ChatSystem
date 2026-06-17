using Microsoft.AspNetCore.Mvc;

namespace ChatSystem.Server.Controllers.Mvc;

public class AdminMvcController : Controller
{
    [HttpGet("/admin")]
    public IActionResult Dashboard()
    {
        return View();
    }

    [HttpGet("/admin/users")]
    public IActionResult Users()
    {
        return View();
    }

    [HttpGet("/admin/messages")]
    public IActionResult Messages()
    {
        return View();
    }
}
