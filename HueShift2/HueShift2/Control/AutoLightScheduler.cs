﻿using HueShift2.Configuration;
using HueShift2.Configuration.Model;
using HueShift2.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Q42.HueApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HueShift2.Control
{
    public class AutoLightScheduler: ILightScheduler
    {
        private readonly HueShiftMode mode;
        private readonly ILogger<AutoLightScheduler> logger;
        private readonly IOptionsMonitor<HueShiftOptions> appOptionsDelegate;

        private IScheduleProvider scheduleProvider;
        private ILightManager lightManager;
   
        public AutoLightScheduler(ILogger<AutoLightScheduler> logger, IOptionsMonitor<HueShiftOptions> appOptionsDelegate, IOptionsMonitor<CustomScheduleOptions> scheduleOptionsDelegate,
            ILightManager lightManager, IEnumerable<IScheduleProvider> scheduleProviders)
        {
            this.mode = HueShiftMode.Auto;
            this.logger = logger;
            this.appOptionsDelegate = appOptionsDelegate;
            this.scheduleProvider = scheduleProviders.First(x => x.Mode() == this.mode);
            this.lightManager = lightManager;
        }

        public HueShiftMode Mode()
        {
            return mode;
        }

        private async Task ExecuteTransition(DateTime currentTime, DateTime? lastRunTime)
        {
            var transitionDuration = scheduleProvider.GetTransitionDuration(currentTime, lastRunTime);
            var resumeControl = scheduleProvider.IsReset(currentTime, lastRunTime);
            var targetLightState = scheduleProvider.TargetLightState(currentTime);
            logger.LogInformation("Performing transition...");
            var command = new LightCommand
            {
                ColorTemperature = targetLightState.Colour.ColourTemperature,
                TransitionTime = transitionDuration
            };
            await lightManager.ExecuteTransition(targetLightState, command, currentTime, resumeControl);
        }

        public async Task<DateTime?> Execute(DateTime? lastRunTime)
        {
            logger.LogDebug("Executing automatic light control...");
            var currentTime = DateTime.Now;
            if (!scheduleProvider.ShouldPerformTransition(currentTime, lastRunTime))
            {
                logger.LogDebug("No transition to perform.");
                await lightManager.ExecuteRefresh(currentTime);
                return currentTime;
            }
            await ExecuteTransition(currentTime, lastRunTime);
            return currentTime;
        }
    }
}