using System.Net.Http;
using System.Net;
using System.Text.Json.Nodes;
using System.ComponentModel.DataAnnotations;

namespace GoDaddyDynDNSClient
{
    class Program
    {
        static async Task Main()
        {

            //declare strings for Domain and API Information
            string myDomain = "";
            string myHostname = "";
            string apiKey = "";
            string apiSecret = "";
            try
            {
                //Pull variables from variables.txt file
                var dic = File.ReadAllLines("variables.txt")
                              .Select(l => l.Split(new[] { '=' }))
                              .ToDictionary(s => s[0].Trim(), s => s[1].Trim());

                myDomain = dic["myDomain"];
                myHostname = dic["myHostname"];
                apiKey = dic["apiKey"];
                apiSecret = dic["apiSecret"];
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to Get Variables from variables.txt, Error: " + e.Message);
            }

            //Build API Url from variables above
            string apiURL = "";
            if (myHostname == "")
            {
                apiURL = "https://api.godaddy.com/v1/domains/" + myDomain + "/records/A/";
            }
            else
            {
                apiURL = "https://api.godaddy.com/v1/domains/" + myDomain + "/records/A/" + myHostname;
            }
            Console.WriteLine("apiURL = " + apiURL);

            // IP from GoDaddy
            string goDaddyIP;

            //Set lastIP to variable, in case file isnt read
            string lastIP = "0.0.0.0";

            //read Lastip.txt file to see last ip that was fetched and updated
            try
            {
                StreamReader sr = new StreamReader("lastip.txt");
                //Read first Line of text
                lastIP = sr.ReadLine();
                sr.Close();
                Console.WriteLine("LastIP from lastip.txt: " + lastIP);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception while Reading lastip.txt: " + e.Message);
            }

            //Retrieve Current Public IP from ipify.org
            string currentIP = await GetExternalIpAsync();

            //Display CurrentIP and LastIP in Console
            Console.WriteLine("Current Public IP is : " + currentIP);
            Console.WriteLine("Last Public IP is : " + lastIP);

            if (currentIP == lastIP)
            {
                //If current IP equals last retrieved IP
                //No need to reach out to GoDaddy and waste API call if IP hasnt changed since last check-in
                Console.WriteLine("IPs are Equal, no need to update!");
            }

            else if (currentIP != lastIP)
            { // Current IP does not equal to last IP, check with GoDaddy to see if IP is different there
                Console.WriteLine("IPs are not equal, reaching out to Godaddy to confirm DNS info");
                goDaddyIP = "";
                try
                {
                    //here is a try
                    goDaddyIP = await GetDnsIpAsync(myDomain, myHostname, apiKey, apiSecret);
                    Console.WriteLine("GoDaddy IP is " + goDaddyIP);
                }
                catch (Exception e)
                {
                    //here is the catch
                    Console.WriteLine("There was an error checking in with Godaddy API, Error: " + e.Message);

                }
                //Compare Current IP with GoDaddy IP
                if (currentIP == goDaddyIP)
                {
                    //If current ip and godaddy IP are same. update lastip.txt to reflect
                    Console.WriteLine("Current IP and GoDaddy IP are the same. Updating LastIP...");

                    //Update Local file to reflect current IP 
                    try
                    {
                        //Open File for Writing and Overwrite current file
                        StreamWriter sw = new StreamWriter("lastip.txt", false);
                        //Write Current IP to File
                        sw.Write(currentIP);
                        //Close the File
                        sw.Close();
                        Console.WriteLine("Last IP Updated to " + currentIP + " Successfully!");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Exception: " + e.Message);
                    }


                }
                else if (currentIP != goDaddyIP)
                {
                    // Current IP doesnt equal GoDaddy IP, Update DNS record
                    goDaddyIP = await GetDnsIpAsync(myDomain, myHostname, apiKey, apiSecret);
                    Console.WriteLine("Current IP is " + currentIP);
                    Console.WriteLine("GoDaddy IP is " + goDaddyIP);
                    Console.WriteLine("Current IP and GoDaddy IP are different! Updating IP At GoDaddy");

                    //Update IP At GoDaddy
                    Console.WriteLine("IP address has changed. Updating GoDaddy DNS record...");

                    try
                    {
                        await UpdateDnsRecordAsync(myDomain, myHostname, apiKey, apiSecret, currentIP);
                        Console.WriteLine("DNS Record at GoDaddy Updated Successfully!");

                        //Write to lastip.txt with current IP that has been updated at GoDaddy
                        try
                        {
                            //Open File for Writing and Overwrite current file
                            StreamWriter sw = new StreamWriter("lastip.txt", false);
                            //Write Current IP to File
                            sw.Write(currentIP);
                            //Close the File
                            sw.Close();
                            Console.WriteLine("Last IP Updated to " + currentIP + " Successfully!");
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Error Updating lastip.txt, Exception: " + e.Message);
                        }

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error Updating IP At GOdaddy, Exception: " + e.Message);
                    }

                }
                else
                {
                    // Error in IP checking
                    Console.WriteLine("There was an error in comparing Current IP to GoDaddy IP, please investigate!");
                }
            }
            else
            {
                Console.WriteLine("THere was an error checking Last IP and Current IP, please investigate!");
            }

            //Various Functions
            static async Task<string> GetExternalIpAsync()
            {
                //Function to get Public IP Address of client
                using var client = new HttpClient();
                return await client.GetStringAsync("https://api.ipify.org");
            }

            static async Task<string> GetDnsIpAsync(string domain, string hostname, string apiKey, string apiSecret)
            {
                //Function to Get Current DNS IP from GoDaddy
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Authorization", $"sso-key {apiKey}:{apiSecret}");
                client.DefaultRequestHeaders.Add("Accept", "application/json");

                string url = $"https://api.godaddy.com/v1/domains/{domain}/records/A/{hostname}";
                Console.WriteLine("URL = " + url);
                string response = await client.GetStringAsync(url);
                var json = JsonArray.Parse(response);
                return json[0]["data"].ToString();
            }

            static async Task UpdateDnsRecordAsync(string domain, string hostname, string apiKey, string apiSecret, string newIp)
            {
                //Function to Update DNS IP at GoDaddy
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Authorization", $"sso-key {apiKey}:{apiSecret}");
                client.DefaultRequestHeaders.Add("Accept", "application/json");

                string url = $"https://api.godaddy.com/v1/domains/{domain}/records/A/{hostname}";
                var content = new StringContent($"[{{\"data\": \"{newIp}\"}}]", System.Text.Encoding.UTF8, "application/json");

                var response = await client.PutAsync(url, content);
                response.EnsureSuccessStatusCode();
            }


            // Need to add Logging Function to Log Successes and Failures with Exceptions
        }
    }
}