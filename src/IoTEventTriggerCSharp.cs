using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Data.SqlClient;
using Microsoft.Azure.NotificationHubs;
using System.Net;
using System.IO;
using System.Data;

namespace HubEvent.Function
{
    public static class IoTEventTriggerCSharp
    {
        /*
        private static string notificationhubConnectionstring = "Endpoint=sb://nssmartproduct.servicebus.windows.net/;SharedAccessKeyName=DefaultFullSharedAccessSignature;SharedAccessKey=GIn9mkow+/5boWrrDn54pn/n6gg62NYBiqWISquT4x8="; //notificationhub connection string
        private static string connectionString = "Server=tcp:smartfandb.database.windows.net,1433;Initial Catalog=SmartFanDb;Persist Security Info=False;User ID=xavor123;Password=Xavor@1999;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
        private static string setStatus = "https://smartfan.azurewebsites.net/api/SmartDeviceStatus/SetDeviceStatus";
        private static string setAlarms = "https://smartfan.azurewebsites.net/api/SmartDeviceStatus/SetDeviceAlarms";
        private static string sendAcknowledgement = "https://smartfan.azurewebsites.net/api/Device/SendAcknowledgement/";
        private static string sendDeviceState = "https://smartfan.azurewebsites.net/api/Device/SendDeviceState/";
        private static string speedCommand ="https://smartfan.azurewebsites.net/api/Device/ChangeDeviceSpeed/";        
        private static string powerCommand ="https://smartfan.azurewebsites.net/api/Device/ChangeDevicePowerStatus/";
        */

        private static string baseUrl = "https://fanapistaging.azurewebsites.net";
        private static string connectionString = "Server=tcp:fanserverstaging.database.windows.net,1433;Initial Catalog=smartfan;Persist Security Info=False;User ID=smartfan;Password=Xavor@1999;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
        private static string notificationhubConnectionstring = "Endpoint=sb://nsfanboxnotificationhub.servicebus.windows.net/;SharedAccessKeyName=DefaultFullSharedAccessSignature;SharedAccessKey=1BSSuuC+aUpBvlDqnM6FMzyzAZuSRgHfBb8EO/xtheQ="; //notificationhub connection string
        private static string notificationhubName = "FanBoxNotificationHub";

        private static string setStatus = baseUrl + "/api/SmartDeviceStatus/SetDeviceStatus";
        private static string setAlarms = baseUrl + "/api/SmartDeviceStatus/SetDeviceAlarms";
        private static string sendAcknowledgement = baseUrl + "/api/Device/SendAcknowledgement/";
        private static string sendDeviceState = baseUrl + "/api/Device/SendDeviceState/";
        private static string speedCommand = baseUrl + "/api/Device/ChangeDeviceSpeed/";
        private static string powerCommand = baseUrl + "/api/Device/ChangeDevicePowerStatus/";

        private static ILogger logger;

        //private static SqlConnection  connection;
        [FunctionName("IoTEventTriggerCSharp")]
        public static async Task Run([EventHubTrigger("iothub", Connection = "Connectionstring")] EventData[] events, ILogger log)
        {
            logger = log;
            foreach (var eventData in events)
            {
                try
                {
                    var obj = Guid.NewGuid();
                    //int DeviceID = 0;
                    string messageBody = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);
                    dynamic input = JsonConvert.DeserializeObject(messageBody);
                    log.LogInformation(messageBody);
                    //connection = new SqlConnection(connectionString); 
                    input["id"].Value = obj.ToString();
                    if (string.IsNullOrEmpty(input["id"].Value) || string.IsNullOrEmpty(input["DeviceId"].Value))
                    {
                        continue;
                    }

                    using (var connection = new SqlConnection(connectionString))
                    {
                        try
                        {
                            await connection.OpenAsync();
                            log.LogInformation("DB Connected");
                            var messageType = Convert.ToInt32(input["MessageType"].Value);
                            switch (messageType)
                            {
                                case 0: // PLC data
                                    log.LogInformation("Device Alarm Scenario");
                                    DeviceAlarm(input, messageBody, connection, log);
                                    break;
                                case 1: // Sensor data
                                    log.LogInformation("Device Status Scenario");
                                    await DeviceStatus(input, messageBody, connection, log);
                                    break;
                                case 2:
                                    log.LogInformation("Acknowledgement Scenario");
                                    Acknowledgement(input);
                                    break;
                                case 3:
                                    log.LogInformation("Device State Scenario");
                                    DeviceState(input, log);
                                    break;
                                case 4:
                                    log.LogInformation("Device Auto Command Scenario");
                                    DeviceAutoCommand(input, log);
                                    break;

                            }
                        }
                        catch (Exception e)
                        {
                            log.LogInformation(e.ToString());
                        }
                        finally
                        {
                            connection.Close();
                            connection.Dispose();
                        }
                    }
                    //connection.Close();
                }
                catch (Exception e)
                {
                    log.LogInformation(e.ToString());
                    //connection.Close();
                }
            }
        }

