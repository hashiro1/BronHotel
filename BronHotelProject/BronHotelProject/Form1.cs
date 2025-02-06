using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Entity;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static BronHotelProject.Program;
using Microsoft.EntityFrameworkCore;

//Linq

namespace BronHotelProject
{
    public partial class Form1 : Form
    {

        private string connectionString = "(localdb)\\MSSQLLocalDB";


        public Form1()
        {
            InitializeComponent();
        }

        public class User
        {
            public int UserId { get; set; }
            public string Username { get; set; }
            public string Password { get; set; } 
            public string Role { get; set; }
            public string Email { get; set; }
            public DateTime CreatedAt { get; set; } = DateTime.Now;

            public bool VerifyPassword(string password)
            {
                return Password == password;
            }
        }

        public class Service
        {
            public int ServiceId { get; set; }
            public string ServiceType { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string Location { get; set; }
            public decimal Price { get; set; }
            public decimal Rating { get; set; }
            public string ImageUrl { get; set; }
        }

        public class Booking
        {
            public int BookingId { get; set; }
            public int UserId { get; set; }
            public int ServiceId { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public int NumberOfPeople { get; set; }
            public DateTime CreatedAt { get; set; } = DateTime.Now;

            public virtual User User { get; set; }
            public virtual Service Service { get; set; }
        }

        public class Review
        {
            public int ReviewId { get; set; }
            public int ServiceId { get; set; }
            public int UserId { get; set; }
            public decimal Rating { get; set; }
            public string Comment { get; set; }
            public DateTime CreatedAt { get; set; } = DateTime.Now;

            public virtual Service Service { get; set; }
            public virtual User User { get; set; }
        }



        public class AppDbContext : DbContext
        {
            public DbSet<User> Users { get; set; }
            public DbSet<Service> Services { get; set; }
            public DbSet<Booking> Bookings { get; set; }
            public DbSet<Review> Reviews { get; set; }

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                optionsBuilder.UseSqlServer("");
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<User>().ToTable("Users");
                modelBuilder.Entity<Service>().ToTable("Services");
                modelBuilder.Entity<Booking>().ToTable("Bookings");
                modelBuilder.Entity<Review>().ToTable("Reviews");
            }
        }

        public List<Service> FilterServices(decimal? minPrice, decimal? maxPrice, decimal? minRating,
                                      DateTime? startDate, DateTime? endDate,
                                      string location, string serviceType)
        {
            using (var context = new AppDbContext())
            {
                var query = context.Services.AsQueryable();

                if (minPrice.HasValue)
                    query = query.Where(s => s.Price >= minPrice.Value);

                if (maxPrice.HasValue)
                    query = query.Where(s => s.Price <= maxPrice.Value);

                if (minRating.HasValue)
                    query = query.Where(s => s.Rating >= minRating.Value);

                if (!string.IsNullOrEmpty(location))
                    query = query.Where(s => s.Location.Contains(location));

                if (!string.IsNullOrEmpty(serviceType))
                    query = query.Where(s => s.ServiceType.Equals(serviceType, StringComparison.OrdinalIgnoreCase));

                if (startDate.HasValue && endDate.HasValue)
                {
                    var bookedServiceIds = context.Bookings
                        .Where(b => (b.StartDate < endDate.Value && b.EndDate > startDate.Value))
                        .Select(b => b.ServiceId)
                        .Distinct();

                    query = query.Where(s => !bookedServiceIds.Contains(s.ServiceId));
                }

                return query.ToList();
            }
        }


        public List<Service> FilterServices(decimal? minPrice, decimal? maxPrice, decimal? minRating)
        {
            using (var context = new AppDbContext())
            {
                var query = context.Services.AsQueryable();

                if (minPrice.HasValue)
                    query = query.Where(s => s.Price >= minPrice.Value);

                if (maxPrice.HasValue)
                    query = query.Where(s => s.Price <= maxPrice.Value);

                if (minRating.HasValue)
                    query = query.Where(s => s.Rating >= minRating.Value);

                return query.ToList();
            }
        }

     

        public class UserService
        {
            private List<User> users = new List<User>(); // Используем список для хранения пользователей

