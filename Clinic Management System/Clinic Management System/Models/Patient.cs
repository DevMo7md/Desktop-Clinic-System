using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic_Management_System.Models
{
    internal class Patient : Person
    {
        // public List<MedicalRecord> medical_record { get; set; }
        // public List<ChronicDisease> chronic_diseases { get; set; }
        public Patient(string name, int age/*, Gander gander, List<MedicalRecord> medical_record ,List<ChronicDisease> chronic_diseases*/) : base(name, age/*, gander*/)
        {
            /*
            this.medical_record = medical_record;
            this.chronic_diseases = chronic_diseases;
            */
        }
        public override string DisplayInfo()
        {
            return $"{base.DisplayInfo()}, Medical Record: [medical_record], Chronic Diseases: [chronic_diseases]";
        }
    }
}
