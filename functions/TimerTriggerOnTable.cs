#r "Microsoft.WindowsAzure.Storage"
#r "Newtonsoft.Json"

using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using Microsoft.Azure; 
using Microsoft.WindowsAzure.Storage; 
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;

// First method to run
public static void Run(TimerInfo myTimer, TraceWriter log)
{
   
    // Get storage account credential
    CloudStorageAccount storageAccount = GetCloudStorageAccount();

    // Create cloud table client
    CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

    // Create the CloudTable object that represents the "people" table.
    CloudTable table = tableClient.GetTableReference("MarketplaceLeads");

    // filter on data
    string filter = TableQuery.CombineFilters(
        TableQuery.GenerateFilterConditionForDate("Timestamp", QueryComparisons.GreaterThanOrEqual,  DateTime.UtcNow.AddMinutes(-10)),
              TableOperators.And,
              TableQuery.GenerateFilterConditionForDate("Timestamp", QueryComparisons.LessThanOrEqual, DateTime.UtcNow ));

    // querry    
    TableQuery<MarketplaceLeadsEntity> query = new TableQuery<MarketplaceLeadsEntity>().Where(filter);

    // Print the fields for each customer.
    foreach (MarketplaceLeadsEntity entity in table.ExecuteQuery(query))
    {
        //log.Info($"Entity: {JsonConvert.SerializeObject(entity)}");

        CustomerInfo customerInfo = JsonConvert.DeserializeObject<CustomerInfo>(entity.CustomerInfo);

        //log.Info(GetTimeStampString(entity.ETag));      
        
        customerInfo.TimeStamp = GetTimeStampString(entity.ETag);

        //log.Info( customerInfo.TimeStamp);

        customerInfo.PartitionKey =entity.PartitionKey; 
        
        log.Info($" Customer info {JsonConvert.SerializeObject(customerInfo)}");

        if(customerInfo != null){

            // validation for data
             string pattern = @"([a-z0-9]{8}[-][a-z0-9]{4}[-][a-z0-9]{4}[-][a-z0-9]{4}[-][a-z0-9]{12})";
             MatchCollection mc =  Regex.Matches(customerInfo.Email, pattern);

            log.Info($"Count: {mc.Count} Product Id: {entity.ProductId} LeadSource: {entity.LeadSource}");

             //if(mc.Count > 0 || entity.ProductId != "redhat.openshift-test-drive" || entity.LeadSource != "TestDrive-StartTestDrive|Red Hat OpenShift")
             //if(mc.Count > 0 || entity.ProductId != "redhat.openshift-test-drive" || entity.LeadSource != "AzureMarketplace-StartTestDrive|Red Hat OpenShift")
             if(mc.Count > 0 || entity.ProductId != "redhat.openshift-container-platform" || entity.LeadSource != "AzureMarketplace-StartTestDrive|Red Hat OpenShift Container Platform")
                continue;

            bool isSuccess = PostData(customerInfo);

             log.Info($"Partition Key: {entity.PartitionKey}, Status: {isSuccess}");

            if(!isSuccess)
                CreateFailedItemQueueMessage(customerInfo);
            else
                log.Info($"Processed item with PartitionId: {entity.PartitionKey}");
        }
    }    
    
    log.Info($"C# Timer trigger function executed at: {DateTime.Now}");
}

// Send data to rest endpoint
public static bool PostData(CustomerInfo info)
{   
    using (var client = new HttpClient())
    {  
        try{ 

           string url =  "";

            
            HttpResponseMessage response = client.GetAsync(url).Result;
            if((int)response.StatusCode == 200){
                Console.WriteLine($"POST METHOD CALLED FOR: {info.Email}");
                return true;  
            }
               
        }
        catch{
            return false;
        }     
    }        
    return false;    
}
    



public static void CreateFailedItemQueueMessage(CustomerInfo info){
    // Get storage account credential
    CloudStorageAccount storageAccount = GetCloudStorageAccount();

    // Create the queue client
    CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

    // Retrieve a reference to a queue
    CloudQueue queue = queueClient.GetQueueReference("failed-items");
    //CloudQueue queue = queueClient.GetQueueReference("faileditems");
   
    // Create the queue if it doesn't already exist.
    queue.CreateIfNotExists();

    // Create a message and add it to the queue.
    CloudQueueMessage message = new CloudQueueMessage(JsonConvert.SerializeObject(info));

    // send the message
    queue.AddMessage(message);
}


// get value from app setting config
private static string GetEnvironmentVariable(string name)
{
    return System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
}

private static CloudStorageAccount GetCloudStorageAccount()
{
    string storageConnectionString = GetEnvironmentVariable("SourceConnectionString");
   
    // Parse the connection string and return a reference to the storage account.
    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);

    return storageAccount;
}

private static string GetTimeStampString(string dateTimeString)
{
    string temp = dateTimeString.Split('\'', '\'')[1];
    temp = temp.Replace("%3A", ":");

    TimeZoneInfo easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");   
    return TimeZoneInfo.ConvertTime(Convert.ToDateTime(temp), easternZone).ToString("yyyy-MM-dd HH\\:mm\\:ss");
}

// Table entity
public class MarketplaceLeadsEntity : TableEntity
{
    public string RowKey { get; set; }
    public DateTime Timestamp { get; set; }
    public string ProductId { get; set; }
    public string CustomerInfo { get; set; }
    public string LeadSource { get; set; }
    public string ActionCode { get; set; }
    public string PublisherDisplayName { get; set; }
    public string OfferDisplayName { get; set; }
    public string CreatedTime { get; set; }
    public string Description { get; set; }
}

// Customer info
public class CustomerInfo
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public string Country { get; set; }
    public string Company { get; set; }
    public string Title { get; set; }
    public string TimeStamp{get;set;}
    public string PartitionKey{get;set;}
}