        public static void DeviceAutoCommand(dynamic input, ILogger log)
        {
            var autoFlag = input["auto_flag"].Value.ToString();
            var customerId = input["CustomerId"].Value.ToString();
            var deviceId = input["DeviceId"].Value.ToString();
            var speed = input["speed"].Value.ToString();
            var power = input["power"].Value.ToString();
            var Uri = "";

            if (autoFlag == "1")
            {
                Uri = speedCommand + customerId + "/" + deviceId + "/" + speed;
            }
            else if (autoFlag == "0")
            {
                Uri = powerCommand + customerId + "/" + deviceId + "/" + power;
            }
            var response = RestAPICall("", Uri);
            log.LogInformation("Response from server:" + response);
        }
        public static string RestAPICall(string Data, string URL)
        {
            var request = (HttpWebRequest)WebRequest.Create(URL);
            request.Method = "POST";
            request.ContentType = "application/json";
            request.ContentLength = Data.Length;
            using (var webStream = request.GetRequestStream())
            using (var requestWriter = new StreamWriter(webStream, System.Text.Encoding.ASCII))
            {
                requestWriter.Write(Data);
            }
            try
            {
                var webResponse = request.GetResponse();
                using (var webStream = webResponse.GetResponseStream() ?? Stream.Null)
                using (var responseReader = new StreamReader(webStream))
                {
                    string response = responseReader.ReadToEnd();
                    Console.Out.WriteLine(response);
                    return response;
                }
            }
            catch (Exception ex)
            {
                return "Failure";
            }
        }

        public static async Task DeviceStatus(dynamic input, string data, SqlConnection connection, ILogger log)
        {
            var response = RestAPICall(data, setStatus);
            if (response != "Failure")
            {
                var test = input["DeviceId"].Value;
                var DeviceID = (int)GetDeviceId(test, connection);
                var userMapping = GetUserMappingList(DeviceID, connection);
                var userIdList = GetUserIdList(userMapping);
                var user = GetUserList(userIdList, connection);

                log.LogInformation("Status is pushed to following user: " + user[0]);
                await SendPushMessage(response, user);
            }
            else
            {
                log.LogInformation("Failure");
            }
        }

        public static void DeviceAlarm(dynamic input, string data, SqlConnection connection, ILogger log)
        {
            var title = "";
            var APIresponse = RestAPICall(data, setAlarms);
            if (input["Alarm"].Value.ToString() != "No Alarm")
            {
                title = "Alarm";
            }
            else if (input["Warning"].Value.ToString() != "No Warning")
            {
                title = "Warning";
            }
            else
            {
                return;
            }

            if (APIresponse != "Failure")
            {
                var DeviceUniqueID = input["DeviceId"].Value.ToString();
                var DeviceID = (int)GetDeviceId(input["DeviceId"].Value.ToString(), connection);
                var DeviceName = (string)GetDeviceName(input["DeviceId"].Value.ToString(), connection);
                var response = "{\"message\":\"" + title + " has Occurred in " + DeviceName + "\",\"statusCode\":\"SUCCESS\",\"data\":{\"deviceId\":\"" + DeviceUniqueID + "\",\"isDeviceStatus\":false}}";
                var userMapping = GetUserMappingList(DeviceID, connection);
                var userIdList = GetUserIdList(userMapping);
                var user = GetUserList(userIdList, connection);
                SendPushMessage(response, user, false, title + " has Occurred in " + DeviceUniqueID);
            }
            else
            {
                log.LogInformation("Failure");
            }
        }

