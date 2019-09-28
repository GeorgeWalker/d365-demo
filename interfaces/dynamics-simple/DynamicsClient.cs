using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace dynamics
{
    public class DynamicsClient
    {
        public static HttpClient GenerateClient(IConfiguration Configuration)
        {
            string dynamicsOdataUri = Configuration["DYNAMICS_ODATA_URI"]; // Dynamics ODATA endpoint

            string token = "";

            if (string.IsNullOrEmpty(dynamicsOdataUri))
            {
                throw new Exception("Configuration setting DYNAMICS_ODATA_URI is blank.");
            }

            // Cloud - x.dynamics.com
            string aadTenantId = Configuration["DYNAMICS_AAD_TENANT_ID"]; // Cloud AAD Tenant ID
            string serverAppIdUri = Configuration["DYNAMICS_SERVER_APP_ID_URI"]; // Cloud Server App ID URI
            string appRegistrationClientKey = Configuration["DYNAMICS_APP_REG_CLIENT_KEY"]; // Cloud App Registration Client Key
            string appRegistrationClientId = Configuration["DYNAMICS_APP_REG_CLIENT_ID"]; // Cloud App Registration Client Id

            // One Premise ADFS (2016)
            string adfsOauth2Uri = Configuration["ADFS_OAUTH2_URI"]; // ADFS OAUTH2 URI - usually /adfs/oauth2/token on STS
            string applicationGroupResource = Configuration["DYNAMICS_APP_GROUP_RESOURCE"]; // ADFS 2016 Application Group resource (URI)
            string applicationGroupClientId = Configuration["DYNAMICS_APP_GROUP_CLIENT_ID"]; // ADFS 2016 Application Group Client ID
            string applicationGroupSecret = Configuration["DYNAMICS_APP_GROUP_SECRET"]; // ADFS 2016 Application Group Secret
            string serviceAccountUsername = Configuration["DYNAMICS_USERNAME"]; // Service account username
            string serviceAccountPassword = Configuration["DYNAMICS_PASSWORD"]; // Service account password


            ServiceClientCredentials serviceClientCredentials = null;
            if (!string.IsNullOrEmpty(appRegistrationClientId) && !string.IsNullOrEmpty(appRegistrationClientKey) && !string.IsNullOrEmpty(serverAppIdUri) && !string.IsNullOrEmpty(aadTenantId))
            // Cloud authentication - using an App Registration's client ID, client key.  Add the App Registration to Dynamics as an Application User.
            {
                var authenticationContext = new AuthenticationContext(
                "https://login.windows.net/" + aadTenantId);
                ClientCredential clientCredential = new ClientCredential(appRegistrationClientId, appRegistrationClientKey);
                var task = authenticationContext.AcquireTokenAsync(serverAppIdUri, clientCredential);
                task.Wait();
                var authenticationResult = task.Result;
                token = authenticationResult.CreateAuthorizationHeader().Substring("Bearer ".Length);
                serviceClientCredentials = new TokenCredentials(token);
            }
            else if (!string.IsNullOrEmpty(adfsOauth2Uri) &&
                !string.IsNullOrEmpty(applicationGroupResource) &&
                !string.IsNullOrEmpty(applicationGroupClientId) &&
                !string.IsNullOrEmpty(applicationGroupSecret) &&
                !string.IsNullOrEmpty(serviceAccountUsername) &&
                !string.IsNullOrEmpty(serviceAccountPassword))
            // ADFS 2016 authentication - using an Application Group Client ID and Secret, plus service account credentials.
            {
                // create a new HTTP client that is just used to get a token.
                var stsClient = new HttpClient();

                //stsClient.DefaultRequestHeaders.Add("x-client-SKU", "PCL.CoreCLR");
                //stsClient.DefaultRequestHeaders.Add("x-client-Ver", "5.1.0.0");
                //stsClient.DefaultRequestHeaders.Add("x-ms-PKeyAuth", "1.0");

                stsClient.DefaultRequestHeaders.Add("client-request-id", Guid.NewGuid().ToString());
                stsClient.DefaultRequestHeaders.Add("return-client-request-id", "true");
                stsClient.DefaultRequestHeaders.Add("Accept", "application/json");

                // Construct the body of the request
                var pairs = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("resource", applicationGroupResource),
                    new KeyValuePair<string, string>("client_id", applicationGroupClientId),
                    new KeyValuePair<string, string>("client_secret", applicationGroupSecret),
                    new KeyValuePair<string, string>("username", serviceAccountUsername),
                    new KeyValuePair<string, string>("password", serviceAccountPassword),
                    new KeyValuePair<string, string>("scope", "openid"),
                    new KeyValuePair<string, string>("response_mode", "form_post"),
                    new KeyValuePair<string, string>("grant_type", "password")
                 };

                // This will also set the content type of the request
                var content = new FormUrlEncodedContent(pairs);
                // send the request to the ADFS server
                var _httpResponse = stsClient.PostAsync(adfsOauth2Uri, content).GetAwaiter().GetResult();
                var _responseContent = _httpResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                // response should be in JSON format.
                try
                {
                    Dictionary<string, string> result = JsonConvert.DeserializeObject<Dictionary<string, string>>(_responseContent);
                    token = result["access_token"];
                    // set the bearer token.
                    serviceClientCredentials = new TokenCredentials(token);
                }
                catch (Exception e)
                {
                    throw new Exception(e.Message + " " + _responseContent);
                }

            }            
            else
            {
                throw new Exception("No configured connection to Dynamics.");
            }

            HttpClient client = new HttpClient();

            client.BaseAddress = new Uri(dynamicsOdataUri);

            string Authorization = $"Bearer {token}";
            client.DefaultRequestHeaders.Add("Authorization", Authorization);
            client.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
            client.DefaultRequestHeaders.Add("OData-Version", "4.0");
            client.DefaultRequestHeaders.Add("Accept", "application/json");

            return client;
        }
    }
}
