﻿using System.Text.Json.Serialization;

namespace WebMason_final.Server.Models
{
    public class RegisterModel
    {
        [JsonPropertyName("username")]
        public string Username { get; set; }
        
        [JsonPropertyName("email")]
        public string Email { get; set; }
        
        [JsonPropertyName("password")]
        public string Password { get; set; }
    }
}