        public static void Acknowledgement(dynamic input)
        {
            var CommandID = input["CommandId"].Value.ToString();
            Console.WriteLine(CommandID);
            var Url = sendAcknowledgement + CommandID;
            Console.WriteLine(Url);
            RestAPICall("", Url);
        }

        public static void DeviceState(dynamic input, ILogger log)
        {
            var DeviceId = input["DeviceId"].Value.ToString();
            var CustomerID = input["CustomerId"].Value.ToString();
            string Url = sendDeviceState + CustomerID + '/' + DeviceId;
            log.LogInformation(Url);
            Console.WriteLine(Url);
            var response = RestAPICall("", Url);
            log.LogInformation(response);
        }
        public static List<int> GetUserIdList(SqlDataReader userdata)
        {
            var userList = new List<int>();
            while (userdata.Read())
            {
                userList.Add((int)userdata["userId"]);
            }
            userdata.Close();
            return userList;
        }

        public static List<string> GetUserList(List<int> userdata, SqlConnection connection)
        {

            var userList = new List<string>();
            foreach (var obj in userdata)
            {
                string query = "SELECT UserID from [User] WHERE id = @Id";
                var cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@Id", obj);
                var result = cmd.ExecuteReader();
                while (result.Read())
                {
                    Console.WriteLine(String.Format("{0}", result[0]));
                    userList.Add(result[0].ToString());
                }
                result.Close();
            }
            return userList;
        }

        public static System.Data.SqlClient.SqlDataReader GetUserMappingList(int DeviceId, SqlConnection connection)
        {
            string query = "SELECT UserId from dbo.userdevice WHERE DeviceId = @DeviceId";
            var cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@DeviceId", DeviceId);
            var result = cmd.ExecuteReader();
            return result;
        }

        public static object GetDeviceName(string DeviceId, SqlConnection connection)
        {
            string query = "SELECT Name from dbo.device WHERE DeviceID = @DeviceId";
            var cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@DeviceId", DeviceId);
            object result = null;
            using (var data = cmd.ExecuteReader())
            {
                while (data.Read())
                {
                    Console.WriteLine(String.Format("{0}", data[0]));
                    result = data[0];
                }
                data.Close();
            }
            return result;
        }

        public static object GetDeviceId(string DeviceId, SqlConnection connection)
        {
            string query = "SELECT id from dbo.device WHERE DeviceID = @DeviceId";
            var cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@DeviceId", DeviceId);
            object result = null;
            using (var data = cmd.ExecuteReader())
            {
                while (data.Read())
                {
                    Console.WriteLine(String.Format("{0}", data[0]));
                    result = data[0];
                }
                data.Close();
            }
            return result;
        }

        public static async Task SendPushMessage(string input, List<string> Tags, bool isStatus = true, string AppleAlert = "")
        {
            var ConnectionString = notificationhubConnectionstring;
            string HubName = notificationhubName;
            var message = Newtonsoft.Json.JsonConvert.SerializeObject(input);
            var _hubClient = NotificationHubClient.CreateClientFromConnectionString(ConnectionString, HubName);
            foreach (var obj in Tags)
            {
                var Content = "{\"data\":{\"message\": " + input + "}}";
                var outcome = await _hubClient.SendFcmNativeNotificationAsync(Content, obj);
                string AppleNotificationContent = "";
                if (isStatus == true)
                {
                    AppleNotificationContent = "{\"aps\":{\"alert\":\"DeviceStatus\", \"alert2\":" + input + "}}";
                }
                else
                {
                    AppleNotificationContent = "{\"aps\":{\"alert\":\"" + AppleAlert + "\", \"alert2\":" + input + "}}";
                }
                var outcome2 = await _hubClient.SendAppleNativeNotificationAsync(AppleNotificationContent, obj);
                logger.LogInformation("Following notification is pushed: " + Content + " To Following user: " + obj);
            }
        }

        public static string GetCommaSeparatedIds(List<int> data)
        {
            var idList = "";
            foreach (var id in data)
            {
                if (string.IsNullOrEmpty(idList))
                    idList = "" + id + "";
                else
                    idList = idList + "," + id + "";
            }
            return idList;
        }
    }
}
