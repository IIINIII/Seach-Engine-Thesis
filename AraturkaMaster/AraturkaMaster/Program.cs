using System.IO;
using AraturkaMaster.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Net;
using HtmlAgilityPack;

namespace AraturkaMaster
{
    class Program
    {
        static bool showErrors = false;
        static WebSayfa sayfa = null;
        static Guid sayfa_id = Guid.Empty;
        static Rake RAKE = null;
        static void Main(string[] args)
        {
            RequestHandler("Hataları göstersin mi (e/H)?");
            string temp = Console.ReadLine();
            if (temp.Trim().ToLower() == "e" || temp.Trim().ToLower() == "evet")
                showErrors = true;
            Engage();
        }
        static void Engage()
        {
            try
            {
                RAKE = new Rake();
                while (true)
                {
                    sayfa_id = NextLink();
                    sayfa = GetHtml(sayfa_id);
                    if (sayfa != null)
                    {
                        SuccessHandler(sayfa.url);
                        InfoHandler(sayfa.Id);
                        if (sayfa.icerik != null)
                        {
                            sayfa.baslik = ExtractTitle(sayfa.icerik, sayfa.Id);
                            sayfa.aciklama = ExtractDescription(sayfa.icerik, sayfa.Id);
                            ExtractLinks(sayfa.icerik, new Uri(sayfa.url));
                            sayfa.icerik = ExtractPageText(sayfa.icerik);
                            if (sayfa.icerik != null)
                            {
                                Dictionary<string, double> allkeys = RAKE.Run(sayfa.icerik);
                                foreach (var kelime in allkeys)
                                    insertSayfaAnahtari(insertAnahtarIfade(kelime.Key), sayfa.Id, kelime.Value);
                            }
                        }
                        if (sayfa.icerik != null) InfoHandler(sayfa.icerik);
                        if (sayfa.baslik != null) InfoHandler(sayfa.baslik);
                        if (sayfa.aciklama != null) InfoHandler(sayfa.aciklama);
                        if (sayfa.response_code != null) InfoHandler(sayfa.response_code);
                        if (sayfa.content_type != null) InfoHandler(sayfa.content_type);
                        using (DBDataContext db = new DBDataContext())
                        {
                            WebSayfa original = db.WebSayfalar.FirstOrDefault(w => w.Id == sayfa.Id);
                            original.baslik = sayfa.baslik != null ? sayfa.baslik : original.baslik;
                            original.aciklama = sayfa.aciklama != null ? sayfa.aciklama : original.aciklama;
                            original.icerik = sayfa.icerik != null ? sayfa.icerik : original.icerik;
                            original.response_code = sayfa.response_code != null ? sayfa.response_code : original.response_code;
                            original.content_type = sayfa.content_type != null ? sayfa.content_type : original.content_type;
                            original.scanned = true;
                            db.SubmitChanges();
                        }
                    }
                    else
                    {
                        using (DBDataContext db = new DBDataContext())
                        {
                            WebSayfa original = db.WebSayfalar.FirstOrDefault(w => w.Id == sayfa_id);
                            original.scanned = true;
                            db.SubmitChanges();
                        }
                    }
                }
            }
            catch(Exception e)
            {
                InfoHandler(sayfa.Id);
                InfoHandler(sayfa.url);
                InfoHandler(sayfa.icerik);
                InfoHandler(sayfa.content_type);
                InfoHandler(sayfa.response_code);
                InfoHandler(sayfa.baslik);
                InfoHandler(sayfa.aciklama);
                ErrorHandler(e, null);
            }
        }
        static WebSayfa GetHtml(Guid link_id)
        {
            try
            {
                using (DBDataContext db = new DBDataContext())
                {
                    WebSayfa sayfa = db.WebSayfalar.FirstOrDefault(l => l.Id == link_id);
                    if (sayfa == null)
                        return null;
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(sayfa.url);
                    request.AutomaticDecompression = DecompressionMethods.GZip;
                    try
                    {
                        using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                        using (Stream stream = response.GetResponseStream())
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            sayfa.content_type = response.ContentType;
                            sayfa.response_code = (short)response.StatusCode;
                            if (response.ContentType.Contains("text/html"))
                                sayfa.icerik = reader.ReadToEnd();
                        }
                        return sayfa;
                    }
                    catch (WebException webException)
                    {
                        try
                        {
                            sayfa.response_code = (short)(webException.Response as HttpWebResponse).StatusCode;
                        }
                        catch (Exception e)
                        {
                            ErrorHandler(e, null);
                        }
                    }
                    catch (Exception e)
                    {
                        ErrorHandler(e, null);
                    }
                }
            }
            catch (Exception e)
            {
                ErrorHandler(e, null);
            }
            return null;
        }
        static string ExtractTitle(string html, Guid link_id)
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);
            var x = htmlDoc.DocumentNode.SelectSingleNode("//title");

            string title = null;

            if (x != null)
            {
                string text = x.InnerText;

                PrepareKeywords(text, link_id);
                insertSayfaAnahtari(insertAnahtarIfade(text), link_id);
                title = text;
            }

            if (title != null)
                return Regex.Replace(title, @"\s+", " ").Trim();
            return null;
        }
        static string ExtractDescription(string html, Guid link_id)
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);
            var x = htmlDoc.DocumentNode.SelectSingleNode("//meta[@name='description']");

            string description = null;

            if (x != null)
            {
                if (x.Attributes["content"] == null)
                    return null;
                string text = x.Attributes["content"].Value;

                PrepareKeywords(text, link_id);
                description = text;
            }

            if (description != null)
                return Regex.Replace(description, @"\s+", " ").Trim();
            return null;
        }
        public static string ExtractPageText(string html)
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);
            var x = htmlDoc.DocumentNode.SelectNodes("//body//text()[not(ancestor::style)][not(ancestor::script)][not(ancestor::button)][not(ancestor::select)][not(ancestor::ul)][not(ancestor::ol)]");
            string page_text = null;
            if (x != null)
            {
                string text = "";
                foreach (HtmlNode node in x)
                {
                    text += " " + node.InnerText.Trim();
                    text.Trim();
                    if (text.Length > 1023) break;
                }
                text = text.Trim();
                page_text = text.Substring(0, Math.Min(1024, text.Length));
            }
            if (page_text != null)
                return Regex.Replace(page_text, @"\s+", " ").Trim();
            return null;
        }
        static void ExtractLinks(string html, Uri uri)
        {
            string prePath = uri.AbsoluteUri;
            prePath = prePath.Substring(0, prePath.LastIndexOf('/') + 1);
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);
            var nodes = htmlDoc.DocumentNode.SelectNodes("//a[@href]");
            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    try
                    {
                        string link = node.Attributes["href"].Value;
                        if (link.Substring(0, Math.Min(4, link.Length)) != "http")
                        {
                            if (link == "")
                                continue;
                            if (link[0] != '/')
                                link = prePath + link;
                            else
                            {
                                string tempPre = prePath.Substring(prePath.IndexOf("://") + 3);
                                link = prePath.Substring(0, prePath.IndexOf("://") + 3) + tempPre.Substring(0, tempPre.IndexOf("/")) + link;
                            }
                        }
                        if (link != null && link.Length > 7 && link.Length < 2048 && link.Contains("websitem"))
                        {
                            Guid link_id = insertSayfa(link, null);
                            PrepareKeywords(link, link_id);
                            insertSayfaAnahtari(insertAnahtarIfade(uri.Authority + uri.AbsolutePath), link_id);
                        }
                    }
                    catch (Exception e)
                    {
                        ErrorHandler(e, null);
                    }
                }
            }
            nodes = htmlDoc.DocumentNode.SelectNodes("//img[@src]");
            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    try
                    {
                        string link = node.Attributes["src"].Value;
                        string title = null;
                        if (node.Attributes["alt"] != null)
                            title = node.Attributes["alt"].Value;
                        if (link.Substring(0, Math.Min(4, link.Length)) != "http")
                        {
                            if (link == "")
                                continue;

                            if (link[0] != '/')
                                link = prePath + link;
                            else
                            {
                                string tempPre = prePath.Substring(prePath.IndexOf("://") + 3);
                                link = prePath.Substring(0, prePath.IndexOf("://") + 3) + tempPre.Substring(0, tempPre.IndexOf("/")) + link;
                            }
                        }
                        if (link != null && link.Length > 7 && link.Length < 2048)
                        {
                            Guid link_id = insertSayfa(link, title);
                            PrepareKeywords(link, link_id);
                            insertSayfaAnahtari(insertAnahtarIfade(link), link_id);
                        }
                    }
                    catch (Exception e)
                    {
                        ErrorHandler(e, null);
                    }
                }
            }
        }
        static Guid NextLink()
        {
            try
            {
                using (DBDataContext db = new DBDataContext())
                {
                    WebSayfa sayfa = db.WebSayfalar.FirstOrDefault(l => l.scanned == false);
                    if (sayfa != null)
                        return sayfa.Id;
                    RequestHandler("No link in queue! Specify one (http://example.com): ");
                    string url = Console.ReadLine();
                    Guid link_id = insertSayfa(url, null);
                    insertSayfaAnahtari(insertAnahtarIfade(url), link_id);
                    PrepareKeywords(url, link_id);
                    return link_id;
                }
            }
            catch (Exception e)
            {
                ErrorHandler(e, null);
            }
            return Guid.Empty;
        }
        static Guid insertSayfa(string url, string title)
        {
            try
            {
                using (DBDataContext db = new DBDataContext())
                {
                    WebSayfa sayfa = db.WebSayfalar.FirstOrDefault(l => l.url == url);
                    if (sayfa != null)
                        return sayfa.Id;
                    Uri uri = new Uri(url);
                    sayfa = new WebSayfa()
                    {
                        Id = Guid.NewGuid(),
                        url = url,
                        website_id = insertSite(uri),
                        path = uri.AbsolutePath,
                        query = uri.Query,
                        uzanti = System.IO.Path.GetExtension(uri.AbsolutePath),
                        scanned = false
                    };
                    if (title != null)
                    {
                        sayfa.baslik = title;
                    }
                    db.WebSayfalar.InsertOnSubmit(sayfa);
                    db.SubmitChanges();
                    if (Guid.Empty != sayfa_id)
                    {
                        SiteSitesi ss = new SiteSitesi()
                        {
                            Id = Guid.NewGuid(),
                            site1 = sayfa_id,
                            site2 = sayfa.Id
                        };
                        db.SiteSiteleri.InsertOnSubmit(ss);
                        db.SubmitChanges();
                    }
                    WarningHandler(url);
                    return sayfa.Id;
                }
            }
            catch (Exception e)
            {
                ErrorHandler(e, null);
            }
            return Guid.Empty;
        }
        static Guid insertSite(Uri uri)
        {
            try
            {
                using (DBDataContext db = new DBDataContext())
                {
                    WebSite site = db.WebSiteler.FirstOrDefault(h => h.url == uri.Authority);
                    if (site != null)
                    {
                        return site.Id;
                    }
                    site = new WebSite()
                    {
                        Id = Guid.NewGuid(),
                        url = uri.Authority
                    };
                    db.WebSiteler.InsertOnSubmit(site);
                    db.SubmitChanges();
                    return site.Id;
                }
            }
            catch (Exception e)
            {
                ErrorHandler(e, null);
            }
            return Guid.Empty;
        }
        static Guid insertAnahtarIfade(string word)
        {
            word = word.Substring(0, Math.Min(2048, word.Length));
            try
            {
                using (DBDataContext db = new DBDataContext())
                {
                    AnahtarIfade ifade = db.AnahtarIfadeler.FirstOrDefault(k => k.ifade == word);
                    if (ifade != null)
                        return ifade.Id;
                    ifade = new AnahtarIfade()
                    {
                        Id = Guid.NewGuid(),
                        ifade = word
                    };
                    db.AnahtarIfadeler.InsertOnSubmit(ifade);
                    db.SubmitChanges();
                    return ifade.Id;
                }
            }
            catch (Exception e)
            {
                ErrorHandler(e, word);
            }
            return Guid.Empty;
        }
        static Guid insertSayfaAnahtari(Guid keyword_id, Guid link_id, double puan = 10)
        {
            try
            {
                using (DBDataContext db = new DBDataContext())
                {
                    SayfaAnahtari sayfaAnahtari = db.SayfaAnahtarlari.FirstOrDefault(l => l.ifade_id == keyword_id && l.sayfa_id == link_id);
                    if (sayfaAnahtari != null)
                        return sayfaAnahtari.Id;
                    sayfaAnahtari = new SayfaAnahtari()
                    {
                        Id = Guid.NewGuid(),
                        ifade_id = keyword_id,
                        sayfa_id = link_id,
                        puan = puan
                    };
                    db.SayfaAnahtarlari.InsertOnSubmit(sayfaAnahtari);
                    db.SubmitChanges();
                    return sayfaAnahtari.Id;
                }
            }
            catch (Exception e)
            {
                ErrorHandler(e, null);
            }
            return Guid.Empty;
        }
        static void PrepareKeywords(string text, Guid link_id)
        {
            if (text == null)
                return;
            MatchCollection matches = Regex.Matches(text, @"\b[\w]{2,}\b", RegexOptions.IgnoreCase);
            insertAnahtarIfadeler(matches, link_id);
        }
        static void insertAnahtarIfadeler(MatchCollection keywords, Guid link_id)
        {
            foreach (Match match in keywords)
            {
                string word = match.Value;
                try
                {
                    insertSayfaAnahtari(insertAnahtarIfade(word), link_id);
                }
                catch (Exception e)
                {
                    ErrorHandler(e, link_id + " " + word);
                }
            }
        }
        static void ErrorHandler(Exception e, object obj)
        {
            if (showErrors)
            {
                ColorfulConsole.Write.error("[  error  ] ");
                ColorfulConsole.WriteLine.error(e.Message);
                ColorfulConsole.WriteLine.error(e.StackTrace + "\n\n");
                if (obj != null)
                {
                    ColorfulConsole.WriteLine.error("------------------------------------------------------------");
                    ColorfulConsole.WriteLine.error(obj);
                    ColorfulConsole.WriteLine.error("------------------------------------------------------------\n\n");
                }
                Console.ReadKey();
            }
        }
        static void WarningHandler(object text)
        {
            ColorfulConsole.Write.warning("[ warning ] ");
            ColorfulConsole.WriteLine._default(text);
        }
        static void SuccessHandler(object text)
        {
            ColorfulConsole.Write.success("[ success ] ");
            ColorfulConsole.WriteLine._default(text);
        }
        static void InfoHandler(object text)
        {
            ColorfulConsole.Write._default("[  info   ] ");
            ColorfulConsole.WriteLine._default(text);
        }
        static void RequestHandler(object text)
        {
            ColorfulConsole.Write.primary("[ request ] ");
            ColorfulConsole.Write.warning(text);
        }
    }
}
