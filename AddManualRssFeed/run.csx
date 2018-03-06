#r "Microsoft.WindowsAzure.Storage"

#load "../Shared/FeedEntity.csx"

using System;
using System.Net; 
using System.Text;

using System.Linq;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage;

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log, ICollector<FeedEntity> outputTable) 
{
    dynamic data = await req.Content.ReadAsAsync<object>();
    string title = data?.title;
    string link = data?.link;
    string date = data?.date;

    if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(link) || string.IsNullOrEmpty(date))
    {
        return req.CreateResponse(HttpStatusCode.BadRequest, "Please pass parameters in the request body");
    }

    var type = "manual";
    var rowKey = link.Substring(link.LastIndexOf('/') + 1);
    var partitionKey = date.Substring(0, date.LastIndexOf('-'));

    var feedEntity = new FeedEntity(rowKey, partitionKey) { Title = title, Date = date, Link = link, Type = type };            
    outputTable.Add(feedEntity); 

    return req.CreateResponse(HttpStatusCode.OK, "Thank you for submitting."); 
}
