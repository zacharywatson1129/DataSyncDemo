using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleDemo
{
    public class APIHelper
    {
        public HttpClient ApiClient { get; set; }
        public APIHelper() 
        {
            ApiClient = new HttpClient();
            //ApiClient.DefaultRequestHeaders
        }
    }
}
