﻿using System;

namespace CarManagement.Models
{
    public class Engine
    {
        private int horsepower;
        private bool isStarted;

        public Engine(int h)
        {
            horsepower = h;
            isStarted = false;
        }

        public void start()
        {
            try
            {
                //Insert Start instructions here
                isStarted = true;
            }
            catch (Exception)
            {
                isStarted = false;
            }
        }

        public bool IsStarted
        {
            get
            {
                return isStarted;
            }
        }
    }
}