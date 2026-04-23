# 🏥 Desktop Clinic System — Backend Documentation

> This document explains the backend logic of the Clinic Management System for team members working on the UI layer. You don't need to read the source code — everything you need to know to connect your UI to the backend is here.

---

## 📁 Project Structure

```
Clinic_Management_System/
├── Models/                  # Data classes (the "things" the system works with)
│   ├── Person.cs
│   ├── Doctor.cs
│   ├── Patient.cs
│   ├── Appointment.cs
│   ├── Payment.cs
│   ├── MedicalHistory.cs    # Contains MedicalRecord & ChronicDisease
│   └── Enums.cs             # Status & gender definitions
│
└── Services/                # Business logic (the "actions" the system can do)
    ├── ClinicService.cs      # ⭐ Main service — your primary entry point
    ├── AnalyticsService.cs   # Dashboard stats & reports
    └── FileManager.cs        # Handles saving/loading data from files
```

---

## 🧱 Models Overview

These are the core data classes. Think of them as the "tables" of the system.

### `Person` (Base Class)
The base for both `Doctor` and `Patient`. Contains shared fields.

| Property | Type | Description |
|---|---|---|
| `ID` | `int` | Auto-generated unique ID |
| `Name` | `string` | Full name |
| `Age` | `int` | Must be between 1–120 |
| `PersonGender` | `Gender` | `Male` or `Female` |

---

### `Doctor : Person`
Inherits all fields from `Person`, plus:

| Property | Type | Description |
|---|---|---|
| `Specialty` | `string` | Doctor's medical specialty |

---

### `Patient : Person`
Inherits all fields from `Person`, plus:

| Property | Type | Description |
|---|---|---|
| `medical_record` | `List<MedicalRecord>` | List of past diagnoses/treatments |
| `chronic_diseases` | `List<ChronicDisease>` | List of ongoing chronic conditions |

---

### `Appointment`

| Property | Type | Description |
|---|---|---|
| `ID` | `int` | Auto-generated unique ID |
| `AppointmentPatient` | `Patient` | The patient for this appointment |
| `AppointmentDoctor` | `Doctor` | The assigned doctor |
| `AppointmentDate` | `DateTime` | Scheduled date and time |
| `Status` | `AppointmentStatus` | `Pending`, `Completed`, or `Cancelled` |
| `Fee` | `decimal` | Appointment fee amount |

---

### `Payment`

| Property | Type | Description |
|---|---|---|
| `ID` | `int` | Auto-generated unique ID |
| `PaymentAppointment` | `Appointment` | The linked appointment |
| `Amount` | `decimal` | Amount paid |
| `PaymentDate` | `DateTime` | Date of payment |
| `Status` | `PaymentStatus` | `Paid` or `Refunded` |

---

### `MedicalRecord`

| Property | Type | Description |
|---|---|---|
| `ID` | `int` | Auto-generated unique ID |
| `Diagnosis` | `string` | Doctor's diagnosis text |
| `Treatment` | `string` | Prescribed treatment |
| `Date` | `DateTime` | Date the record was created |

---

### `ChronicDisease`

| Property | Type | Description |
|---|---|---|
| `ID` | `int` | Auto-generated unique ID |
| `Name` | `string` | Name of the chronic condition |
| `Notes` | `string` | Additional notes |

---

### Enums (`Enums.cs`)

```csharp
enum Gender            { Male, Female }
enum AppointmentStatus { Pending, Completed, Cancelled }
enum PaymentStatus     { Paid, Refunded }
```

---

## ⚙️ ClinicService — Main Entry Point

`ClinicService` is the **only class your UI needs to interact with** for all data operations. Create one instance and use it throughout the app.

```csharp
var service = new ClinicService();
```

It automatically loads all saved data on startup.

---

### 👤 Patient Methods

#### Add a Patient
```csharp
service.AddPatient(string name, int age, Gender gender);
```

#### Edit a Patient
```csharp
service.EditPatient(int id, string name, int age, Gender gender);
```

#### Delete a Patient
```csharp
service.DeletePatient(int id);
```
> ⚠️ **Cannot delete** a patient who has **pending appointments**. Cancel those first.  
> Deleting a patient also removes all their appointments and payments (cascade delete).

#### Get All Patients
```csharp
List<Patient> patients = service.GetPatients();
```

#### Search Patients
```csharp
List<Patient> results = service.SearchPatients(string query);
```
> Searches by **ID** or **name** (case-insensitive). Pass an empty string to get all.

---

### 🩺 Doctor Methods

#### Add a Doctor
```csharp
service.AddDoctor(string name, int age, Gender gender, string specialty);
```

#### Edit a Doctor
```csharp
service.EditDoctor(int id, string name, int age, Gender gender, string specialty);
```

#### Delete a Doctor
```csharp
service.DeleteDoctor(int id);
```
> ⚠️ **Cannot delete** a doctor with **pending appointments**.

#### Get All Doctors
```csharp
List<Doctor> doctors = service.GetDoctors();
```

#### Search Doctors
```csharp
List<Doctor> results = service.SearchDoctors(string query);
```
> Searches by **ID**, **name**, or **specialty** (case-insensitive).

---

### 📋 Medical Records & Chronic Diseases

All medical data is stored **inside the Patient object**. Methods require the patient's ID.

#### Medical Records
```csharp
service.AddMedicalRecord(int patientId, string diagnosis, string treatment);
service.EditMedicalRecord(int patientId, int recordId, string diagnosis, string treatment);
service.DeleteMedicalRecord(int patientId, int recordId);
```

