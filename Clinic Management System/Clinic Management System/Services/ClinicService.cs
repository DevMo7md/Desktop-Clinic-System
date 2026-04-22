using Clinic_Management_System.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Clinic_Management_System.Services
{
    public class ClinicService
    {
        private readonly FileManager _fileManager;
        private List<Doctor> _doctors;
        private List<Patient> _patients;
        private List<Appointment> _appointments;
        private List<Payment> _payments;

        public ClinicService()
        {
            _fileManager = new FileManager();
            _fileManager.LoadData(out _doctors, out _patients, out _appointments, out _payments);

        }

        public List<Patient> GetPatients() => new List<Patient>(_patients);
        public List<Doctor> GetDoctors() => new List<Doctor>(_doctors);
        public List<Appointment> GetAppointments() => new List<Appointment>(_appointments);
        public List<Payment> GetPayments() => new List<Payment>(_payments);


        #region Patient Methods
        public void AddPatient(string name, int age, Gender gender)
        {
            ValidatePersonData(name, age);

            _patients.Add(new Patient(name, age, gender));
            Sync();
        }


        public void EditPatient(int id, string name, int age, Gender gender)
        {
            var p = _patients.FirstOrDefault(x => x.ID == id);
            if (p == null) throw new ArgumentException("المريض غير موجود في النظام.");
            ValidatePersonData(name, age);
            p.Name = name;
            p.Age = age;
            p.PersonGender = gender;
            Sync();
        }

        public void DeletePatient(int id)
        {
            var p = _patients.FirstOrDefault(x => x.ID == id);
            if (p == null) throw new ArgumentException("المريض غير موجود في النظام.");

            if (_appointments.Any(a => a.AppointmentPatient.ID == id && a.Status == AppointmentStatus.Pending))
            {
                throw new InvalidOperationException("لا يمكن حذف المريض لأن لديه مواعيد قادمة. يرجى إلغاء المواعيد أولاً.");
            }

            // CASCADE delete
            var relatedApps = _appointments.Where(a => a.AppointmentPatient.ID == id).Select(a => a.ID).ToList();
            _payments.RemoveAll(pay => relatedApps.Contains(pay.PaymentAppointment.ID));
            _appointments.RemoveAll(a => relatedApps.Contains(a.ID));
            _patients.Remove(p);

            Sync();
        }
        #endregion

        #region Medical Records & Chronic Diseases
        public void AddMedicalRecord(int patientId, string diagnosis, string treatment)
        {
            var p = _patients.FirstOrDefault(x => x.ID == patientId);
            if (p == null) throw new ArgumentException("المريض غير موجود.");

            ValidateString(diagnosis, "التشخيص");
            ValidateString(treatment, "العلاج");

            p.medical_record.Add(new MedicalRecord(diagnosis, treatment));
            Sync();
        }

        public void EditMedicalRecord(int patientId, int recordId, string diagnosis, string treatment)
        {
            var p = _patients.FirstOrDefault(x => x.ID == patientId);
            if (p == null) throw new ArgumentException("المريض غير موجود.");

            var record = p.medical_record.FirstOrDefault(r => r.ID == recordId);
            if (record == null) throw new ArgumentException("السجل الطبي المطلوب غير موجود.");

            ValidateString(diagnosis, "التشخيص");
            ValidateString(treatment, "العلاج");

            record.Diagnosis = diagnosis;
            record.Treatment = treatment;
            Sync();
        }

        public void DeleteMedicalRecord(int patientId, int recordId)
        {
            var p = _patients.FirstOrDefault(x => x.ID == patientId);
            if (p == null) throw new ArgumentException("المريض غير موجود.");

            var record = p.medical_record.FirstOrDefault(r => r.ID == recordId);
            if (record == null) throw new ArgumentException("السجل الطبي المطلوب غير موجود.");

            p.medical_record.Remove(record);
            Sync();
        }

        public void AddChronicDisease(int patientId, string name, string notes)
        {
            var p = _patients.FirstOrDefault(x => x.ID == patientId);
            if (p == null) throw new ArgumentException("المريض غير موجود.");
            
            ValidateString(name, "اسم المرض المزمن");
            ValidateString(notes, "ملاحظات المرض المزمن");

            p.chronic_diseases.Add(new ChronicDisease(name, notes));
            Sync();
        }

        public void EditChronicDisease(int patientId, int diseaseId, string name, string notes)
        {
            var p = _patients.FirstOrDefault(x => x.ID == patientId);
            if (p == null) throw new ArgumentException("المريض غير موجود.");

            var disease = p.chronic_diseases.FirstOrDefault(c => c.ID == diseaseId);
            if (disease == null) throw new ArgumentException("المرض المزمن المطلوب غير موجود.");
            
            ValidateString(name, "اسم المرض المزمن");
            ValidateString(notes, "ملاحظات المرض المزمن");
            
            disease.Name = name;
            disease.Notes = notes;
            Sync();
        }

        public void DeleteChronicDisease(int patientId, int diseaseId)
        {
            var p = _patients.FirstOrDefault(x => x.ID == patientId);
            if (p == null) throw new ArgumentException("المريض غير موجود.");

            var disease = p.chronic_diseases.FirstOrDefault(c => c.ID == diseaseId);
            if (disease == null) throw new ArgumentException("المرض المزمن المطلوب غير موجود.");

            p.chronic_diseases.Remove(disease);
            Sync();
        }
        #endregion

        #region Doctor Methods
        public void AddDoctor(string name, int age, Gender gender, string specialty)
        {
            ValidatePersonData(name, age);

            _doctors.Add(new Doctor(name, age, gender, specialty));
            Sync();
        }

        public void EditDoctor(int id, string name, int age, Gender gender, string specialty)
        {
            var d = _doctors.FirstOrDefault(x => x.ID == id);
            if (d == null) throw new ArgumentException("الطبيب غير موجود.");
            ValidatePersonData(name, age);
            d.Name = name;
            d.Age = age;
            d.PersonGender = gender;
            d.Specialty = specialty;
            Sync();
        }

        public void DeleteDoctor(int id)
        {
            var d = _doctors.FirstOrDefault(x => x.ID == id);
            if (d == null) throw new ArgumentException("الطبيب غير موجود في النظام.");

            if (_appointments.Any(a => a.AppointmentDoctor.ID == id && a.Status == AppointmentStatus.Pending)) throw new InvalidOperationException("لا يمكن حذف الطبيب لأنه لديه مواعيد قادمة.");

            _doctors.Remove(d);
            Sync();
        }
        #endregion

        #region Appointment Methods
        public void AddAppointment(int patientId, int doctorId, decimal fee, DateTime date)
        {
            var p = _patients.FirstOrDefault(x => x.ID == patientId);
            var d = _doctors.FirstOrDefault(x => x.ID == doctorId);

            if (p == null) throw new ArgumentException("المريض غير موجود في النظام.");
            if (d == null) throw new ArgumentException("الطبيب غير موجود في النظام.");

            if (date < DateTime.Now)
                throw new ArgumentException("لا يمكن حجز ميعاد في وقت سابق.");

            if (IsDoctorBusy(doctorId, date)) throw new InvalidOperationException("الطبيب مشغول في هذا الوقت.");

            _appointments.Add(new Appointment(p, d, fee, date));
            Sync();
        }

        public void EditAppointment(int appId, int newDoctorId, DateTime newDate, decimal newFee)
        {
            var app = _appointments.FirstOrDefault(x => x.ID == appId);
            if (app == null) throw new ArgumentException("الحجز غير موجود.");

            if (app.AppointmentDoctor.ID != newDoctorId || app.AppointmentDate != newDate)
            {
                if (IsDoctorBusy(newDoctorId, newDate, appId))
                    throw new InvalidOperationException("الطبيب الجديد مشغول في هذا الوقت.");

                var newDoc = _doctors.FirstOrDefault(d => d.ID == newDoctorId);
                if (newDoc == null) throw new ArgumentException("الطبيب الجديد غير موجود.");


                app.AppointmentDoctor = newDoc;
                app.AppointmentDate = newDate;
            }

            app.Fee = newFee;
            Sync();
        }

        public void CompleteAppointment(int id)
        {
            var app = _appointments.FirstOrDefault(x => x.ID == id);
            if (app == null) throw new ArgumentException("الحجز غير موجود.");

            if (app.Status == AppointmentStatus.Cancelled)
                throw new InvalidOperationException("لا يمكن إتمام حجز ملغي.");

            app.Status = AppointmentStatus.Completed;
            Sync();
        }

        public void DeleteAppointment(int id)
        {
            var app = _appointments.FirstOrDefault(x => x.ID == id);
         
            if (app == null) throw new ArgumentException("الحجز غير موجود.");

            if (app.Status == AppointmentStatus.Completed)
                throw new InvalidOperationException("لا يمكن إلغاء حجز تم اكتماله بالفعل.");
            
            app.Status = AppointmentStatus.Cancelled;

            var relatedPayment = _payments.FirstOrDefault(p => p.PaymentAppointment.ID == id);
            if (relatedPayment != null)
            {
                relatedPayment.Status = PaymentStatus.Refunded;
            }
            Sync();
        }
        #endregion


        #region Payment Methods
        public void AddPayment(int appointmentId, decimal amount)
        {
            var app = _appointments.FirstOrDefault(a => a.ID == appointmentId);
            if (app == null) throw new ArgumentException("الموعد غير موجود في النظام.");

            _payments.Add(new Payment(app, amount, PaymentStatus.Paid));
            Sync();
     
        }

        public void EditPayment(int paymentId, decimal newAmount)
        {
            var pay = _payments.FirstOrDefault(p => p.ID == paymentId);
            if (pay == null) throw new ArgumentException("عملية الدفع غير موجودة.");

            pay.Amount = newAmount;
            pay.PaymentDate = DateTime.Now; 
            Sync();
        }

        public void DeletePayment(int id)
        {
            var pay = _payments.FirstOrDefault(p => p.ID == id);
            if (pay == null) throw new ArgumentException("عملية الدفع غير موجودة.");

            _payments.Remove(pay);
            Sync();
        }
        #endregion


        #region Search & Filtering
        // Search by ID or name (case-insensitive)
        public List<Patient> SearchPatients(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return _patients;

            return _patients.Where(p =>
                p.ID.ToString() == query ||
                p.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }

        public List<Doctor> SearchDoctors(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return _doctors;

            return _doctors.Where(d =>
                d.ID.ToString() == query ||
                d.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                d.Specialty.Contains(query, StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }

        public List<Appointment> GetDoctorAppointments(int doctorId)
        {
            return _appointments.Where(a => a.AppointmentDoctor.ID == doctorId).ToList();
        }

        public List<Appointment> GetTodayAppointments()
        {
            var today = DateTime.Today;
            return _appointments
                .Where(a => a.AppointmentDate.Date == today && a.Status != AppointmentStatus.Cancelled)
                .OrderBy(a => a.AppointmentDate) // Oldest appointments first
                .ToList();
        }

        public List<Appointment> GetPatientHistory(int patientId)
        {
            return _appointments.Where(a => a.AppointmentPatient.ID == patientId).ToList();
        }
        #endregion

        #region Validation Helpers
        private bool IsDoctorBusy(int doctorId, DateTime date, int? excludeAppId = null)
        {
            return _appointments.Any(a => a.AppointmentDoctor.ID == doctorId && (a.ID != excludeAppId || excludeAppId == null) && a.Status != AppointmentStatus.Cancelled && Math.Abs((a.AppointmentDate - date).TotalMinutes) < 30);
        }
        private void ValidatePersonData(string name, int age)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("الاسم لا يمكن أن يكون فارغاً.");
            if (age <= 0 || age > 120) throw new ArgumentException("يرجى إدخال عمر منطقي بين 1 و 120.");
        }
        private void ValidateString(string value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"{fieldName} لا يمكن أن يكون فارغاً.");
        }
        #endregion
        private void Sync()
        {
            _fileManager.SaveData(_doctors, _patients, _appointments, _payments);
        }
    }
}