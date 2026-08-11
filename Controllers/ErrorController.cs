using Microsoft.AspNetCore.Mvc;

namespace ToDoListTracker.Controllers;

public class ErrorController : Controller
{
    [Route("/Error")]
    public IActionResult Index() => View("Error");
}
