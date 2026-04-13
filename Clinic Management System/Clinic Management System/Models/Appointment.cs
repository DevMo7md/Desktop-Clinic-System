using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic_Management_System.Models
{
    internal class Appointment
    {
        private static int _idCounter = 0;
        public int ID { get; private set; }
        public Patient AppointmentPatient { get; set; }
        public Doctor AppointmentDoctor { get; set; }
        public DateTime AppointmentDate { get; set; }
        //public AppointmentStatus Status { get; set; }

        public Appointment(Patient patient, Doctor doctor)
        {
            ID = ++_idCounter;
            AppointmentPatient = patient;
            AppointmentDoctor = doctor;
            AppointmentDate = DateTime.Now;
            //Status = AppointmentStatus.Pending;
        }

        public Appointment(Patient patient, Doctor doctor, DateTime date)
        {
            ID = ++_idCounter;
            AppointmentPatient = patient;
            AppointmentDoctor = doctor;
            AppointmentDate = date;
            //Status = AppointmentStatus.Pending;
        }

        public string GetAppointmentDetails()
        {
            return $"Patient: {AppointmentPatient.Name} | Doctor: {AppointmentDoctor.Name} | Date: {AppointmentDate}";
        }

        // to update the counter if program closed
        public static void UpdateCounter(int lastId)
        {
            _idCounter = lastId;
        }
    }
}
