using Microsoft.AspNetCore.Mvc;

namespace VrRestApi.Controllers
{
    [Route("api/[controller]")]
    public class StartController : Controller
    {
        [HttpGet]
        public ActionResult<string> Start()
        {
            return "api start!";
        }

        [HttpPost]
        public ActionResult<TestClass> PostMethod([FromBody] TestClass obj)
        {
            return obj;
        }
    }

    public class TestClass
    {
        public int Id { get; set; }
        public string Title { get; set; }
    }
}
