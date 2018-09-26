using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AraturkaSlave.Models
{
    public class NormalSonuc
    {
        public System.Guid Id { get; set; }
        public string baslik { get; set; }
        public string aciklama { get; set; }
        public string icerik { get; set; }
        public string url { get; set; }
    }
}