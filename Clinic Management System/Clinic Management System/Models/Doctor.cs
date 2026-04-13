using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic_Management_System.Models
{
    internal class Doctor : Person
    {
        public string Specialty { get; set; }

        public Doctor(string name, int age,/*Gander gander,*/ string specialty) : base(name, age/*, gander*/)
        {
            Specialty = specialty;
        }

        public override string DisplayInfo()
        {
            return $"{base.DisplayInfo()}, Specialty: {Specialty}";
        }
    }
}
