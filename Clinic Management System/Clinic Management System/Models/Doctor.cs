using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic_Management_System.Models
{
    public class Doctor : Person
    {
        public string Specialty { get; set; }

        public Doctor(string name, int age,Gender gender, string specialty) : base(name, age, gender)
        {
            Specialty = specialty;
        }

        public override string DisplayInfo()
        {
            return $"{base.DisplayInfo()}, Specialty: {Specialty}";
        }
    }
}
