using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic_Management_System.Models
{
    public class Doctor : Person
    {
        private static int _idCounter = 0;
        public string Specialty { get; set; }

        public Doctor(string name, int age,Gender gender, string specialty) : base(name, age, gender)
        {
            ID = ++_idCounter;
            Specialty = specialty;
        }

        public override string DisplayInfo()
        {
            return $"{base.DisplayInfo()}, Specialty: {Specialty}";
        }
        // to update the counter if program closed 
        public static void UpdateCounter(int lastId)
        {
            _idCounter = lastId;
        }
    }
}
