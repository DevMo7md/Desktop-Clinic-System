using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic_Management_System.Models
{
    public class Patient : Person
    {
        public List<MedicalRecord> Medical_record { get; set; }
        public List<ChronicDisease> Chronic_diseases { get; set; }
        public Patient(string name, int age, Gender gender, List<MedicalRecord> medical_record ,List<ChronicDisease> chronic_diseases) : base(name, age, gender)
        {
           
            Medical_record = medical_record;
            Chronic_diseases = chronic_diseases;
            
        }
        public override string DisplayInfo()
        {
            return $"{base.DisplayInfo()}, Medical Record: {Medical_record.Count}, Chronic Diseases: {Chronic_diseases.Count}";
        }
    }
}
