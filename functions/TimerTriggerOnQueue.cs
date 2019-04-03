#r "Microsoft.WindowsAzure.Storage"
#r "Newtonsoft.Json"

using System;
using System.Net.Http;
using Microsoft.Azure; 
using Microsoft.WindowsAzure.Storage; 
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;

public static void Run(TimerInfo myTimer, TraceWriter log)
{
    // Retrieve storage account from connection string.
    CloudStorageAccount storageAccount = GetCloudStorageAccount();

    // Create the queue client.
    CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

    // Retrieve a reference to a container.
    CloudQueue queue = queueClient.GetQueueReference("failed-items");

    // Create the queue if it doesn't already exist
    queue.CreateIfNotExists();

    // Peek at the next message
   // CloudQueueMessage peekedMessage = queue.PeekMessage();

    // object message
    //log.Info(peekedMessage);

    // Display message.
    //if(peekedMessage != null)
       // log.Info(peekedMessage.AsString);

    // Get the next message
    CloudQueueMessage retrievedMessage = queue.GetMessage();

    while(retrievedMessage != null){
        //Process the message in less than 30 seconds, and then delete the message
    
        log.Info(retrievedMessage.AsString);

        CustomerInfo customerInfo = JsonConvert.DeserializeObject<CustomerInfo>(retrievedMessage.AsString);
        //CustomerInfo customerInfo = JsonConvert.DeserializeObject<CustomerInfo>(entity.CustomerInfo);

        bool isSuccess = PostData(customerInfo);

        if(isSuccess){
            queue.DeleteMessage(retrievedMessage);   
        }     
        else
        {
            EmailModel email = new EmailModel();
            email.Subject = "Error occurred!";
            email.Body = $"Eloqua service is not reachable. Failed to send record with PartitionId: {customerInfo.PartitionKey}";
            CreateEmailQueueMessage(email);
            break;
        }
        retrievedMessage = queue.GetMessage();
    }

    log.Info($"C# Timer trigger function executed at: {DateTime.Now}");    
}

public static void CreateEmailQueueMessage(EmailModel email){
    // Get storage account credential
    CloudStorageAccount storageAccount = GetCloudStorageAccount();

    // Create the queue client
    CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

    // Retrieve a reference to a queue
    CloudQueue queue = queueClient.GetQueueReference("emails");
    
   
    // Create the queue if it doesn't already exist.
    queue.CreateIfNotExists();

    // Create a message and add it to the queue.
    CloudQueueMessage message = new CloudQueueMessage(JsonConvert.SerializeObject(email));

    // send the message
    queue.AddMessage(message);
}

// Send data to rest endpoint
public static bool PostData(CustomerInfo info)
{
    using (var client = new HttpClient())
    {  
        try{  
        string url = "";
        HttpResponseMessage response = client.GetAsync(url).Result;
        if((int)response.StatusCode == 200)
            return true;  
        }
        catch{
            return false;
        }     
    }
    return false;
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

public class EmailModel
{
    public string Subject {get; set;}
    public string Body {get; set;}
}
