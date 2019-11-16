using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PuppeteerSharp;
using System;
using System.IO;
using System.Threading.Tasks;

namespace docker
{
    public static class HtmlToPdf
    {
        [FunctionName("HtmlToPdf")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultRevision);
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            dynamic html = data?.html;
            string local = data.local;


            using (Browser browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" },
                DefaultViewport = new ViewPortOptions
                {
                    Width = 2000,
                    Height = 800
                }
            }))
            using (Page page = await browser.NewPageAsync())
            {
                if (Convert.ToBoolean(local))
                {
                    string BaseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    html = File.ReadAllText("test_html.html");
                }
                await page.SetContentAsync(html);
                string result = await page.GetContentAsync();
                await page.PdfAsync("test.pdf");
                byte[] content = File.ReadAllBytes("test.pdf");

                //SaveHtmlToDB(result);
                System.Net.Mime.ContentDisposition cd = new System.Net.Mime.ContentDisposition
                {
                    FileName = "PDF TEST",
                    Inline = false  // false = prompt the user for downloading;  true = browser to try to show the file inline
                };

                return new FileContentResult(content, "application/pdf");
            }

        }
    }
}
