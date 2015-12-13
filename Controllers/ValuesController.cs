using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;

namespace CoreCLRWebAPISample.Controllers
{
    [Route("[controller]")]
    public class ValuesController : Controller
    {
        // GET api/values/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return id.ToString();
        }
    }
}
