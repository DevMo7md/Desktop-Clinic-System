using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic_Management_System.Models
{
    public class Patient : Person
    {
        public List<MedicalRecord> medical_record { get; set; }
        public List<ChronicDisease> chronic_diseases { get; set; }
        public Patient(string name, int age, Gender gender) : base(name, age, gender)
        {

            medical_record = new List<MedicalRecord>();
            chronic_diseases = new List<ChronicDisease>();

        }
        public override string DisplayInfo()
        {
            return $"{base.DisplayInfo()}, Medical Record: {medical_record.Count}, Chronic Diseases: {chronic_diseases.Count}";
        }
    }
}
