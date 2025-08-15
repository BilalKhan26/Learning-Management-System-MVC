# 📚 Learning Management System (LMS)

## Overview
This **Learning Management System** is a web application built with **ASP.NET Core MVC** that allows **admins, instructors, and students** to manage courses, lessons, and assignments.  
It follows a **Controller → Service → Repository** pattern for clean separation of concerns and is fully tested with **xUnit** and **Moq**.

---

## ✨ Features

### Admin Dashboard
- **Manage Instructors**: Create, update, delete instructors.
- **Manage Students**: List students and perform simple CRUD operations inline.
- **View Courses**: See all courses with details of instructors, lessons, and enrolled students.
- **Assignments Overview**: Track assignments per course and student.
- Role-based security with `[Authorize(Roles = "Admin")]`.

### Instructor
- Create and manage courses.
- Upload and manage lessons.
- Assign homework/assignments to students.

### Student
- View enrolled courses.
- Access lessons and assignments.
- Submit assignments.

---

## 🛠️ Tech Stack

- **Backend**: ASP.NET Core MVC (.NET 6/7)
- **Frontend**: Razor Views with Bootstrap 5
- **Authentication**: ASP.NET Identity + JWT
- **Email**: Custom EmailSender service
- **Database**: Entity Framework Core (SQL Server or InMemory for tests)
- **Testing**: xUnit, Moq, InMemoryDatabase

---

## 📂 Project Structure

LMS/
│
├── Controllers/
│ ├── AdminDashboardController.cs
│ ├── InstructorController.cs
│ ├── StudentDashboardController.cs
│
├── Models/
│ ├── ApplicationUser.cs
│ ├── Course.cs
│ ├── Lesson.cs
│ ├── Assignment.cs
│
├── Services/
│ ├── Interfaces/
│ ├── Implementations/
│
├── Data/
│ ├── ApplicationDbContext.cs
│
├── Views/
│ ├── AdminDashboard/
│ │ ├── Instructors.cshtml
│ │ ├── Students.cshtml
│ │ ├── Courses.cshtml
│ │ ├── CourseDetails.cshtml
│
├── Tests/
│ ├── Controllers/
│ │ ├── AdminDashboardControllerTests.cs
│ │ ├── StudentDashboardControllerTests.cs
│
└── README.md

csharp
Copy
Edit

---

## 🧪 Testing

Unit tests are written with **xUnit** and **Moq**.

### Covered Areas
- Admin dashboard controller (positive + negative scenarios)
- Student dashboard (course filtering)
- JWT authentication service
- Email sender service
- Repository and service layer for `Course` and `Lesson`
