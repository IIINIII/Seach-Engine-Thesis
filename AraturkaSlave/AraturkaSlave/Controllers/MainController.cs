using AraturkaSlave.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;

namespace AraturkaSlave.Controllers
{
    public class MainController : Controller
    {
        static int pageNo = 0;
        int MRPP = 10;
        int IMRPP = 100;
        // GET: Main
        public ActionResult Index()
        {
            return View();
        }

        // GET: Main/Ara
        public ActionResult Ara(string sorgu = "araturka",int sayfano = 1)
        {
            sayfano = Math.Max(sayfano, 1);
            pageNo = sayfano-1;
            List<NormalSonuc> model = new List<NormalSonuc>();
            string[] kelimeler = Regex.Split(sorgu, @"[\W]+");
            ViewData["sorgu"] = sorgu;
            using (DBDataContext db = new DBDataContext())
            {
                // Biraz daha islem eklenecektir
                var data = db.WebSayfalars.Where(
                    sayfa => sayfa.scanned == true &&
                sayfa.baslik != null &&
                sayfa.icerik != null).Where(
                    sayfa => sayfa.SayfaAnahtarlaris.Where(
                        anahtar => kelimeler.Contains(anahtar.AnahtarIfadeler.ifade)).Count()>0).Select(sayfa=>new PuanliSite(){ item = sayfa,sum=sayfa.SayfaAnahtarlaris.Where(ifad=>kelimeler.Contains(ifad.AnahtarIfadeler.ifade)).Sum(ifad=>ifad.puan)}).OrderByDescending(ps=>ps.sum).ThenBy(ps=>ps.item.baslik.Length).ToArray<PuanliSite>();
                pageNo = Math.Min(pageNo, ((data.Count() + MRPP - 1) / MRPP) - 1);
                if (data.Count() > 0 && pageNo >= 0)
                {
                    for (int i = MRPP * pageNo; i < Math.Min(MRPP * (pageNo + 1), data.Count()); i++)
                    {
                        var sayfa = data[i];
                        model.Add(new NormalSonuc() { Id = sayfa.item.Id, baslik = sayfa.item.baslik+"("+sayfa.sum+")", aciklama = sayfa.item.aciklama, icerik = (sayfa.item.icerik != null ? sayfa.item.icerik.Substring(0, Math.Min(sayfa.item.icerik.Length, 320)) + (sayfa.item.icerik.Length > 320 ? "..." : "") : ""), url = sayfa.item.url });
                    }
                }
                ViewData["toplam"] = data.Count() + " (Sayfa: " + (pageNo + 1) + "/" + ((data.Count() + MRPP - 1) / MRPP) + ")";
                ViewData["sayfano"] = pageNo + 1;
                ViewData["bas"] = Math.Max(1, pageNo - 3);
                ViewData["son"] = Math.Min((int)ViewData["bas"] + 8, ((data.Count() + MRPP - 1) / MRPP));
            }
            return View(model);
        }

        // GET: Main/GorselAra
        public ActionResult GorselAra(string sorgu="araturka",int sayfano = 1)
        {
            sayfano = Math.Max(sayfano, 1);
            pageNo = sayfano-1;
            List<GorselSonuc> model = new List<GorselSonuc>();
            string[] kelimeler = Regex.Split(sorgu, @"[\W]+");
            ViewData["sorgu"] = sorgu;
            using (DBDataContext db = new DBDataContext())
            {
                // Biraz daha islem eklenecektir
                var data = db.WebSayfalars.Where(
                    sayfa => sayfa.scanned == true &&
                    sayfa.content_type.Contains("image")).Take(IMRPP * 10).Where(
                    sayfa => sayfa.SayfaAnahtarlaris.Where(
                        anahtar => kelimeler.Contains(anahtar.AnahtarIfadeler.ifade)).Count() > 0).Select(sayfa => new PuanliSite() { item = sayfa, sum = sayfa.SayfaAnahtarlaris.Where(ifad => kelimeler.Contains(ifad.AnahtarIfadeler.ifade)).Sum(ifad => ifad.puan) }).OrderByDescending(ps => ps.sum).ToArray<PuanliSite>();
                pageNo = Math.Min(pageNo, ((data.Count() + IMRPP - 1) / IMRPP) - 1);
                if (data.Count() > 0 && pageNo >= 0)
                {
                    for (int i = IMRPP * pageNo; i < Math.Min(IMRPP * (pageNo + 1), data.Count()); i++)
                    {
                        var sayfa = data[i];
                        model.Add(new GorselSonuc() { Id = sayfa.item.Id, baslik = sayfa.item.baslik+"("+sayfa.sum+")", source_url = sayfa.item.url, url = sayfa.item.url });
                    }
                }
                ViewData["toplam"] = data.Count() + " (Sayfa: " + (pageNo + 1) + "/" + ((data.Count() + IMRPP - 1) / IMRPP) + ")";
                ViewData["sayfano"] = pageNo + 1;
                ViewData["bas"] = Math.Max(1, pageNo - 3);
                ViewData["son"] = Math.Min((int)ViewData["bas"] + 8, ((data.Count() + IMRPP - 1) / IMRPP));
            }
            return View(model);
        }
    }
}