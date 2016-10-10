using RestSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CoxHomelifeAlexaSkill.Domain
{
    public class CoxHomelifeService
    {
        private readonly string _username;
        private readonly string _password;
        private readonly string _alarmCode;
        private readonly bool _allowDisarm;

        private RestClient _restClient = new RestClient("https://portal.coxhomelife.com");
        private bool _loggedIn = false;

        private string _jSessionId = "";
        private JObject _loginResponseJObject = null;
        private string _disarmEndpoint = "";
        private string _armEndpoint = "";

        public CoxHomelifeService()
        {
            _username = ConfigurationManager.AppSettings["CoxHomelife_UserName"].ToString();
            _password = ConfigurationManager.AppSettings["CoxHomelife_PasswordHashed"].ToString();
            _alarmCode = ConfigurationManager.AppSettings["CoxHomelife_AlarmCode"].ToString();

            _allowDisarm = Convert.ToBoolean(ConfigurationManager.AppSettings["AllowEchoToDisarmSecuritySystem"].ToString());
        }

        public CoxServiceResponse Arm(ArmType armType)
        {
            var serviceResponse = new CoxServiceResponse();

            if (!_loggedIn)
            {
                LogIn();
            }

            var armTypeString = armType.ToString(); // 3 possible options are "night", "away", "stay"

            var request = new RestRequest(_armEndpoint, Method.POST);

            request.AddParameter("code", _alarmCode);
            request.AddParameter("armType", armTypeString);
            request.AddHeader("X-format", "json");
            request.AddHeader("X-ClientInfo", "7.3.4.90");
            request.AddCookie("JSESSIONID", _jSessionId);

            IRestResponse httpResponse = _restClient.Execute(request);
            
            if (httpResponse.StatusCode == System.Net.HttpStatusCode.Accepted)
            {
                serviceResponse.AlexaSpokenResponse = $"System arming {armTypeString}";
                serviceResponse.AlexaAppCardTitle = $"Armed {armTypeString}";
                serviceResponse.AlexaAppCardText = $"System successfully armed {armTypeString}";
            }
            else
            {
                serviceResponse.AlexaSpokenResponse = $"System failed to arm";
                serviceResponse.AlexaAppCardTitle = $"Failed to arm {armTypeString}";
                serviceResponse.AlexaAppCardText = $"System failed to arm {armTypeString}";
            }

            return serviceResponse;
        }

        public CoxServiceResponse Disarm()
        {
            var serviceResponse = new CoxServiceResponse();

            if (!_allowDisarm)
            {
                serviceResponse.AlexaSpokenResponse = "Sorry but I am not allowed to disarm the security system";
                serviceResponse.AlexaAppCardTitle = "Not allowed to disarm";
                serviceResponse.AlexaAppCardText = "Configuration doesn't allow disarming of the system";
                return serviceResponse;
            }

            if (!_loggedIn)
            {
                LogIn();
            }

            var request = new RestRequest(_disarmEndpoint, Method.POST);

            request.AddParameter("code", _alarmCode);
            request.AddHeader("X-format", "json");
            request.AddHeader("X-ClientInfo", "7.3.4.90");
            request.AddCookie("JSESSIONID", _jSessionId);

            IRestResponse httpResponse = _restClient.Execute(request);

            if (httpResponse.StatusCode == System.Net.HttpStatusCode.Accepted)
            {
                serviceResponse.AlexaSpokenResponse = "System disarmed";
                serviceResponse.AlexaAppCardTitle = "Disarmed";
                serviceResponse.AlexaAppCardText = "System successfully disarmed";
            }
            else
            {
                serviceResponse.AlexaSpokenResponse = $"System failed to arm";
                serviceResponse.AlexaAppCardTitle = "Failed to disarm";
                serviceResponse.AlexaAppCardText = "System failed to disarm";
            }

            return serviceResponse;
        }


        public CoxServiceResponse ChangeZone(string zone, string onOrOff)
        {
            var serviceResponse = new CoxServiceResponse();

            if (!_loggedIn)
            {
                LogIn();
            }

            var zoneEndpoint = GetZoneEndpoint(zone);

            var isBypassed = "false"; // true for bypassed, false for not bypassed
            if(onOrOff.ToLower() == "off")
            {
                isBypassed = "true";
            }
            else
            {
                isBypassed = "false";
            }

            var request = new RestRequest(zoneEndpoint, Method.POST);

            request.AddQueryParameter("value", isBypassed);
            request.AddHeader("X-format", "json");
            request.AddHeader("X-ClientInfo", "7.3.4.90");
            request.AddCookie("JSESSIONID", _jSessionId);

            IRestResponse httpResponse = _restClient.Execute(request);

            if (httpResponse.StatusCode == System.Net.HttpStatusCode.Accepted)
            {
                serviceResponse.AlexaSpokenResponse = $"Zone {zone} turned {onOrOff}";
                serviceResponse.AlexaAppCardTitle = $"Zone {zone} changed";
                serviceResponse.AlexaAppCardText = $"Zone {zone} successfully turned {onOrOff}";
            }
            else
            {
                serviceResponse.AlexaSpokenResponse = $"Zone status failed to change";
                serviceResponse.AlexaAppCardTitle = $"Failed to change zone {zone}";
                serviceResponse.AlexaAppCardText = $"System failed to change {zone} {onOrOff}";
            }

            return serviceResponse;
        }

        private void LogIn()
        {
            var request = new RestRequest("rest/icontrol/login?expand=sites,instances,points,functions", Method.GET);

            request.AddHeader("X-allowNonActivatedLogin", "true");
            request.AddHeader("X-AppKey", "defaultKey");
            request.AddHeader("X-expires", "1800000");
            request.AddHeader("X-format", "json");
            request.AddHeader("X-ClientInfo", "7.3.4.90");
            request.AddHeader("X-login", _username);
            request.AddHeader("X-loginEncoded", "false");
            request.AddHeader("X-password", _password);

            IRestResponse response = _restClient.Execute(request);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var jSessionIdCookie = response.Cookies.FirstOrDefault(x => x.Name == "JSESSIONID");
                if (jSessionIdCookie != null)
                {
                    _jSessionId = jSessionIdCookie.Value;

                    _loginResponseJObject = JObject.Parse(response.Content);
                    _disarmEndpoint = GetDisarmEndpoint(_loginResponseJObject);
                    _armEndpoint = GetArmEndpoint(_loginResponseJObject);
                    _loggedIn = true;
                    return;
                }
            }

            _jSessionId = "";
            _loggedIn = false;
        }

        private string GetDisarmEndpoint(JObject jObject)
        {
            var touchscreenNode = jObject["login"]["instances"]["instance"].FirstOrDefault(x => (string)x["tags"] == "TouchScreen");
            var disarmEndpoint = touchscreenNode["functions"]["function"].FirstOrDefault(x => (string)x["mediaType"] == "panel/disarm")["action"].ToString();

            return disarmEndpoint.Replace("/subscriberPortal/", String.Empty);
        }

        private string GetArmEndpoint(JObject jObject)
        {
            var touchscreenNode = jObject["login"]["instances"]["instance"].FirstOrDefault(x => (string)x["tags"] == "TouchScreen");
            var armEndpoint = touchscreenNode["functions"]["function"].FirstOrDefault(x => (string)x["mediaType"] == "panel/arm")["action"].ToString();

            return armEndpoint.Replace("/subscriberPortal/", String.Empty);
        }

        private string GetZoneEndpoint(string zone)
        {
            var zoneNode = _loginResponseJObject["login"]["instances"]["instance"].FirstOrDefault(x => ((string)x?["name"])?.ToLower() == zone.ToLower());
            if(zoneNode == null)
            {
                return null;
            }

            var zoneEndpoint = zoneNode["points"][0]["href"].ToString();
            return zoneEndpoint.Replace("/subscriberPortal/", String.Empty) + "isBypassed";
        }
    }
}
