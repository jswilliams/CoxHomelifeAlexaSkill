using CoxHomelifeAlexaSkill.Domain;
using CoxHomelifeAlexaSkill.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Http;

namespace CoxHomelifeAlexaSkill.Controllers
{
    public class AlexaController : ApiController
    {
        private string _allowedAmazonUserId = ConfigurationManager.AppSettings["Amazon_UserId"].ToString();

        [HttpPost, Route("api/alexa/securitysystem")]
        public dynamic SecuritySystem([FromBody] AlexaSkillRequestModel request)
        {
            var coxHomelifeService = new CoxHomelifeService();
            CoxServiceResponse coxServiceResponse = null;

            // Check if the requesting user id is allowed to use this skill
            var intent = request.Request.Intent.Name;
            var fromUser = request.Session.User.UserId;
            if(fromUser != _allowedAmazonUserId)
            {
                return null;
            }
            
            // Check which intent the user wants and act on it
            if(intent == "DisarmIntent")
            {
                coxServiceResponse = coxHomelifeService.Disarm();
            }
            else if(intent == "ArmStayIntent")
            {
                ArmType armType = ArmType.STAY;

                coxServiceResponse = coxHomelifeService.Arm(armType);
            }
            else if(intent == "ArmNightIntent")
            {
                ArmType armType = ArmType.NIGHT;

                coxServiceResponse = coxHomelifeService.Arm(armType);
            }
            else if(intent == "ArmAwayIntent")
            {
                ArmType armType = ArmType.AWAY;

                coxServiceResponse = coxHomelifeService.Arm(armType);
            }
            else if(intent == "ZoneIntent")
            {
                var zone = request.Request.Intent.Slots["Zone"].Value;
                var onOrOff = request.Request.Intent.Slots["OnOff"].Value;

                coxServiceResponse = coxHomelifeService.ChangeZone(zone, onOrOff);
            }

            // Create the response and return to amazon
            var alexaResponse = CreateAlexaResponseFromCoxServiceResponse(coxServiceResponse);
            return alexaResponse;
        }

        private dynamic CreateAlexaResponseFromCoxServiceResponse(CoxServiceResponse coxServiceResponse)
        {
            var alexaResponse = new
            {
                version = "1.0",
                sessionAttributes = new
                {

                },
                response = new
                {
                    outputSpeech = new
                    {
                        type = "PlainText",
                        text = coxServiceResponse.AlexaSpokenResponse // phrase that the Echo will respond to the user with
                    },
                    card = new
                    {
                        type = "Simple",
                        title = coxServiceResponse.AlexaAppCardTitle, // this will show up as a card inside the Amazon Alexa mobile app
                        content = coxServiceResponse.AlexaAppCardText
                    },
                    shouldEndSession = true
                }
            };

            return alexaResponse;
        }
    }
}