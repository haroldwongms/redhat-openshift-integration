#r "Newtonsoft.Json"
#r "SendGrid"

using System;
using Newtonsoft.Json;
using SendGrid;
using SendGrid.Helpers.Mail;


public static void Run(string myQueueItem, TraceWriter log, out Mail message)
{
    log.Info(myQueueItem);

    EmailModel email = JsonConvert.DeserializeObject<EmailModel>(myQueueItem);

    message = new Mail
    {        
        //Subject = "Azure news"  
        Subject = email.Subject        
    };

    var personalization = new Personalization();
    // change to email of recipient
    personalization.AddTo(new Email("abc@xyz.com"));   
    personalization.AddCc(new Email("abc@xyz.com"));   

    Content content = new Content
    {
        Type = "text/html",
        Value = email.Body
        //Value = "Test"
    };
    
    message.AddContent(content);
    message.AddPersonalization(personalization);

    log.Info($"C# Timer trigger function executed at: {DateTime.Now}");
}


public class EmailModel
{
    public string Subject {get; set;}
    public string Body {get; set;}
}
