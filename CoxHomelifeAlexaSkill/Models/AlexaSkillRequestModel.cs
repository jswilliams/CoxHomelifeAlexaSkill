using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoxHomelifeAlexaSkill.Models
{
    public class AlexaSkillRequestModel
    {
        public string Version { get; set; }
        public Session Session { get; set; }
        public RequestBundle Request { get; set; }
    }

    public class Session
    {
        public bool New { get; set; }
        public string SessionId { get; set; }
        public Dictionary<string, object> Attributes { get; set; }
        public Application Application { get; set; }
        public User User { get; set; }
    }

    public class RequestBundle
    {
        public string Type { get; set; }
        public string RequestId { get; set; }
        public DateTime Timestamp { get; set; }
        public Intent Intent { get; set; }
        public string Reason { get; set; }
    }

    public class Intent
    {
        public string Name { get; set; }
        public Dictionary<string, Slot> Slots { get; set; }
    }

    public class Slot
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class Application
    {
        public string ApplicationId { get; set; }
    }

    public class User
    {
        public string UserId { get; set; }
        public string AccessToken { get; set; }
    }
}