﻿using CarManagement.Models;
using CarManagement.Services;

namespace BusinessCore.Tests
{
    public class SingleEnrollmentProvider : IEnrollmentProvider
    {
        private class Enrollment : IEnrollment
        {

            public Enrollment(string serial, int number)
            {
                this.Serial = serial;
                this.Number = number;
            }

            public string Serial { get; }
            public int Number { get; }

            public string Print()
            {
                return $"{this.Serial}-{this.Number.ToString("0000")}";
            }
        }

        public SingleEnrollmentProvider()
        {
            this.DefaultEnrollment = new Enrollment(serial: "XXX", number: 666);
        }

        public IEnrollment DefaultEnrollment { get; }

        IEnrollment IEnrollmentProvider.getNew()
        {
            return this.DefaultEnrollment;
        }

        IEnrollment IEnrollmentProvider.import(string serial, int number)
        {
            return new Enrollment(serial, number);
        }
    }
}