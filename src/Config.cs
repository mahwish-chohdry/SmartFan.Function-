using System;
using System.Collections.Generic;
using System.Text;

namespace IoTHubTrigger
{
    public partial class Config
    {
        //APP Function Details
        public const string FunctionNameValue = "IoTEventTriggerCSharp";
        public const string TriggerNameValue = "iothub";
        public const string TriggerConnectionValue = "Connectionstring";
        //API Url
        public const string BaseUrl = "https://fanapistaging.azurewebsites.net";
        //API Endpoints
        public const string SetStatus = "/api/SmartDeviceStatus/SetDeviceStatus";
        public const string SetAlarms = "/api/SmartDeviceStatus/SetDeviceAlarms";
        public const string SendAcknowledgement = "/api/Device/SendAcknowledgement/";
        public const string SendDeviceState = "/api/Device/SendDeviceState/";
        public const string SpeedCommand = "/api/Device/ChangeDeviceSpeed/";
        public const string PowerCommand = "/api/Device/ChangeDevicePowerStatus/";
        //DB connection String
        public static readonly string ConnectionString = "Server=tcp:fanserverstaging.database.windows.net,1433;Initial Catalog=smartfan;Persist Security Info=False;User ID=smartfan;Password=Xavor@1999;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
        //Notification Hub
        public const string NotificationHubConnectionstring = "Endpoint=sb://nsfanboxnotificationhub.servicebus.windows.net/;SharedAccessKeyName=DefaultFullSharedAccessSignature;SharedAccessKey=1BSSuuC+aUpBvlDqnM6FMzyzAZuSRgHfBb8EO/xtheQ=";
        public const string NotificationHubName = "FanBoxNotificationHub";
    }
}
