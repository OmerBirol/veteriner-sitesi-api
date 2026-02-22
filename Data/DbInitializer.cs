using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VetRandevu.Api.Models;
using VetRandevu.Api.Security;

namespace VetRandevu.Api.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<VetRandevuDbContext>();
        await db.Database.MigrateAsync();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        await EnsureRoleAsync(roleManager, Roles.Admin);
        await EnsureRoleAsync(roleManager, Roles.ClinicAdmin);
        await EnsureRoleAsync(roleManager, Roles.User);

        var adminEmail = configuration["SeedAdmin:Email"];
        var adminPassword = configuration["SeedAdmin:Password"];
        var clinicAdminEmail = configuration["SeedClinicAdmin:Email"];
        var clinicAdminPassword = configuration["SeedClinicAdmin:Password"];

        if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var existing = await userManager.FindByEmailAsync(adminEmail);

            if (existing is null)
            {
                var admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(admin, adminPassword);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Seed admin oluşturulamadı: {errors}");
                }

                await userManager.AddToRoleAsync(admin, Roles.Admin);
            }
            else
            {
                var passwordValid = await userManager.CheckPasswordAsync(existing, adminPassword);
                if (!passwordValid)
                {
                    var token = await userManager.GeneratePasswordResetTokenAsync(existing);
                    await userManager.ResetPasswordAsync(existing, token, adminPassword);
                }

                if (!await userManager.IsInRoleAsync(existing, Roles.Admin))
                {
                    await userManager.AddToRoleAsync(existing, Roles.Admin);
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(clinicAdminEmail) && !string.IsNullOrWhiteSpace(clinicAdminPassword))
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var existing = await userManager.FindByEmailAsync(clinicAdminEmail);

            if (existing is null)
            {
                var clinicAdmin = new ApplicationUser
                {
                    UserName = clinicAdminEmail,
                    Email = clinicAdminEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(clinicAdmin, clinicAdminPassword);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Seed clinic admin oluşturulamadı: {errors}");
                }

                await userManager.AddToRoleAsync(clinicAdmin, Roles.ClinicAdmin);
            }
            else
            {
                var passwordValid = await userManager.CheckPasswordAsync(existing, clinicAdminPassword);
                if (!passwordValid)
                {
                    var token = await userManager.GeneratePasswordResetTokenAsync(existing);
                    await userManager.ResetPasswordAsync(existing, token, clinicAdminPassword);
                }

                if (!await userManager.IsInRoleAsync(existing, Roles.ClinicAdmin))
                {
                    await userManager.AddToRoleAsync(existing, Roles.ClinicAdmin);
                }
            }
        }

        var clinics = new List<Clinic>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "PatiCare Veteriner",
                City = "Istanbul",
                Address = "Kadikoy, Moda Cad. No:12",
                Phone = "+90 212 000 11 11",
                Description = "7/24 acil hizmet ve uzman hekim kadrosu.",
                ImageUrl = "https://images.unsplash.com/photo-1529778873920-4da4926a72c2",
                Rating = 0,
                IsApproved = true,
                Latitude = 40.9859,
                Longitude = 29.0245
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "VetLife Klinik",
                City = "Ankara",
                Address = "Cankaya, Ataturk Bulv. No:58",
                Phone = "+90 312 000 22 22",
                Description = "Evcil hayvanlar için kapsamli saglik paketleri.",
                ImageUrl = "https://images.unsplash.com/photo-1517849845537-4d257902454a",
                Rating = 0,
                IsApproved = true,
                Latitude = 39.9208,
                Longitude = 32.8541
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Izmir Vet Center",
                City = "Izmir",
                Address = "Karsiyaka, Iskele Sk. No:5",
                Phone = "+90 232 000 33 33",
                Description = "Modern cihazlar ve hizli randevu sistemi.",
                ImageUrl = "https://images.unsplash.com/photo-1450778869180-41d0601e046e",
                Rating = 0,
                IsApproved = true,
                Latitude = 38.4237,
                Longitude = 27.1428
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Bursa Animal Hospital",
                City = "Bursa",
                Address = "Nilufer, Cumhuriyet Cd. No:34",
                Phone = "+90 224 000 44 44",
                Description = "Cerrahi ve ic hastaliklar uzmanligi.",
                ImageUrl = "https://images.unsplash.com/photo-1517423440428-a5a00ad493e8",
                Rating = 0,
                IsApproved = true,
                Latitude = 40.1828,
                Longitude = 29.0663
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Antalya Pet Care",
                City = "Antalya",
                Address = "Muratpasa, Lara Cd. No:90",
                Phone = "+90 242 000 55 55",
                Description = "Sicak yaklasim, deneyimli veteriner ekibi.",
                ImageUrl = "https://images.unsplash.com/photo-1548199973-03cce0bbc87b",
                Rating = 0,
                IsApproved = true,
                Latitude = 36.8969,
                Longitude = 30.7133
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Ege Pati Klinigi",
                City = "Izmir",
                Address = "Bornova, Universite Cd. No:21",
                Phone = "+90 232 000 66 66",
                Description = "Kedi ve kopekler icin uzman kadro.",
                ImageUrl = "https://images.unsplash.com/photo-1508672019048-805c876b67e2",
                Rating = 0,
                IsApproved = true,
                Latitude = 38.4560,
                Longitude = 27.2252
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Karadeniz Vet",
                City = "Samsun",
                Address = "Atakum, Sahil Yolu No:7",
                Phone = "+90 362 000 77 77",
                Description = "Acil ve rutin muayene hizmetleri.",
                ImageUrl = "https://images.unsplash.com/photo-1517849845537-4d257902454a",
                Rating = 0,
                IsApproved = true,
                Latitude = 41.2928,
                Longitude = 36.3313
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Marmara Petline",
                City = "Istanbul",
                Address = "Besiktas, Barbaros Blv. No:52",
                Phone = "+90 212 000 88 88",
                Description = "Modern cihazlar ve deneyimli ekip.",
                ImageUrl = "https://images.unsplash.com/photo-1494256997604-768d1f608cac",
                Rating = 0,
                IsApproved = true,
                Latitude = 41.0420,
                Longitude = 29.0084
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Adana VetPlus",
                City = "Adana",
                Address = "Seyhan, Ataturk Cd. No:110",
                Phone = "+90 322 000 99 99",
                Description = "Cerrahi ve dahiliye hizmetleri.",
                ImageUrl = "https://images.unsplash.com/photo-1517849845537-4d257902454a",
                Rating = 0,
                IsApproved = true,
                Latitude = 37.0000,
                Longitude = 35.3213
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Eskisehir Pati",
                City = "Eskisehir",
                Address = "Tepebasi, Porsuk Sk. No:15",
                Phone = "+90 222 000 10 10",
                Description = "Sicak yaklasim, hizli randevu.",
                ImageUrl = "https://images.unsplash.com/photo-1517423440428-a5a00ad493e8",
                Rating = 0,
                IsApproved = true,
                Latitude = 39.7767,
                Longitude = 30.5206
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Gaziantep VetCare",
                City = "Gaziantep",
                Address = "Sehitkamil, Sanayi Cd. No:44",
                Phone = "+90 342 000 11 10",
                Description = "Acil servis ve laboratuvar hizmetleri.",
                ImageUrl = "https://images.unsplash.com/photo-1450778869180-41d0601e046e",
                Rating = 0,
                IsApproved = true,
                Latitude = 37.0662,
                Longitude = 37.3833
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Konya Animal Center",
                City = "Konya",
                Address = "Selcuklu, Meram Cd. No:9",
                Phone = "+90 332 000 12 12",
                Description = "Kapsamli saglik paketleri.",
                ImageUrl = "https://images.unsplash.com/photo-1529778873920-4da4926a72c2",
                Rating = 0,
                IsApproved = true,
                Latitude = 37.8714,
                Longitude = 32.4846
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Mersin Pati",
                City = "Mersin",
                Address = "Yenisehir, Liman Cd. No:5",
                Phone = "+90 324 000 13 13",
                Description = "Aile dostu veteriner ekibi.",
                ImageUrl = "https://images.unsplash.com/photo-1548199973-03cce0bbc87b",
                Rating = 0,
                IsApproved = true,
                Latitude = 36.8121,
                Longitude = 34.6415
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Kayseri Vet Health",
                City = "Kayseri",
                Address = "Melikgazi, Cumhuriyet Cd. No:28",
                Phone = "+90 352 000 14 14",
                Description = "Dijital randevu ve takip sistemi.",
                ImageUrl = "https://images.unsplash.com/photo-1517849845537-4d257902454a",
                Rating = 0,
                IsApproved = true,
                Latitude = 38.7205,
                Longitude = 35.4826
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Trabzon Pet Klinik",
                City = "Trabzon",
                Address = "Ortahisar, Meydan Sk. No:3",
                Phone = "+90 462 000 15 15",
                Description = "Acil ve rutin kontroller.",
                ImageUrl = "https://images.unsplash.com/photo-1508672019048-805c876b67e2",
                Rating = 0,
                IsApproved = true,
                Latitude = 41.0027,
                Longitude = 39.7168
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Denizli Vet Point",
                City = "Denizli",
                Address = "Merkezefendi, Camlik Cd. No:19",
                Phone = "+90 258 000 16 16",
                Description = "Dis bakimi ve mikrocip hizmetleri.",
                ImageUrl = "https://images.unsplash.com/photo-1494256997604-768d1f608cac",
                Rating = 0,
                IsApproved = true,
                Latitude = 37.7765,
                Longitude = 29.0864
            }
        };

        var existingClinics = await db.Clinics.ToListAsync();
        var existingClinicNames = existingClinics.Select(c => c.Name).ToList();
        var existingByName = existingClinics.ToDictionary(c => c.Name, c => c);

        foreach (var clinic in clinics)
        {
            if (existingClinicNames.Contains(clinic.Name))
            {
                if (existingByName.TryGetValue(clinic.Name, out var existing))
                {
                    if (existing.Latitude is null && clinic.Latitude is not null)
                    {
                        existing.Latitude = clinic.Latitude;
                    }
                    if (existing.Longitude is null && clinic.Longitude is not null)
                    {
                        existing.Longitude = clinic.Longitude;
                    }
                }
                continue;
            }

            db.Clinics.Add(clinic);

            db.Services.AddRange(
                new Service
                {
                    Id = Guid.NewGuid(),
                    ClinicId = clinic.Id,
                    Name = "Muayene",
                    Price = 500,
                    DurationMinutes = 30
                },
                new Service
                {
                    Id = Guid.NewGuid(),
                    ClinicId = clinic.Id,
                    Name = "Acil Muayene",
                    Price = 850,
                    DurationMinutes = 45
                },
                new Service
                {
                    Id = Guid.NewGuid(),
                    ClinicId = clinic.Id,
                    Name = "Asi Uygulamasi",
                    Price = 350,
                    DurationMinutes = 20
                },
                new Service
                {
                    Id = Guid.NewGuid(),
                    ClinicId = clinic.Id,
                    Name = "Genel Kontrol",
                    Price = 650,
                    DurationMinutes = 40
                },
                new Service
                {
                    Id = Guid.NewGuid(),
                    ClinicId = clinic.Id,
                    Name = "Dis Temizligi",
                    Price = 700,
                    DurationMinutes = 45
                },
                new Service
                {
                    Id = Guid.NewGuid(),
                    ClinicId = clinic.Id,
                    Name = "Ultrason",
                    Price = 900,
                    DurationMinutes = 30
                },
                new Service
                {
                    Id = Guid.NewGuid(),
                    ClinicId = clinic.Id,
                    Name = "Kan Testi",
                    Price = 600,
                    DurationMinutes = 25
                },
                new Service
                {
                    Id = Guid.NewGuid(),
                    ClinicId = clinic.Id,
                    Name = "Mikrocip",
                    Price = 450,
                    DurationMinutes = 15
                },
                new Service
                {
                    Id = Guid.NewGuid(),
                    ClinicId = clinic.Id,
                    Name = "Kisirlastirma",
                    Price = 1500,
                    DurationMinutes = 90
                },
                new Service
                {
                    Id = Guid.NewGuid(),
                    ClinicId = clinic.Id,
                    Name = "Pet Pansiyon",
                    Price = 1200,
                    DurationMinutes = 60
                });

            var baseDate = DateTime.UtcNow.Date.AddDays(1);
            for (var dayOffset = 0; dayOffset < 7; dayOffset++)
            {
                var day = baseDate.AddDays(dayOffset);
                db.Slots.AddRange(
                    new AvailabilitySlot
                    {
                        Id = Guid.NewGuid(),
                        ClinicId = clinic.Id,
                        StartUtc = day.AddHours(9),
                        EndUtc = day.AddHours(10),
                        IsBooked = false
                    },
                    new AvailabilitySlot
                    {
                        Id = Guid.NewGuid(),
                        ClinicId = clinic.Id,
                        StartUtc = day.AddHours(11),
                        EndUtc = day.AddHours(12),
                        IsBooked = false
                    },
                    new AvailabilitySlot
                    {
                        Id = Guid.NewGuid(),
                        ClinicId = clinic.Id,
                        StartUtc = day.AddHours(14),
                        EndUtc = day.AddHours(15),
                        IsBooked = false
                    },
                    new AvailabilitySlot
                    {
                        Id = Guid.NewGuid(),
                        ClinicId = clinic.Id,
                        StartUtc = day.AddHours(16),
                        EndUtc = day.AddHours(17),
                        IsBooked = false
                    });
            }

            var reviews = new List<Review>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ClinicId = clinic.Id,
                    UserId = "seed",
                    Rating = 5,
                    Comment = "Cok ilgili ve hizli hizmet.",
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-3)
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    ClinicId = clinic.Id,
                    UserId = "seed",
                    Rating = 4,
                    Comment = "Randevu sureci kolaydi.",
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-1)
                }
            };

            db.Reviews.AddRange(reviews);
            clinic.Rating = Math.Round(reviews.Average(r => r.Rating), 2);
        }

        await db.SaveChangesAsync();
    }

    private static async Task EnsureRoleAsync(RoleManager<IdentityRole> roleManager, string roleName)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }
}
