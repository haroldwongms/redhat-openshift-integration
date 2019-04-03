
### Overview
All leads from Azure Test Drives are stored in a table. 
System sends all leads received to Eloqua site by calling an endpoint with appropriate values in correct format.
It notifies one or more users via email for all failed requests. 
In case of failure to send the leads, system retries again in future and keeps doing till it clears all backlogs. 

### Architecture Diagram:
![Alt text](intgration.png?raw=true "Architecture Diagram")

### Resources:
•	Azure Storage (Table - 1 & Queue - 2)
•	Azure Function - 3
•	SendGrid

### Eloqua URL Format:  
{elqSiteURL}?elqSiteID={elqSiteID}&elqFormName=sysgain-integration&C_FirstName={info.FirstName}&C_LastName={info.LastName}&C_EmailAddress={info.Email}&C_BusPhone={info.Phone}&C_Company={info.Company}&C_Title={info.Title}&C_Country={info.Country}&A_Timestamp={info.TimeStamp}&F_FormData_Trigger=Test Drive Started on Azure"