#### Chronic Diseases
```csharp
service.AddChronicDisease(int patientId, string name, string notes);
service.EditChronicDisease(int patientId, int diseaseId, string name, string notes);
service.DeleteChronicDisease(int patientId, int diseaseId);
```

> To **display** a patient's records, get the patient object and read its lists directly:
> ```csharp
> var patient = service.GetPatients().First(p => p.ID == someId);
> var records = patient.medical_record;
> var diseases = patient.chronic_diseases;
> ```

---

### 📅 Appointment Methods

#### Add an Appointment
```csharp
service.AddAppointment(int patientId, int doctorId, decimal fee, DateTime date);
```
> ⚠️ Will throw an error if:
> - The date is in the past.
> - The doctor already has an appointment within **30 minutes** of the requested time.

#### Edit an Appointment
```csharp
service.EditAppointment(int appId, int newDoctorId, DateTime newDate, decimal newFee);
```

#### Complete an Appointment
```csharp
service.CompleteAppointment(int id);
```
> Marks the appointment as `Completed`. Cannot be done if already `Cancelled`.

#### Cancel an Appointment
```csharp
service.DeleteAppointment(int id);
```
> Sets status to `Cancelled` (does **not** physically delete it).  
> If a payment exists for this appointment, it is automatically marked as `Refunded`.

#### Get All Appointments
```csharp
List<Appointment> all = service.GetAppointments();
```

#### Get Today's Appointments
```csharp
List<Appointment> todays = service.GetTodayAppointments();
```
> Returns non-cancelled appointments for today, ordered by time (earliest first).

#### Get a Doctor's Appointments
```csharp
List<Appointment> doctorApps = service.GetDoctorAppointments(int doctorId);
```

#### Get a Patient's Appointment History
```csharp
List<Appointment> history = service.GetPatientHistory(int patientId);
```

---

### 💳 Payment Methods

#### Add a Payment
```csharp
service.AddPayment(int appointmentId, decimal amount);
```
> Payment is automatically marked as `Paid` with today's date.

#### Edit a Payment
```csharp
service.EditPayment(int paymentId, decimal newAmount);
```
> Also updates the payment date to now.

#### Delete a Payment
```csharp
service.DeletePayment(int id);
```

#### Get All Payments
```csharp
List<Payment> payments = service.GetPayments();
```

---

## 📊 AnalyticsService — Dashboard & Reports

Used for the home dashboard and financial summaries. Must be created by passing the current data lists.

```csharp
var analytics = new AnalyticsService(
    service.GetAppointments(),
    service.GetPayments(),
    service.GetPatients(),
    service.GetDoctors()
);
```

> ⚠️ Re-create or refresh this object whenever data changes to get up-to-date stats.

### Available Methods

| Method | Returns | Description |
|---|---|---|
| `GetTodayTotalPatientsCount()` | `int` | Unique patients with non-cancelled appointments today |
| `GetTodayPendingAppointmentsCount()` | `int` | Appointments still pending today |
| `GetTodayTotalRevenue()` | `decimal` | Sum of `Paid` payments made today |
| `GetTodayPresentDoctorsCount()` | `int` | Unique doctors with active appointments today |
| `GetNetRevenue(DateTime start, DateTime end)` | `decimal` | Revenue in a date range (subtracts refunds) |
| `GetMonthlyGrowthRate()` | `double` | % change in revenue vs last month |
| `GetPeakHour()` | `int` | Hour of the day (0–23) with most appointments |
| `GetPatientAgeDemographics()` | `Dictionary<string, int>` | Patient count by age group |

#### Age Demographics Keys (in Arabic)
```
"أطفال (0-15)"   → Children
"شباب (16-40)"   → Youth
"كبار سن (41+)"  → Seniors
```

---

## 💾 Data Storage

The system stores all data in **plain text `.txt` files** in the application's working directory. No database is needed.

| File | Contents |
|---|---|
| `doctors.txt` | All doctor records |
| `patients.txt` | All patient records |
| `medical_records.txt` | Medical records linked to patients |
| `chronic_diseases.txt` | Chronic disease records linked to patients |
| `appointments.txt` | All appointment records |
| `payments.txt` | All payment records |

- Data is **automatically saved** after every operation (add, edit, delete).
- Data is **automatically loaded** when `ClinicService` is created.
- ⚠️ If any `.txt` file is open in another program (e.g., Notepad) during a save, an error will be thrown.

---

## 🚨 Error Handling

All service methods throw **exceptions** when something goes wrong. Your UI should always wrap calls in `try/catch` to show friendly messages to the user.

```csharp
try
{
    service.AddAppointment(patientId, doctorId, fee, date);
    // show success message
}
catch (ArgumentException ex)
{
    // Invalid input (e.g., patient not found, past date)
    MessageBox.Show(ex.Message);
}
catch (InvalidOperationException ex)
{
    // Business rule violation (e.g., doctor is busy)
    MessageBox.Show(ex.Message);
}
catch (Exception ex)
{
    // Unexpected errors (e.g., file save failed)
    MessageBox.Show(ex.Message);
}
```

> Error messages are written in **Arabic** — they are safe to display directly to the user.

---

## 🔄 ID System

Every model (`Doctor`, `Patient`, `Appointment`, `Payment`, `MedicalRecord`, `ChronicDisease`) uses an **auto-incrementing integer ID**. IDs are assigned automatically when a new object is created — you never set them manually. When the app restarts, counters are restored from saved files to avoid duplicates.

---
