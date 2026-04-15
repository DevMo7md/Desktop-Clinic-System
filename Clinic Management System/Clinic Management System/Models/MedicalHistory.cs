using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic_Management_System.Models
{
    public class MedicalRecord
    {
        private static int _idCounter = 0;
        public int ID { get; private set; }
        public string Diagnosis { get; set; }
        public string Treatment { get; set; }
        public DateTime Date { get; set; }

        public MedicalRecord(string diagnosis, string treatment)
        {
            ID = ++_idCounter; 
            Diagnosis = diagnosis;
            Treatment = treatment;
            Date = DateTime.Now; 
        } 
        public MedicalRecord(string diagnosis, string treatment, DateTime date)
        {
            ID = ++_idCounter;
            Diagnosis = diagnosis;
            Treatment = treatment;
            Date = date;
        }

        public string GetRecordDetails()
        {
            return $"ID: {ID} | Diagnosis: {Diagnosis} | Treatment: {Treatment} | Date: {Date}";
        }
        public void LoadID(int id)
        {
            ID = id;
        }
        // to update the counter if program closed
        public static void UpdateCounter(int lastId)
        {
            _idCounter = lastId;
        }
    }

    public class ChronicDisease
    {
        private static int _idCounter = 0;
        public int ID { get; private set; }
        public string Name { get; set; }
        public string Notes { get; set; }
        public ChronicDisease(string name, string notes)
        {
            ID = ++_idCounter; 
            Name = name;
            Notes = notes;
        }

        public string GetDiseaseDetails()
        {
            return $"ID: {ID} | Name: {Name} | Notes: {Notes}";
        }
        public void LoadID(int id)
        {
            ID = id;
        }
        // to update the counter if program closed
        public static void UpdateCounter(int lastId)
        {
            _idCounter = lastId;
        }
    }
}