            public void RegisterUser(string username, string password, string email, string role)
            {
                var user = new User
                {
                    Username = username,
                    Email = email,
                    Role = role,
                    Password = password
                };

                users.Add(user); // Добавляем пользователя в список
            }

            public User Authenticate(string username, string password)
            {
                var user = users.SingleOrDefault(u => u.Username == username);
                if (user != null && user.VerifyPassword(password))
                {
                    return user;
                }
                return null;
            }
        }


        public User Authenticate(string username, string password)
        {
            using (var context = new AppDbContext())
            {
                var user = context.Users.SingleOrDefault(u => u.Username == username);
                if (user != null && user.VerifyPassword(password))
                {
                    return user; 
                }
                return null; 
            }
        }


        public void CreateBooking(int userId, int serviceId, DateTime startDate, DateTime endDate, int numberOfPeople)
        {
            using (var context = new AppDbContext())
            {
                var user = context.Users.Find(userId);

                if (user.Role != "Customer")
                {
                    throw new UnauthorizedAccessException("Только клиенты могут создавать заказы.");
                }

                var booking = new Booking
                {
                    UserId = userId,
                    ServiceId = serviceId,
                    StartDate = startDate,
                    EndDate = endDate,
                    NumberOfPeople = numberOfPeople
                };

                context.Bookings.Add(booking);
                context.SaveChanges();
            }
        }

        public void AddReview(int userId, int serviceId, decimal rating, string comment)
        {
            using (var context = new AppDbContext())
            {
                var review = new Review
                {
                    UserId = userId,
                    ServiceId = serviceId,
                    Rating = rating,
                    Comment = comment
                };

                context.Reviews.Add(review);
                context.SaveChanges();
            }
        }

        public class AdminManagement
        {
            private readonly AppDbContext _context;

            public AdminManagement(AppDbContext context)
            {
                _context = context;
            }

            public void AddService(Service service)
            {
                if (service == null)
                    throw new ArgumentNullException(nameof(service));

                _context.Services.Add(service);
                _context.SaveChanges();
            }

            public void EditService(Service updatedService)
            {
                if (updatedService == null)
                    throw new ArgumentNullException(nameof(updatedService));

                var existingService = _context.Services.Find(updatedService.ServiceId);
                if (existingService == null)
                    throw new InvalidOperationException("Услуга не найдена.");

                existingService.Name = updatedService.Name;
                existingService.Description = updatedService.Description;
                existingService.Price = updatedService.Price;
                existingService.Rating = updatedService.Rating;
                existingService.Location = updatedService.Location;
                existingService.ServiceType = updatedService.ServiceType;

                _context.SaveChanges();
            }

            public void DeleteService(int serviceId)
            {
                var service = _context.Services.Find(serviceId);
                if (service == null)
                    throw new InvalidOperationException("Услуга не найдена.");

                _context.Services.Remove(service);
                _context.SaveChanges();
            }
        }


        public void ManageServices()
        {
            using (var context = new AppDbContext())
            {
                var adminManagement = new AdminManagement(context);

                var newService = new Service
                {
                    Name = "Люкс Отель",
                    Description = "5-ти Звездночный Отель с Видом на Центр.",
                    Price = 300,
                    Rating = 4.8m,
                    Location = "New York",
                    ServiceType = "Отель"
                };
                adminManagement.AddService(newService);

                var updatedService = new Service
                {
                    ServiceId = newService.ServiceId,
                    Name = "Обновленный Люкс Отель",
                    Description = "Обновленное описание.",
                    Price = 400,
                    Rating = 4.9m,
                    Location = "New York",
                    ServiceType = "Отель"
                };
                adminManagement.EditService(updatedService);

                adminManagement.DeleteService(newService.ServiceId);
            }
        }

       


        public partial class RegisterForm : Form
        {
            public UserService userService;

            public RegisterForm(UserService service)
            {
                userService = service;
            }

            private void button1(object sender, EventArgs e)
            {
                var username = textBox1.Text;
                var password = textBox2.Text;
                var email = textBox3.Text;

                userService.RegisterUser(username, password, email, "Клиент");
                MessageBox.Show("Пользователь зарегистрирован!");
            }
        }







        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }


        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
