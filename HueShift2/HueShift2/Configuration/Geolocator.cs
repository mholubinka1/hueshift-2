﻿using HueShift2.Configuration.Model;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace HueShift2.Configuration
{
    public class Geolocator : IGeoLocator
    {
        private readonly IConfigurationSection config;

        public Geolocator(IConfigurationSection config)
        {
            if (config == null) throw new ArgumentNullException();
            this.config = config;
        }

        public async Task<Geolocation> Get()
        {
            //confirm config is present
            var geolocationUri = new Uri(config["Uri"] + config["Key"]);
            string geolocationResponse;
            using (var client = new HttpClient())
            {
                geolocationResponse = await client.GetStringAsync(geolocationUri);
            }
            dynamic response = JObject.Parse(geolocationResponse);
            return new Geolocation(
                (double) response.latitude,
                (double) response.longitude);
        }
    }
}
