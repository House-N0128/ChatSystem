using Microsoft.AspNetCore.Mvc;

namespace ChatSystem.Server.Controllers.Mvc;

public class ChatController : Controller
{
    [HttpGet("/chat")]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet("/chat/friends")]
    public IActionResult Friends()
    {
        return View();
    }

    [HttpGet("/chat/history")]
    public IActionResult History()
    {
        return View();
    }

    [HttpGet("/chat/requests")]
    public IActionResult Requests()
    {
        return View();
    }

    [HttpGet("/chat/profile")]
    public IActionResult Profile()
    {
        return View();
    }
}
