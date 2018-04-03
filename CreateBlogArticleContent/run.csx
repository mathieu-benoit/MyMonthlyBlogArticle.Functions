#r "Newtonsoft.Json"
#r "Microsoft.WindowsAzure.Storage"

#load "../Shared/FeedEntity.csx"

using System;
using System.Net; 
using System.Text;
using Newtonsoft.Json;

using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage;
using Microsoft.ApplicationInsights;

static Regex AppDevKeyWordsRegex = new Regex("(App Service|Functions|Logic Apps|DevOps|VSTS|Visual Studio|Application Insights|Web|Media|Xamarin|Mobile|ASP.NET|CDN|Java|.NET|Azure Search|Azure Redis Cache|DevTest Labs|API Management|Container|Service Fabric|ACS|Sitecore|Video Indexer|TFS)");
static Regex InfraKeyWordsRegex = new Regex("(Azure Stack|ExpressRoute|Virtual Machine|VM|Azure Batch|Availability Zones|Azure Automation|Azure Site Recovery|Azure Storage|Azure Log Analytics|OMS|Azure Monitor|Azure Cost Management|Load Balancer|DNS|Network|Traffic Manager|VNet|StorSimple|Backup|Service Health|Blockchain|Azure Migrate|Reserved Instance)");
static Regex DataAIKeyWordsRegex = new Regex("(Cosmos DB|CosmosDB|SQL|IoT|Cognitive Services|PowerBI|Power BI|Data Lake|Azure Analysis Services|Database|HDInsight|Machine Learning|Data Factory|Stream Analytics|Time Series Insights|MySQL|PostgreSQL|Data Warehouse|Event Grid|Event Hub|Service Bus|Databricks)");
static Regex SecurityKeyWordsRegex = new Regex("(Security Center|GDPR|Azure Active Directory|AAD|AD DS|SOC|Azure Information Protection|Azure AD|EMS|Traffic Analytics|Azure Advanced Threat Protection)");

public static var telemetry = new TelemetryClient()
{
    InstrumentationKey = Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY")
};

public static async Task<object> Run(HttpRequestMessage req, TraceWriter log)
{
    dynamic requestBody = await req.Content.ReadAsAsync<object>();
    string date = requestBody?.date;
    var blogArticleDate = GetBlogArticleDate(date);
    log.Info($"Current date: {blogArticleDate.ToString()}");
    var blogArticleTitle = GetBlogArticleTitle(blogArticleDate);
    var blogArticleContent = GetBlogArticleContent(blogArticleDate);
    return req.CreateResponse(HttpStatusCode.OK, new
    {
        title = blogArticleTitle,
        content = blogArticleContent
    });
}

static DateTime GetBlogArticleDate(string date)
{
    var currentDate = !string.IsNullOrEmpty(date) ? DateTime.Parse(date) : DateTime.UtcNow;
    return currentDate.Day == 1 ? currentDate.AddMonths(-1) : currentDate;
}

static string GetBlogArticleTitle(DateTime currentDate)
{
    return $"Microsoft Azure - News & Updates - {currentDate.ToString("MMMM yyyy")}";
}

static string GetBlogArticleContent(DateTime currentDate)
{
    var builder = new StringBuilder();
    
    //Header
    builder.Append($"<br /><br /><div class=\"separator\" style=\"clear:both;text-align:center;\"><a href=\"https://3.bp.blogspot.com/-4STUQFrmLaw/WlyVADOjSTI/AAAAAAAAQMg/wOElBvJG8iMzEnphozggFw5cSxbKcJnpgCLcBGAs/s1600/IMG_20180114_122358.jpg\" imageanchor=\"1\"style=\"margin-left:1em; margin-right:1em;\"><img border=\"0\" data-original-height=\"987\" data-original-width=\"1600\" height=\"197\" src =\"https://3.bp.blogspot.com/-4STUQFrmLaw/WlyVADOjSTI/AAAAAAAAQMg/wOElBvJG8iMzEnphozggFw5cSxbKcJnpgCLcBGAs/s1600/IMG_20180114_122358.jpg\" width=\"320\" /></a ></div >");

    //Body
    var startTime = DateTime.UtcNow;
    var timer = System.Diagnostics.Stopwatch.StartNew();
    var storageAccountConnectionString = Environment.GetEnvironmentVariable("RssFeedsTableStorageConnectionString");
    var storageAccount = CloudStorageAccount.Parse(storageAccountConnectionString);
    var tableClient = storageAccount.CreateCloudTableClient();
    var table = tableClient.GetTableReference("RssFeeds");
    var query = new TableQuery<FeedEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, currentDate.ToString("yyyy-MM")));
    var results = table.ExecuteQuery(query).OrderByDescending(f => f.Date);
    var resultsCount = results.Count();
    telemetry.TrackDependency("TableStorage", "GetFeedsForCurrentMonth", startTime, timer.Elapsed, true);
    var appDevStringBuilder = new StringBuilder();
    var infraStringBuilder = new StringBuilder();
    var dataAiStringBuilder = new StringBuilder();
    var securityStringBuilder = new StringBuilder();
    var miscStringBuilder = new StringBuilder();
    foreach (var feed in results)
    {
        var feedInHtml = $"<a href=\"{feed.Link}\">{DateTime.Parse(feed.Date).ToString("dd")}</a> - {feed.Title}";
        if (AppDevKeyWordsRegex.IsMatch(feed.Title))
        {
            appDevStringBuilder.Append($"<br />{feedInHtml}");
        }
        else if (DataAIKeyWordsRegex.IsMatch(feed.Title))
        {
            dataAiStringBuilder.Append($"<br />{feedInHtml}");
        }
        else if (InfraKeyWordsRegex.IsMatch(feed.Title))
        {
            infraStringBuilder.Append($"<br />{feedInHtml}");
        }
        else if (SecurityKeyWordsRegex.IsMatch(feed.Title))
        {
            securityStringBuilder.Append($"<br />{feedInHtml}");
        }
        else
        {
            miscStringBuilder.Append($"<br />{feedInHtml}");
        }
    }

    builder.Append($"<br />Microsoft Azure news, updates and announcements ({resultsCount} entries) for {currentDate.ToString("MMMM yyyy")}:");
    builder.Append("<br /><br /><b>Application Development:</b><br />");
    builder.Append(appDevStringBuilder.ToString());
    builder.Append("<br /><br /><b>Data Platform & AI:</b><br />");
    builder.Append(dataAiStringBuilder.ToString());
    builder.Append("<br /><br /><b>Infrastructure:</b><br />");
    builder.Append(infraStringBuilder.ToString());
    builder.Append("<br /><br /><b>Security:</b><br />");
    builder.Append(securityStringBuilder.ToString());
    builder.Append("<br /><br /><b>General:</b><br />");
    builder.Append(miscStringBuilder.ToString());

    //Footer
    builder.Append($"<br /><br />Did you miss the previous ones? <a href=\"https://alwaysupalwayson.blogspot.com/search/label/News%20and%20Updates\">Check them out</a>!");
    builder.Append($"<br /><br />Enjoy!");
    builder.Append($"<br /><br /><i>This blog article has been powered by Azure Logic Apps, Azure Functions and Azure Table Storage, <a href=\"https://alwaysupalwayson.blogspot.com/2017/08/my-monthly-azure-news-updates-powered.html\">check out the story</a></i>!");
    
    return builder.ToString();
}
