using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc;

namespace DiscordBot.Controllers;

public class HelloWorld : Controller
{
    // GET
    public IActionResult Index()
    {
        return Ok(HtmlEncoder.Default.Encode("Yes, yes, hello human."));
    }
}