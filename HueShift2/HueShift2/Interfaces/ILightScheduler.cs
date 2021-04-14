﻿using HueShift2.Configuration;
using System;
using System.Threading.Tasks;

namespace HueShift2.Interfaces
{
    public interface ILightScheduler
    {
        public HueShiftMode Mode();
        public Task<DateTime?> Execute(DateTime? lastRunTime);
    }
}
