// Data/ApplicationDbContext.cs
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AtelierProject.Models;

namespace AtelierProject.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Branch> Branches { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<ProductDefinition> ProductDefinitions { get; set; }
        public DbSet<ProductItem> ProductItems { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<BookingItem> BookingItems { get; set; }
        public DbSet<BookingRentalItem> BookingRentalItems { get; set; }
        public DbSet<BookingServiceItem> BookingServiceItems { get; set; }

        public DbSet<SalonService> SalonServices { get; set; }
        public DbSet<SalonAppointment> SalonAppointments { get; set; }
        public DbSet<SalonAppointmentItem> SalonAppointmentItems { get; set; }

        public DbSet<ExpenseCategory> ExpenseCategories { get; set; }
        public DbSet<Expense> Expenses { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // يمكنك إضافة قيود أو علاقات معقدة هنا
            // مثال: جعل الباركود فريداً
            builder.Entity<ProductItem>()
                .HasIndex(p => p.Barcode)
                .IsUnique();
        }
    }
}