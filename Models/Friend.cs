using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MVC_testApp.Models
{
    public class Friend
    {
        public Friend(int id)
        {
            ID = id;
        }

        public int ID { get; set; }
    }
}