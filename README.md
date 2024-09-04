# GoDaddyDyDNSClient
Dynamic DNS Client for use with GoDaddy's API.

Created with Visual Studio code, Written in C#. 

Generate API Key:
API key needs to be generated at https://developer.godaddy.com/keys.

Update the Variables.txt file with Domain info and API information:
myDomain = domain.com
myHostname = hostname
apiKey = apikey
apiSecret = apisecret
(URL will be hostname.domain, i.e. subdomain.domain.com)

The app will check lastip.txt file inside the project folder to see if the current IP is different from the Last IP that was fetched. If it is the same, then no need to waste API checks with Godaddy. If the IPs are different, the app will reach out to GoDaddy to check what A record is and update if different. It will also update the lastip.txt file for the next run. 
