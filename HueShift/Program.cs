﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using System.Dynamic;
using Newtonsoft.Json.Linq;
using System.Threading;
using Innovative.SolarCalculator;
using McMaster.Extensions.CommandLineUtils;
using Q42.HueApi.NET;
using Q42.HueApi.Models.Bridge;
using Q42.HueApi;
using System.IO;
using Newtonsoft.Json;

namespace HueShift
{
    public partial class Program
    {
        public static int Main(string[] args)
        {
            var app = new CommandLineApplication();
            app.ValueParsers.Add(new TimeSpanParser());

            app.HelpOption("-h|--help");

            var resetOption = app.Option("-r|--reset", "Clear all saved configuration to defaults.", CommandOptionType.NoValue);
            var noConfigurationSaveOption = app.Option("--do-not-save-config", "Do not save configuration.", CommandOptionType.NoValue);

            var discoverBridgesOption = app.Option("-d|--discover-bridges", "Discover bridges on network even if you already have.", CommandOptionType.NoValue);
            var bridgeHostnameOption = app.Option<string>("--bridge-hostname <hostname>", "Manually enter bridge hostname or ip address.", CommandOptionType.SingleValue);

            var listDevicesOption = app.Option("-l|--list-devices", "List all known devices on network.", CommandOptionType.NoValue);

            var sunsetMustBeAfterOption = app.Option<TimeSpan>("--sunset-must-be-after <Time>", "Lights will not shift to nightime until at least this time.", CommandOptionType.SingleValue);
            var sunsetMustBeBeforeOption = app.Option<TimeSpan>("--sunset-must-be-before <Time>", "Lights will always shift to nighttime even if the sun is still up.", CommandOptionType.SingleValue);

            var sunriseMustBeAfterOption = app.Option<TimeSpan>("--sunrise-must-be-after <Time>", "Lights will not shift to day until at least this time.", CommandOptionType.SingleValue);
            var sunriseMustBeBeforeOption = app.Option<TimeSpan>("--sunrise-must-be-before <Time>", "Lights will always shift to day even if the sun is still down.", CommandOptionType.SingleValue);

            var latitudeOption = app.Option<double>("--latitude <degrees>", "Latitude for calculating sunrise/sunset. Must have longitude", CommandOptionType.SingleValue);
            var longitudeOption = app.Option<double>("--longitude <degrees>", "Longitude for calculating sunrise/sunset. Must have latitude", CommandOptionType.SingleValue);

            var dayColorTemperatureOption = app.Option<int>("--day-color-temperature <temperature>", "Day color temperature. (250)", CommandOptionType.SingleValue);
            var nightColorTemperatureOption = app.Option<int>("--night-color-temperature <temperature>", "Night color temperature. (454)", CommandOptionType.SingleValue);

            var pollingFrequencyOption = app.Option<TimeSpan>("-p|--polling-frequency <Time>", "How frequently should the lights be checked for the right temperature.", CommandOptionType.SingleValue);
            var transitionTimeOption = app.Option<TimeSpan>("-t|--transition-time <Time>", "How quickly should the lights fade between colors.", CommandOptionType.SingleValue);

            app.OnExecute(async () =>
            {
                Configuration configuration = new Configuration();

                if (discoverBridgesOption.HasValue())
                {
                    var locatedBridges = await DiscoverBridgesAsync(configuration);

                    if (locatedBridges.Count == 0)
                    {
                        Console.WriteLine("No bridges found");
                    }

                    for (int i = 0; i < locatedBridges.Count; i++)
                    {
                        Console.WriteLine($"ID: {locatedBridges[i].BridgeId,-10} IP: {locatedBridges[i].IpAddress}");
                    }
                    return;
                }

                string configurationFileName = @"configuration.json";
                if (resetOption.HasValue())
                {
                    File.Delete(configurationFileName);
                    return;
                }

                if (File.Exists(configurationFileName))
                {
                    configuration = JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(configurationFileName));
                }

                if (sunsetMustBeAfterOption.HasValue())
                    configuration.SunsetMustBeAfter = sunsetMustBeAfterOption.ParsedValue;

                if (sunsetMustBeBeforeOption.HasValue())
                    configuration.SunsetMustBeBeBefore = sunsetMustBeBeforeOption.ParsedValue;

                if (sunriseMustBeAfterOption.HasValue())
                    configuration.SunriseMustBeAfter = sunriseMustBeAfterOption.ParsedValue;

                if (sunriseMustBeBeforeOption.HasValue())
                    configuration.SunriseMustBeBeBefore = sunriseMustBeBeforeOption.ParsedValue;

                if (dayColorTemperatureOption.HasValue())
                    configuration.DayColorTemperature = dayColorTemperatureOption.ParsedValue;

                if (nightColorTemperatureOption.HasValue())
                    configuration.NightColorTemperature = nightColorTemperatureOption.ParsedValue;

                if (pollingFrequencyOption.HasValue())
                    configuration.PollingFrequency = pollingFrequencyOption.ParsedValue;

                if (transitionTimeOption.HasValue())
                    configuration.TransitionTime = transitionTimeOption.ParsedValue;

                if (latitudeOption.HasValue() ^ longitudeOption.HasValue())
                    throw new Exception("When supplying latitude or longitude, you must supply both.  Only one of them was given.");

                if (latitudeOption.HasValue())
                {
                    configuration.PositionState = new PositionState
                    {
                        Latitude = latitudeOption.ParsedValue,
                        Longitude = longitudeOption.ParsedValue,
                    };
                }
                else
                {
                    if (configuration.PositionState == null)
                    {
                        configuration.PositionState = new PositionState();
                        (configuration.PositionState.Latitude, configuration.PositionState.Longitude) = await AsyncUtils.Retry(() => Geolocation.GetLocationFromIPAddress(configuration), 120);
                    }
                }

                if (configuration.BridgeState == null)
                {
                    configuration.BridgeState = new BridgeState();
                }

                if (bridgeHostnameOption.HasValue())
                    configuration.BridgeState.BridgeHostname = bridgeHostnameOption.ParsedValue;

                if (string.IsNullOrEmpty(configuration.BridgeState.BridgeHostname))
                {
                    var locatedBridges = await DiscoverBridgesAsync(configuration);

                    if (locatedBridges.Count == 0)
                    {
                        throw new Exception("No bridges discovered on your network.");
                    }

                    configuration.BridgeState.BridgeHostname = locatedBridges[0].IpAddress;
                }

                LocalHueClient hueClient = new LocalHueClient(configuration.BridgeState.BridgeHostname);
                if (string.IsNullOrEmpty(configuration.BridgeState.BridgeApiKey))
                {
                    configuration.BridgeState.BridgeApiKey = await hueClient.RegisterAsync("HueShift", "Bridge0");

                    if(string.IsNullOrEmpty(configuration.BridgeState.BridgeApiKey))
                        throw new Exception("did not register");
                }
                else
                {
                    hueClient.Initialize(configuration.BridgeState.BridgeApiKey);
                }

                if (!noConfigurationSaveOption.HasValue())
                {
                    File.WriteAllText(configurationFileName, JsonConvert.SerializeObject(configuration, Formatting.Indented));
                }

                await LightScheduler.ContinuallyEnforceLightTemperature(configuration, hueClient);
            });

            return app.Execute(args);
        }

        public static bool TryParse<T>(CommandOption<T> option, out T value)
        {
            if (option.HasValue())
            {
                value = option.ParsedValue;
                return true;
            }
            value = default(T);
            return false;
        }

        private static async Task<List<LocatedBridge>> DiscoverBridgesAsync(Configuration configuration)
        {
            List<LocatedBridge> locatedBridges = new List<LocatedBridge>();
            try
            {
                SSDPBridgeLocator locator = new SSDPBridgeLocator();
                var bridges = (await locator.LocateBridgesAsync(TimeSpan.FromSeconds(10))).ToList();

                locatedBridges.AddRange(bridges);
            }
            catch
            {

            }

            if (locatedBridges.Count == 0)
            {
                HttpBridgeLocator locator = new HttpBridgeLocator();
                var bridges = (await locator.LocateBridgesAsync(TimeSpan.FromSeconds(10))).ToList();

                locatedBridges.AddRange(bridges);
            }

            return locatedBridges;
        }
    }
}
