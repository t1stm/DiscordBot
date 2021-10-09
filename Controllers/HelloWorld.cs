using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc;

namespace BatToshoRESTApp.Controllers
{
    public class HelloWorld : Controller
    {
        // GET
        public string Index()
        {
            return HtmlEncoder.Default.Encode("Hello World!");
        }
    }
}