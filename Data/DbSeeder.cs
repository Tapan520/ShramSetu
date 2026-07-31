using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Core.Entities;
using ShramSetu.Core.Enums;

namespace ShramSetu.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
        var db          = services.GetRequiredService<ApplicationDbContext>();

        await db.Database.EnsureCreatedAsync();

        // ?? Roles ??????????????????????????????????????????????????????????????
        foreach (var role in new[] { "Admin", "Employer", "Worker" })
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));

        // ?? Admin User ?????????????????????????????????????????????????????????
        const string adminEmail = "admin@shramsetu.in";
        if (await userManager.FindByEmailAsync(adminEmail) is null)
        {
            var admin = new IdentityUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
            if ((await userManager.CreateAsync(admin, "Admin@12345")).Succeeded)
                await userManager.AddToRoleAsync(admin, "Admin");
        }

        // ?? Skill Categories ???????????????????????????????????????????????????
        if (!db.SkillCategories.Any())
        {
            db.SkillCategories.AddRange(
                new() { Id = Guid.NewGuid(), Name = "Plumber",        IconCssClass = "bi bi-wrench" },
                new() { Id = Guid.NewGuid(), Name = "Electrician",    IconCssClass = "bi bi-lightning-charge" },
                new() { Id = Guid.NewGuid(), Name = "Carpenter",      IconCssClass = "bi bi-tools" },
                new() { Id = Guid.NewGuid(), Name = "Labourer",       IconCssClass = "bi bi-person-arms-up" },
                new() { Id = Guid.NewGuid(), Name = "Maid / Cook",    IconCssClass = "bi bi-house-heart" },
                new() { Id = Guid.NewGuid(), Name = "Barber",         IconCssClass = "bi bi-scissors" },
                new() { Id = Guid.NewGuid(), Name = "Driver",         IconCssClass = "bi bi-truck" },
                new() { Id = Guid.NewGuid(), Name = "Security Guard", IconCssClass = "bi bi-shield-check" },
                new() { Id = Guid.NewGuid(), Name = "Painter",        IconCssClass = "bi bi-brush" },
                new() { Id = Guid.NewGuid(), Name = "Welder",         IconCssClass = "bi bi-fire" }
            );
            await db.SaveChangesAsync();
        }

        // ?? Subscription Plans ?????????????????????????????????????????????????
        if (!db.SubscriptionPlans.Any())
        {
            db.SubscriptionPlans.AddRange(
                new() { Id = Guid.NewGuid(), Name = "Free",       Tier = SubscriptionTier.Free,       PriceMonthly = 0,    PriceYearly = 0,     MaxJobPosts = 2,  MaxSourcingRequests = 1,  CanAccessChat = false, CanAccessAnalytics = false, PrioritySupport = false },
                new() { Id = Guid.NewGuid(), Name = "Basic",      Tier = SubscriptionTier.Basic,      PriceMonthly = 499,  PriceYearly = 4999,  MaxJobPosts = 10, MaxSourcingRequests = 5,  CanAccessChat = true,  CanAccessAnalytics = false, PrioritySupport = false },
                new() { Id = Guid.NewGuid(), Name = "Pro",        Tier = SubscriptionTier.Pro,        PriceMonthly = 1499, PriceYearly = 14999, MaxJobPosts = 50, MaxSourcingRequests = 20, CanAccessChat = true,  CanAccessAnalytics = true,  PrioritySupport = false },
                new() { Id = Guid.NewGuid(), Name = "Enterprise", Tier = SubscriptionTier.Enterprise, PriceMonthly = 4999, PriceYearly = 49999, MaxJobPosts = -1, MaxSourcingRequests = -1, CanAccessChat = true,  CanAccessAnalytics = true,  PrioritySupport = true  }
            );
            await db.SaveChangesAsync();
        }

        // Skip remaining seed if test data already exists
        if (db.Workers.Any()) return;

        // ??????????????????????????????????????????????????????????????????????
        //  TEST DATA  realistic Indian names, cities, phone numbers
        // ??????????????????????????????????????????????????????????????????????

        var skills    = await db.SkillCategories.ToListAsync();
        var planBasic = await db.SubscriptionPlans.FirstAsync(p => p.Tier == SubscriptionTier.Basic);
        var planPro   = await db.SubscriptionPlans.FirstAsync(p => p.Tier == SubscriptionTier.Pro);

        SkillCategory Skill(string name) => skills.First(s => s.Name == name);

        // ?? Module 1: Worker Users (10 workers) ???????????????????????????????
        var workerData = new[]
        {
            ("Ramesh Kumar",     "9876543201", "Plumber",        "Mumbai",    "Maharashtra", 5,  650m,  19100m, 18.9388, 72.8354),
            ("Suresh Yadav",     "9876543202", "Electrician",    "Pune",      "Maharashtra", 8,  800m,  21000m, 18.5204, 73.8567),
            ("Mohan Lal",        "9876543203", "Carpenter",      "Delhi",     "Delhi",       10, 900m,  23000m, 28.7041, 77.1025),
            ("Priya Devi",       "9876543204", "Maid / Cook",    "Bengaluru", "Karnataka",   3,  500m,  14000m, 12.9716, 77.5946),
            ("Rajesh Singh",     "9876543205", "Painter",        "Hyderabad", "Telangana",   6,  700m,  18000m, 17.3850, 78.4867),
            ("Abdul Karim",      "9876543206", "Welder",         "Chennai",   "Tamil Nadu",  12, 1000m, 26000m, 13.0827, 80.2707),
            ("Geeta Kumari",     "9876543207", "Labourer",       "Kolkata",   "West Bengal", 2,  450m,  12000m, 22.5726, 88.3639),
            ("Vikram Patel",     "9876543208", "Driver",         "Ahmedabad", "Gujarat",     7,  750m,  19500m, 23.0225, 72.5714),
            ("Santosh Sharma",   "9876543209", "Security Guard", "Jaipur",    "Rajasthan",   4,  600m,  15600m, 26.9124, 75.7873),
            ("Lakshmi Naidu",    "9876543210", "Electrician",    "Visakhapatnam","Andhra Pradesh",9, 850m,22000m,17.6868,83.2185),
        };

        var workerUsers   = new List<IdentityUser>();
        var workerEntities = new List<Worker>();

        for (int i = 0; i < workerData.Length; i++)
        {
            var (name, phone, skillName, city, state, exp, daily, monthly, lat, lng) = workerData[i];
            var email = $"worker{i + 1}@shramsetu.in";

            var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
            if ((await userManager.CreateAsync(user, "Worker@12345")).Succeeded)
                await userManager.AddToRoleAsync(user, "Worker");

            workerUsers.Add(user);

            var worker = new Worker
            {
                Id = Guid.NewGuid(), UserId = user.Id,
                FullName = name, Phone = phone,
                SkillCategoryId = Skill(skillName).Id,
                SubSkills = skillName switch {
                    "Plumber"       => "pipe fitting, bathroom fitting, leak repair",
                    "Electrician"   => "wiring, panel board, inverter installation",
                    "Carpenter"     => "furniture making, door fitting, wood polish",
                    "Welder"        => "arc welding, MIG welding, fabrication",
                    "Driver"        => "LMV, HMV, outstation driving",
                    _               => null
                },
                YearsOfExperience  = exp,
                ExpectedDailyWage  = daily,
                ExpectedMonthlyWage = monthly,
                LocationCity       = city,
                LocationState      = state,
                Latitude           = lat,
                Longitude          = lng,
                IsAvailable        = i % 5 != 4,   // 4 out of 5 available
                KycStatus          = i < 7 ? VerificationStatus.Verified : VerificationStatus.Pending,
                PhotoUrl           = $"/images/workers/worker{i + 1}.png",
                CreatedAt          = DateTime.UtcNow.AddDays(-(60 - i * 5))
            };
            workerEntities.Add(worker);
        }

        db.Workers.AddRange(workerEntities);
        await db.SaveChangesAsync();

        // ?? Module 2: Employer Users (5 employers) ?????????????????????????????
        var employerData = new[]
        {
            ("Anjali Constructions",   "employer1@shramsetu.in", "9811100001", EmployerType.Company,     "Harish Mehra",    "Mumbai",    "Maharashtra"),
            ("BuildRight Pvt Ltd",     "employer2@shramsetu.in", "9811100002", EmployerType.Company,     "Sunil Batra",     "Delhi",     "Delhi"),
            ("HomeFix Services",       "employer3@shramsetu.in", "9811100003", EmployerType.Individual,  "Kavita Sharma",   "Pune",      "Maharashtra"),
            ("RapidLabour Agency",     "employer4@shramsetu.in", "9811100004", EmployerType.Contractor,  "Mohammed Arif",   "Hyderabad", "Telangana"),
            ("GreenCity Infra",        "employer5@shramsetu.in", "9811100005", EmployerType.Company,     "Deepak Nair",     "Bengaluru", "Karnataka"),
        };

        var employerUsers    = new List<IdentityUser>();
        var employerEntities = new List<EmployerAccount>();

        for (int i = 0; i < employerData.Length; i++)
        {
            var (company, email, phone, type, contactName, city, state) = employerData[i];
            var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
            if ((await userManager.CreateAsync(user, "Employer@12345")).Succeeded)
                await userManager.AddToRoleAsync(user, "Employer");

            employerUsers.Add(user);

            employerEntities.Add(new EmployerAccount
            {
                Id = Guid.NewGuid(), UserId = user.Id,
                Name = contactName, CompanyName = company,
                Type = type, Phone = phone, Email = email,
                CreatedAt = DateTime.UtcNow.AddDays(-(90 - i * 10))
            });
        }

        db.EmployerAccounts.AddRange(employerEntities);
        await db.SaveChangesAsync();

        // ?? Module 3: Job Posts (8 open jobs) ?????????????????????????????????
        var jobPosts = new List<JobPost>
        {
            new() { Id=Guid.NewGuid(), EmployerId=employerEntities[0].Id, SkillCategoryId=Skill("Plumber").Id,        Title="Senior Plumber for Residential Project",      Description="Need experienced plumber for 3BHK bathroom fitting in Andheri.",      LocationCity="Mumbai",    LocationState="Maharashtra", DailyWage=700m,  DurationDays=15, VacancyCount=2, Status=JobPostStatus.Open,   CreatedAt=DateTime.UtcNow.AddDays(-10) },
            new() { Id=Guid.NewGuid(), EmployerId=employerEntities[1].Id, SkillCategoryId=Skill("Electrician").Id,    Title="Electrician for Commercial Building Wiring",   Description="Full building electrical work in Connaught Place. PF/ESI provided.",  LocationCity="Delhi",     LocationState="Delhi",       DailyWage=900m,  DurationDays=30, VacancyCount=4, Status=JobPostStatus.Open,   CreatedAt=DateTime.UtcNow.AddDays(-7)  },
            new() { Id=Guid.NewGuid(), EmployerId=employerEntities[2].Id, SkillCategoryId=Skill("Carpenter").Id,      Title="Carpenter for Interior Work",                  Description="Modular kitchen and wardrobe fitting. Pune Kothrud location.",         LocationCity="Pune",      LocationState="Maharashtra", DailyWage=800m,  DurationDays=20, VacancyCount=1, Status=JobPostStatus.Open,   CreatedAt=DateTime.UtcNow.AddDays(-5)  },
            new() { Id=Guid.NewGuid(), EmployerId=employerEntities[3].Id, SkillCategoryId=Skill("Labourer").Id,       Title="Site Labourers for Road Construction",         Description="Urgently need 10 labourers. Daily wages + food + accommodation.",     LocationCity="Hyderabad", LocationState="Telangana",   DailyWage=500m,  DurationDays=60, VacancyCount=10,Status=JobPostStatus.Open,   CreatedAt=DateTime.UtcNow.AddDays(-3)  },
            new() { Id=Guid.NewGuid(), EmployerId=employerEntities[4].Id, SkillCategoryId=Skill("Welder").Id,         Title="Skilled Welder for Steel Structure",           Description="MIG/ARC welding required for metro station structure.",               LocationCity="Bengaluru", LocationState="Karnataka",   DailyWage=1100m, DurationDays=45, VacancyCount=3, Status=JobPostStatus.Open,   CreatedAt=DateTime.UtcNow.AddDays(-2)  },
            new() { Id=Guid.NewGuid(), EmployerId=employerEntities[0].Id, SkillCategoryId=Skill("Painter").Id,        Title="Painter for Apartment Complex",                Description="Interior & exterior painting. 50 flats. Material provided.",          LocationCity="Mumbai",    LocationState="Maharashtra", DailyWage=750m,  DurationDays=25, VacancyCount=5, Status=JobPostStatus.Open,   CreatedAt=DateTime.UtcNow.AddDays(-1)  },
            new() { Id=Guid.NewGuid(), EmployerId=employerEntities[1].Id, SkillCategoryId=Skill("Security Guard").Id, Title="Security Guards for Mall",                     Description="12-hour shift. Uniform + EPF provided. Exp with CCTV preferred.",    LocationCity="Delhi",     LocationState="Delhi",       DailyWage=650m,  DurationDays=90, VacancyCount=6, Status=JobPostStatus.Closed, CreatedAt=DateTime.UtcNow.AddDays(-20) },
            new() { Id=Guid.NewGuid(), EmployerId=employerEntities[2].Id, SkillCategoryId=Skill("Driver").Id,         Title="Driver for Corporate Office",                  Description="Office cab driver. 8AM-8PM. Petrol reimbursed. Outstation allowed.", LocationCity="Pune",      LocationState="Maharashtra", DailyWage=800m,  DurationDays=30, VacancyCount=2, Status=JobPostStatus.Open,   CreatedAt=DateTime.UtcNow.AddDays(-4)  },
        };

        db.JobPosts.AddRange(jobPosts);
        await db.SaveChangesAsync();

        // ?? Module 4: Job Applications ?????????????????????????????????????????
        var applications = new List<JobApplication>
        {
            new() { Id=Guid.NewGuid(), JobPostId=jobPosts[0].Id, WorkerId=workerEntities[0].Id, Status=JobApplicationStatus.Accepted,    AppliedAt=DateTime.UtcNow.AddDays(-9), CoverNote="5 years exp in Mumbai. Available immediately." },
            new() { Id=Guid.NewGuid(), JobPostId=jobPosts[0].Id, WorkerId=workerEntities[3].Id, Status=JobApplicationStatus.Rejected,    AppliedAt=DateTime.UtcNow.AddDays(-8), CoverNote="Can start from next week." },
            new() { Id=Guid.NewGuid(), JobPostId=jobPosts[1].Id, WorkerId=workerEntities[1].Id, Status=JobApplicationStatus.Shortlisted, AppliedAt=DateTime.UtcNow.AddDays(-6), CoverNote="Certified electrician. Available from Monday." },
            new() { Id=Guid.NewGuid(), JobPostId=jobPosts[1].Id, WorkerId=workerEntities[9].Id, Status=JobApplicationStatus.Applied,     AppliedAt=DateTime.UtcNow.AddDays(-5), CoverNote="8 years experience in commercial wiring." },
            new() { Id=Guid.NewGuid(), JobPostId=jobPosts[2].Id, WorkerId=workerEntities[2].Id, Status=JobApplicationStatus.Accepted,    AppliedAt=DateTime.UtcNow.AddDays(-4), CoverNote="Specialise in modular furniture." },
            new() { Id=Guid.NewGuid(), JobPostId=jobPosts[3].Id, WorkerId=workerEntities[6].Id, Status=JobApplicationStatus.Applied,     AppliedAt=DateTime.UtcNow.AddDays(-2), CoverNote="Ready to relocate. Team of 3 available." },
            new() { Id=Guid.NewGuid(), JobPostId=jobPosts[4].Id, WorkerId=workerEntities[5].Id, Status=JobApplicationStatus.Shortlisted, AppliedAt=DateTime.UtcNow.AddDays(-1), CoverNote="12 years welding. MIG certified." },
            new() { Id=Guid.NewGuid(), JobPostId=jobPosts[7].Id, WorkerId=workerEntities[7].Id, Status=JobApplicationStatus.Applied,     AppliedAt=DateTime.UtcNow.AddDays(-3), CoverNote="LMV + HMV licence. 7 years city driving." },
        };

        db.JobApplications.AddRange(applications);
        await db.SaveChangesAsync();

        // ?? Module 5: Bookings ?????????????????????????????????????????????????
        var bookings = new List<Booking>
        {
            new() { Id=Guid.NewGuid(), WorkerId=workerEntities[0].Id, EmployerId=employerEntities[0].Id, Type=BookingType.DirectContact, Status=BookingStatus.Completed,  StartDate=DateTime.UtcNow.AddDays(-40), DurationDays=15, AgreedWage=700m,  Notes="Bathroom fittings, 2nd floor apartment. Completed on time.", CreatedAt=DateTime.UtcNow.AddDays(-42) },
            new() { Id=Guid.NewGuid(), WorkerId=workerEntities[1].Id, EmployerId=employerEntities[1].Id, Type=BookingType.DirectContact, Status=BookingStatus.InProgress, StartDate=DateTime.UtcNow.AddDays(-5),  DurationDays=30, AgreedWage=900m,  Notes="Full building wiring. Floor by floor.",                     CreatedAt=DateTime.UtcNow.AddDays(-7)  },
            new() { Id=Guid.NewGuid(), WorkerId=workerEntities[2].Id, EmployerId=employerEntities[2].Id, Type=BookingType.DirectContact, Status=BookingStatus.Confirmed,  StartDate=DateTime.UtcNow.AddDays(2),   DurationDays=20, AgreedWage=800m,  Notes="Kitchen + wardrobe fitting. Materials by employer.",        CreatedAt=DateTime.UtcNow.AddDays(-2)  },
            new() { Id=Guid.NewGuid(), WorkerId=workerEntities[5].Id, EmployerId=employerEntities[4].Id, Type=BookingType.ViaAdmin,      Status=BookingStatus.InProgress, StartDate=DateTime.UtcNow.AddDays(-10), DurationDays=45, AgreedWage=1100m, Notes="Steel structure welding for metro project.",                CreatedAt=DateTime.UtcNow.AddDays(-12) },
            new() { Id=Guid.NewGuid(), WorkerId=workerEntities[4].Id, EmployerId=employerEntities[0].Id, Type=BookingType.DirectContact, Status=BookingStatus.Completed,  StartDate=DateTime.UtcNow.AddDays(-30), DurationDays=10, AgreedWage=750m,  Notes="Interior painting. 10 units done.",                         CreatedAt=DateTime.UtcNow.AddDays(-32) },
            new() { Id=Guid.NewGuid(), WorkerId=workerEntities[7].Id, EmployerId=employerEntities[2].Id, Type=BookingType.DirectContact, Status=BookingStatus.Requested,  StartDate=DateTime.UtcNow.AddDays(5),   DurationDays=30, AgreedWage=800m,  Notes="Corporate cab driver. Interview done.",                     CreatedAt=DateTime.UtcNow.AddDays(-1)  },
            new() { Id=Guid.NewGuid(), WorkerId=workerEntities[8].Id, EmployerId=employerEntities[1].Id, Type=BookingType.ViaAdmin,      Status=BookingStatus.Completed,  StartDate=DateTime.UtcNow.AddDays(-60), DurationDays=30, AgreedWage=650m,  Notes="Mall security. Night shift.",                               CreatedAt=DateTime.UtcNow.AddDays(-62) },
            new() { Id=Guid.NewGuid(), WorkerId=workerEntities[3].Id, EmployerId=employerEntities[4].Id, Type=BookingType.DirectContact, Status=BookingStatus.Cancelled,  StartDate=DateTime.UtcNow.AddDays(-15), DurationDays=7,  AgreedWage=500m,  Notes="Cancelled due to worker unavailability.",                   CreatedAt=DateTime.UtcNow.AddDays(-16) },
        };

        db.Bookings.AddRange(bookings);
        await db.SaveChangesAsync();

        // ?? Module 6: Reviews ??????????????????????????????????????????????????
        var reviews = new List<Review>
        {
            new() { Id=Guid.NewGuid(), BookingId=bookings[0].Id, WorkerId=workerEntities[0].Id, EmployerId=employerEntities[0].Id, Rating=5, Comment="Excellent work! Ramesh completed everything on time. Very professional.",   CreatedAt=DateTime.UtcNow.AddDays(-24) },
            new() { Id=Guid.NewGuid(), BookingId=bookings[4].Id, WorkerId=workerEntities[4].Id, EmployerId=employerEntities[0].Id, Rating=4, Comment="Good painter. Minor touch-ups were needed but overall satisfied.",           CreatedAt=DateTime.UtcNow.AddDays(-20) },
            new() { Id=Guid.NewGuid(), BookingId=bookings[6].Id, WorkerId=workerEntities[8].Id, EmployerId=employerEntities[1].Id, Rating=5, Comment="Santosh was very vigilant and disciplined. Will hire again.",               CreatedAt=DateTime.UtcNow.AddDays(-28) },
        };

        db.Reviews.AddRange(reviews);
        await db.SaveChangesAsync();

        // ?? Module 7: Worker Badges ????????????????????????????????????????????
        db.WorkerBadges.AddRange(
            new() { Id=Guid.NewGuid(), WorkerId=workerEntities[0].Id, Tier=BadgeTier.AadhaarVerified,   IsActive=true },
            new() { Id=Guid.NewGuid(), WorkerId=workerEntities[0].Id, Tier=BadgeTier.PhoneVerified,     IsActive=true },
            new() { Id=Guid.NewGuid(), WorkerId=workerEntities[1].Id, Tier=BadgeTier.AadhaarVerified,   IsActive=true },
            new() { Id=Guid.NewGuid(), WorkerId=workerEntities[1].Id, Tier=BadgeTier.BackgroundCleared, IsActive=true },
            new() { Id=Guid.NewGuid(), WorkerId=workerEntities[2].Id, Tier=BadgeTier.PhoneVerified,     IsActive=true },
            new() { Id=Guid.NewGuid(), WorkerId=workerEntities[5].Id, Tier=BadgeTier.AadhaarVerified,   IsActive=true },
            new() { Id=Guid.NewGuid(), WorkerId=workerEntities[5].Id, Tier=BadgeTier.BackgroundCleared, IsActive=true },
            new() { Id=Guid.NewGuid(), WorkerId=workerEntities[8].Id, Tier=BadgeTier.PhoneVerified,     IsActive=true }
        );
        await db.SaveChangesAsync();

        // ?? Module 8: Disputes ?????????????????????????????????????????????????
        db.Disputes.AddRange(
            new() { Id=Guid.NewGuid(), RaisedByUserId=workerEntities[3].UserId!, AgainstUserId=employerEntities[4].UserId, BookingId=bookings[7].Id, Type=DisputeType.NonPayment,  Title="Wages not paid for 3 days",                          Description="Employer cancelled the booking but did not pay for 3 days already worked. Amount: ₹1,500.",                  Status=DisputeStatus.UnderReview, CreatedAt=DateTime.UtcNow.AddDays(-5) },
            new() { Id=Guid.NewGuid(), RaisedByUserId=workerEntities[6].UserId!, AgainstUserId=employerEntities[3].UserId, BookingId=null,           Type=DisputeType.Mistreatment, Title="Verbal abuse by site supervisor",                     Description="Supervisor used abusive language during work. Requesting intervention from ShramSetu admin.",               Status=DisputeStatus.Open,         CreatedAt=DateTime.UtcNow.AddDays(-2) },
            new() { Id=Guid.NewGuid(), RaisedByUserId=employerEntities[1].UserId, AgainstUserId=workerEntities[9].UserId!,BookingId=null,           Type=DisputeType.QualityIssue, Title="Poor workmanship - wiring done incorrectly",          Description="Electrician left job incomplete. Multiple shorts reported. Seeking refund of advance paid.",                Status=DisputeStatus.Open,         CreatedAt=DateTime.UtcNow.AddDays(-1) }
        );
        await db.SaveChangesAsync();

        // ?? Module 9: Sourcing Requests ????????????????????????????????????????
        var sourcingSkillId = Skill("Labourer").Id;
        db.SourcingRequests.AddRange(
            new() { Id=Guid.NewGuid(), EmployerId=employerEntities[0].Id, SkillCategoryId=Skill("Plumber").Id, Description="Need 5 Plumbers - 200-unit housing project in Navi Mumbai. 3-month contract.",       LocationCity="Mumbai",    LocationState="Maharashtra", WorkerCount=5,  BudgetPerDay=700m,  DurationDays=90,  Status=SourcingStatus.InProgress, CreatedAt=DateTime.UtcNow.AddDays(-15) },
            new() { Id=Guid.NewGuid(), EmployerId=employerEntities[3].Id, SkillCategoryId=sourcingSkillId,      Description="50 Labourers for NHAI highway. Food & accommodation included. 6-month contract.",   LocationCity="Hyderabad", LocationState="Telangana",   WorkerCount=50, BudgetPerDay=500m,  DurationDays=180, Status=SourcingStatus.Open,       CreatedAt=DateTime.UtcNow.AddDays(-8)  },
            new() { Id=Guid.NewGuid(), EmployerId=employerEntities[4].Id, SkillCategoryId=Skill("Welder").Id,  Description="10 Welders for metro station steel structure. Safety training provided. Bengaluru.", LocationCity="Bengaluru", LocationState="Karnataka",   WorkerCount=10, BudgetPerDay=1100m, DurationDays=120, Status=SourcingStatus.Fulfilled,  CreatedAt=DateTime.UtcNow.AddDays(-30) }
        );
        await db.SaveChangesAsync();

        // ?? Module 10: Attendance Records ??????????????????????????????????????
        for (int d = 0; d < 5; d++)
        {
            db.AttendanceRecords.Add(new AttendanceRecord
            {
                Id = Guid.NewGuid(), BookingId = bookings[1].Id,
                WorkerId = workerEntities[1].Id,
                Date = DateTime.UtcNow.AddDays(-(4 - d)).Date,
                Status = d == 2 ? AttendanceStatus.HalfDay : AttendanceStatus.Present,
                HoursWorked = d == 2 ? 4 : 8,
                Notes = d == 2 ? "Left early - family emergency" : null
            });
        }
        await db.SaveChangesAsync();

        // ?? Module 11: Payroll Records ?????????????????????????????????????????
        db.PayrollRecords.AddRange(
            new() { Id=Guid.NewGuid(), BookingId=bookings[0].Id, WorkerId=workerEntities[0].Id, EmployerId=employerEntities[0].Id, PeriodStart=bookings[0].StartDate, PeriodEnd=bookings[0].StartDate.AddDays(14), DaysWorked=15, DailyWage=700m,  GrossAmount=10500m, Deductions=0m,    NetAmount=10500m, Status=PayrollStatus.Paid,     PaidAt=DateTime.UtcNow.AddDays(-24) },
            new() { Id=Guid.NewGuid(), BookingId=bookings[4].Id, WorkerId=workerEntities[4].Id, EmployerId=employerEntities[0].Id, PeriodStart=bookings[4].StartDate, PeriodEnd=bookings[4].StartDate.AddDays(9),  DaysWorked=10, DailyWage=750m,  GrossAmount=7500m,  Deductions=375m,  NetAmount=7125m,  Status=PayrollStatus.Paid,     PaidAt=DateTime.UtcNow.AddDays(-19) },
            new() { Id=Guid.NewGuid(), BookingId=bookings[6].Id, WorkerId=workerEntities[8].Id, EmployerId=employerEntities[1].Id, PeriodStart=bookings[6].StartDate, PeriodEnd=bookings[6].StartDate.AddDays(29), DaysWorked=30, DailyWage=650m,  GrossAmount=19500m, Deductions=975m,  NetAmount=18525m, Status=PayrollStatus.Paid,     PaidAt=DateTime.UtcNow.AddDays(-29) },
            new() { Id=Guid.NewGuid(), BookingId=bookings[1].Id, WorkerId=workerEntities[1].Id, EmployerId=employerEntities[1].Id, PeriodStart=bookings[1].StartDate, PeriodEnd=bookings[1].StartDate.AddDays(29), DaysWorked=5,  DailyWage=900m,  GrossAmount=4500m,  Deductions=225m,  NetAmount=4275m,  Status=PayrollStatus.Draft,    PaidAt=null }
        );
        await db.SaveChangesAsync();

        // ?? Module 12: Worker Wallets & Transactions ???????????????????????????
        var wallets = new List<WorkerWallet>();
        for (int i = 0; i < 5; i++)
        {
            wallets.Add(new WorkerWallet
            {
                Id = Guid.NewGuid(), WorkerId = workerEntities[i].Id,
                Balance = (i + 1) * 2500m, UpiId = $"worker{i + 1}@paytm",
                BankAccountNumber = $"SBI{100000 + i}", IfscCode = "SBIN0001234"
            });
        }
        db.WorkerWallets.AddRange(wallets);
        await db.SaveChangesAsync();

        db.WalletTransactions.AddRange(
            new() { Id=Guid.NewGuid(), WalletId=wallets[0].Id, Type=WalletTransactionType.Credit,     Amount=10500m, BalanceAfter=10500m, Description="Payroll for booking - Anjali Constructions",   TransactedAt=DateTime.UtcNow.AddDays(-24) },
            new() { Id=Guid.NewGuid(), WalletId=wallets[0].Id, Type=WalletTransactionType.Withdrawal,  Amount=8000m,  BalanceAfter=2500m,  Description="Bank transfer to SBI account",                  TransactedAt=DateTime.UtcNow.AddDays(-23) },
            new() { Id=Guid.NewGuid(), WalletId=wallets[1].Id, Type=WalletTransactionType.Credit,     Amount=4275m,  BalanceAfter=4275m,  Description="Partial payroll - BuildRight Pvt Ltd",          TransactedAt=DateTime.UtcNow.AddDays(-3)  },
            new() { Id=Guid.NewGuid(), WalletId=wallets[1].Id, Type=WalletTransactionType.Debit,       Amount=275m,   BalanceAfter=4000m,  Description="Platform commission (2%)",                      TransactedAt=DateTime.UtcNow.AddDays(-3)  },
            new() { Id=Guid.NewGuid(), WalletId=wallets[2].Id, Type=WalletTransactionType.Credit,     Amount=16000m, BalanceAfter=16000m, Description="Payroll for booking - HomeFix Services",        TransactedAt=DateTime.UtcNow.AddDays(-18) },
            new() { Id=Guid.NewGuid(), WalletId=wallets[2].Id, Type=WalletTransactionType.Withdrawal,  Amount=13500m, BalanceAfter=2500m,  Description="UPI transfer to worker3@paytm",                 TransactedAt=DateTime.UtcNow.AddDays(-17) }
        );
        await db.SaveChangesAsync();

        // ?? Module 13: Salary Advances ?????????????????????????????????????????
        db.SalaryAdvances.AddRange(
            new() { Id=Guid.NewGuid(), WorkerId=workerEntities[0].Id, EmployerId=employerEntities[0].Id, BookingId=bookings[0].Id, Amount=2000m, AmountRepaid=2000m, Status=LoanStatus.Repaid, IssuedAt=DateTime.UtcNow.AddDays(-38), RepaidAt=DateTime.UtcNow.AddDays(-24), Notes="Medical emergency" },
            new() { Id=Guid.NewGuid(), WorkerId=workerEntities[5].Id, EmployerId=employerEntities[4].Id, BookingId=bookings[3].Id, Amount=3500m, AmountRepaid=1000m, Status=LoanStatus.Active,  IssuedAt=DateTime.UtcNow.AddDays(-8),  RepaidAt=null,                         Notes="Family function" },
            new() { Id=Guid.NewGuid(), WorkerId=workerEntities[6].Id, EmployerId=employerEntities[3].Id, BookingId=null,           Amount=1500m, AmountRepaid=0m,    Status=LoanStatus.Active,  IssuedAt=DateTime.UtcNow.AddDays(-2),  RepaidAt=null,                         Notes="Travel expenses to worksite" }
        );
        await db.SaveChangesAsync();

        // ?? Module 14: Background Checks ???????????????????????????????????????
        db.BackgroundChecks.AddRange(
            new() { Id=Guid.NewGuid(), WorkerId=workerEntities[0].Id, CheckType=BackgroundCheckType.Criminal,   Status=BackgroundCheckStatus.Passed,     RequestedAt=DateTime.UtcNow.AddDays(-35), CompletedAt=DateTime.UtcNow.AddDays(-30), ResultSummary="Clean record. No criminal history found.",        ProviderName="IDfy" },
            new() { Id=Guid.NewGuid(), WorkerId=workerEntities[1].Id, CheckType=BackgroundCheckType.Address,    Status=BackgroundCheckStatus.Passed,     RequestedAt=DateTime.UtcNow.AddDays(-28), CompletedAt=DateTime.UtcNow.AddDays(-25), ResultSummary="Address verified from Pune, Maharashtra.",        ProviderName="IDfy" },
            new() { Id=Guid.NewGuid(), WorkerId=workerEntities[5].Id, CheckType=BackgroundCheckType.Employment, Status=BackgroundCheckStatus.Passed,     RequestedAt=DateTime.UtcNow.AddDays(-15), CompletedAt=DateTime.UtcNow.AddDays(-12), ResultSummary="Previous employer confirmed 10+ years experience.", ProviderName="AuthBridge" },
            new() { Id=Guid.NewGuid(), WorkerId=workerEntities[8].Id, CheckType=BackgroundCheckType.Criminal,   Status=BackgroundCheckStatus.InProgress, RequestedAt=DateTime.UtcNow.AddDays(-3),  CompletedAt=null,                         ResultSummary="Verification in progress with local police.",     ProviderName="IDfy" }
        );
        await db.SaveChangesAsync();

        // ?? Module 15: Employer Subscriptions & Platform Fees ??????????????????
        var subBasic = new EmployerSubscription { Id=Guid.NewGuid(), EmployerId=employerEntities[0].Id, PlanId=planBasic.Id, Status=SubscriptionStatus.Active,  StartDate=DateTime.UtcNow.AddDays(-30), EndDate=DateTime.UtcNow.AddDays(60),  AmountPaid=499m   };
        var subPro   = new EmployerSubscription { Id=Guid.NewGuid(), EmployerId=employerEntities[1].Id, PlanId=planPro.Id,   Status=SubscriptionStatus.Active,  StartDate=DateTime.UtcNow.AddDays(-60), EndDate=DateTime.UtcNow.AddDays(305), AmountPaid=14999m };
        var subExp   = new EmployerSubscription { Id=Guid.NewGuid(), EmployerId=employerEntities[2].Id, PlanId=planBasic.Id, Status=SubscriptionStatus.Expired, StartDate=DateTime.UtcNow.AddDays(-90), EndDate=DateTime.UtcNow.AddDays(-1),  AmountPaid=499m   };
        db.EmployerSubscriptions.AddRange(subBasic, subPro, subExp);
        await db.SaveChangesAsync();

        db.PlatformFees.AddRange(
            new() { Id=Guid.NewGuid(), Type=PlatformFeeType.BookingCommission,  Status=PlatformFeeStatus.Collected, BookingId=bookings[0].Id, UserId=employerEntities[0].UserId, Amount=525m,   CommissionRate=5, CreatedAt=DateTime.UtcNow.AddDays(-24), CollectedAt=DateTime.UtcNow.AddDays(-24) },
            new() { Id=Guid.NewGuid(), Type=PlatformFeeType.BookingCommission,  Status=PlatformFeeStatus.Collected, BookingId=bookings[4].Id, UserId=employerEntities[0].UserId, Amount=375m,   CommissionRate=5, CreatedAt=DateTime.UtcNow.AddDays(-19), CollectedAt=DateTime.UtcNow.AddDays(-19) },
            new() { Id=Guid.NewGuid(), Type=PlatformFeeType.SubscriptionFee,    Status=PlatformFeeStatus.Collected, BookingId=null,            UserId=employerEntities[0].UserId, Amount=499m,   CommissionRate=0, CreatedAt=DateTime.UtcNow.AddDays(-30), CollectedAt=DateTime.UtcNow.AddDays(-30) },
            new() { Id=Guid.NewGuid(), Type=PlatformFeeType.SubscriptionFee,    Status=PlatformFeeStatus.Collected, BookingId=null,            UserId=employerEntities[1].UserId, Amount=14999m, CommissionRate=0, CreatedAt=DateTime.UtcNow.AddDays(-60), CollectedAt=DateTime.UtcNow.AddDays(-60) },
            new() { Id=Guid.NewGuid(), Type=PlatformFeeType.BookingCommission,  Status=PlatformFeeStatus.Pending,   BookingId=bookings[1].Id, UserId=employerEntities[1].UserId, Amount=1350m,  CommissionRate=5, CreatedAt=DateTime.UtcNow.AddDays(-5)  }
        );
        await db.SaveChangesAsync();

        // ?? Module 16: Announcements & Feature Flags ???????????????????????????
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        db.Announcements.AddRange(
            new() { Id=Guid.NewGuid(), Title="New Feature: GPS Worker Search",        Body="Employers can now search for workers within a specific radius using GPS. Try it today!", Target=AnnouncementTarget.Employers, ShowBanner=true,  BannerCssClass="alert-info",    SendPush=true,  IsActive=true, CreatedByUserId=adminUser!.Id, CreatedAt=DateTime.UtcNow.AddDays(-3),  ExpiresAt=DateTime.UtcNow.AddDays(7)  },
            new() { Id=Guid.NewGuid(), Title="Complete your Profile to Get More Jobs", Body="Workers with complete profiles get 3x more bookings. Upload your photo and documents today.", Target=AnnouncementTarget.Workers,   ShowBanner=true,  BannerCssClass="alert-warning", SendPush=true,  IsActive=true, CreatedByUserId=adminUser!.Id, CreatedAt=DateTime.UtcNow.AddDays(-1),  ExpiresAt=DateTime.UtcNow.AddDays(14) },
            new() { Id=Guid.NewGuid(), Title="System Maintenance Notice",              Body="Scheduled maintenance on Sunday 2:00-4:00 AM. Platform may be briefly unavailable.",         Target=AnnouncementTarget.All,       ShowBanner=true,  BannerCssClass="alert-danger",  SendPush=false, IsActive=true, CreatedByUserId=adminUser!.Id, CreatedAt=DateTime.UtcNow.AddDays(-7),  ExpiresAt=DateTime.UtcNow.AddDays(1)  }
        );

        db.FeatureFlags.AddRange(
            new() { Id=Guid.NewGuid(), Name="EnableJobAlerts",       Description="Send automated job alerts to workers when matching jobs are posted",  Status=FeatureFlagStatus.Enabled,  UpdatedByUserId=adminUser!.Id },
            new() { Id=Guid.NewGuid(), Name="EnableGPSSearch",        Description="Allow employers to search workers by GPS radius",                      Status=FeatureFlagStatus.Enabled,  UpdatedByUserId=adminUser!.Id },
            new() { Id=Guid.NewGuid(), Name="EnableSkillAssessment",  Description="Show skill quiz option on worker profile page",                        Status=FeatureFlagStatus.Enabled,  UpdatedByUserId=adminUser!.Id },
            new() { Id=Guid.NewGuid(), Name="EnableRazorpayPayment",  Description="Enable Razorpay live payment gateway integration",                     Status=FeatureFlagStatus.Disabled, UpdatedByUserId=adminUser!.Id },
            new() { Id=Guid.NewGuid(), Name="EnableWhatsAppBot",      Description="Enable WhatsApp webhook for job search via WhatsApp",                  Status=FeatureFlagStatus.Disabled, UpdatedByUserId=adminUser!.Id }
        );
        await db.SaveChangesAsync();

        // ?? Module 17: Notifications ???????????????????????????????????????????
        db.Notifications.AddRange(
            new() { Id=Guid.NewGuid(), UserId=workerEntities[0].UserId!, RecipientUserId=workerEntities[0].UserId!, Title="Booking Confirmed",            Message="Your booking with Anjali Constructions has been confirmed. Start date: today.", ActionUrl="/Account/Bookings", IsRead=true,  CreatedAt=DateTime.UtcNow.AddDays(-40) },
            new() { Id=Guid.NewGuid(), UserId=workerEntities[0].UserId!, RecipientUserId=workerEntities[0].UserId!, Title="Payment Received",             Message="Rs.10,500 has been credited to your ShramSetu wallet.", ActionUrl="/Workers/Wallet",      IsRead=true,  CreatedAt=DateTime.UtcNow.AddDays(-24) },
            new() { Id=Guid.NewGuid(), UserId=workerEntities[1].UserId!, RecipientUserId=workerEntities[1].UserId!, Title="New Job Match",                 Message="A new Electrician job in Delhi matches your profile. Rs.900/day for 30 days.", ActionUrl="/Jobs/Index",      IsRead=false, CreatedAt=DateTime.UtcNow.AddDays(-1)  },
            new() { Id=Guid.NewGuid(), UserId=workerEntities[5].UserId!, RecipientUserId=workerEntities[5].UserId!, Title="Salary Advance Approved",       Message="Your advance of Rs.3,500 has been approved by GreenCity Infra.", ActionUrl="/Workers/Wallet",           IsRead=false, CreatedAt=DateTime.UtcNow.AddDays(-7)  },
            new() { Id=Guid.NewGuid(), UserId=employerEntities[0].UserId, RecipientUserId=employerEntities[0].UserId,Title="New Application Received",    Message="Ramesh Kumar applied for your Plumber job in Mumbai.", ActionUrl="/Jobs/Applications",              IsRead=false, CreatedAt=DateTime.UtcNow.AddDays(-9)  },
            new() { Id=Guid.NewGuid(), UserId=employerEntities[1].UserId, RecipientUserId=employerEntities[1].UserId,Title="Dispute Filed Against You",    Message="A quality dispute has been raised against your booking. Please review.", ActionUrl="/Disputes/Index",  IsRead=false, CreatedAt=DateTime.UtcNow.AddDays(-1)  }
        );
        await db.SaveChangesAsync();

        // ?? Module 18: Emergency Contacts ??????????????????????????????????????
        db.EmergencyContacts.AddRange(
            new() { Id=Guid.NewGuid(), WorkerId=workerEntities[0].Id, Name="Sunita Kumar",  Phone="9876500001", Relation=EmergencyRelation.Spouse,  IsPrimary=true  },
            new() { Id=Guid.NewGuid(), WorkerId=workerEntities[0].Id, Name="Rajiv Kumar",   Phone="9876500002", Relation=EmergencyRelation.Father,  IsPrimary=false },
            new() { Id=Guid.NewGuid(), WorkerId=workerEntities[1].Id, Name="Meena Yadav",   Phone="9876500003", Relation=EmergencyRelation.Mother,  IsPrimary=true  },
            new() { Id=Guid.NewGuid(), WorkerId=workerEntities[5].Id, Name="Razia Karim",   Phone="9876500004", Relation=EmergencyRelation.Spouse,  IsPrimary=true  }
        );
        await db.SaveChangesAsync();

        // ?? Module 19: Testimonials & Worker of the Month ??????????????????????
        db.Testimonials.AddRange(
            new() { Id=Guid.NewGuid(), WorkerId=workerEntities[0].Id, Headline="From ₹300/day to ₹700/day in 6 months!", Story="I was struggling to find consistent plumbing work before ShramSetu. After completing my profile and getting KYC verified, I now earn more than double. The platform connects me with genuine employers.", PhotoUrl="/images/workers/worker1.png", MonthlyEarnings=18000m, IsFeatured=true,  DisplayOrder=1, IsActive=true },
            new() { Id=Guid.NewGuid(), WorkerId=workerEntities[5].Id, Headline="Secured 12-month metro project via ShramSetu", Story="As a certified welder I was always looking for long-term projects. ShramSetu helped me get a 12-month contract with GreenCity Infra. The background check badge helped employers trust me faster.", PhotoUrl="/images/workers/worker6.png", MonthlyEarnings=26000m, IsFeatured=true,  DisplayOrder=2, IsActive=true },
            new() { Id=Guid.NewGuid(), WorkerId=workerEntities[8].Id, Headline="Steady security work in Jaipur",              Story="ShramSetu gave me steady work as a security guard. Earlier I would struggle for 2-3 months without work. Now I always have a booking lined up.", PhotoUrl="/images/workers/worker9.png", MonthlyEarnings=15600m, IsFeatured=false, DisplayOrder=3, IsActive=true }
        );

        db.WorkerOfTheMonths.Add(new WorkerOfTheMonth
        {
            Id = Guid.NewGuid(), WorkerId = workerEntities[0].Id,
            Month = DateTime.UtcNow.Month, Year = DateTime.UtcNow.Year,
            Reason = "Ramesh completed 5 bookings this month with a perfect 5-star rating. Most reliable plumber on the platform.",
            NominatedByUserId = adminUser!.Id, IsActive = true
        });
        await db.SaveChangesAsync();

        // ?? Module 20: Assessment Questions ???????????????????????????????????
        db.AssessmentQuestions.AddRange(
            new() { Id=Guid.NewGuid(), SkillCategoryId=Skill("Plumber").Id,     QuestionText="What type of pipe is best for hot water supply?",           OptionA="PVC", OptionB="CPVC", OptionC="GI",  OptionD="HDPE", CorrectOption="B", Marks=10, IsActive=true },
            new() { Id=Guid.NewGuid(), SkillCategoryId=Skill("Plumber").Id,     QuestionText="Which tool is used to cut GI pipes?",                        OptionA="Pipe wrench", OptionB="Hacksaw", OptionC="Pliers", OptionD="Chisel", CorrectOption="B", Marks=10, IsActive=true },
            new() { Id=Guid.NewGuid(), SkillCategoryId=Skill("Plumber").Id,     QuestionText="What does a P-trap do in plumbing?",                         OptionA="Increases pressure", OptionB="Prevents backflow", OptionC="Blocks odour", OptionD="Both B and C", CorrectOption="D", Marks=10, IsActive=true },
            new() { Id=Guid.NewGuid(), SkillCategoryId=Skill("Electrician").Id, QuestionText="What is the standard colour of the earth wire in India?",    OptionA="Red", OptionB="Black", OptionC="Green", OptionD="Yellow", CorrectOption="C", Marks=10, IsActive=true },
            new() { Id=Guid.NewGuid(), SkillCategoryId=Skill("Electrician").Id, QuestionText="What does MCB stand for?",                                   OptionA="Main Circuit Breaker", OptionB="Miniature Circuit Breaker", OptionC="Motor Control Board", OptionD="Manual Circuit Board", CorrectOption="B", Marks=10, IsActive=true },
            new() { Id=Guid.NewGuid(), SkillCategoryId=Skill("Electrician").Id, QuestionText="Which instrument measures electrical resistance?",           OptionA="Voltmeter", OptionB="Ammeter", OptionC="Ohmmeter", OptionD="Wattmeter", CorrectOption="C", Marks=10, IsActive=true },
            new() { Id=Guid.NewGuid(), SkillCategoryId=Skill("Carpenter").Id,   QuestionText="Which wood is best for furniture making in India?",          OptionA="Pine", OptionB="Teak", OptionC="Bamboo", OptionD="Cedar", CorrectOption="B", Marks=10, IsActive=true },
            new() { Id=Guid.NewGuid(), SkillCategoryId=Skill("Welder").Id,      QuestionText="What gas is used in MIG welding?",                           OptionA="Oxygen", OptionB="Nitrogen", OptionC="Argon/CO2 mix", OptionD="Hydrogen", CorrectOption="C", Marks=10, IsActive=true },
            new() { Id=Guid.NewGuid(), SkillCategoryId=Skill("Driver").Id,      QuestionText="What does a double solid white line on a highway mean?",     OptionA="Overtaking allowed", OptionB="No overtaking from either side", OptionC="One-way traffic", OptionD="Parking zone", CorrectOption="B", Marks=10, IsActive=true },
            new() { Id=Guid.NewGuid(), SkillCategoryId=Skill("Driver").Id,      QuestionText="What is the valid driving licence type to drive a truck?",   OptionA="LMV", OptionB="MCWG", OptionC="HMV", OptionD="TRANS", CorrectOption="C", Marks=10, IsActive=true }
        );
        await db.SaveChangesAsync();

        // ?? Module 21: Minimum Wage Config ????????????????????????????????????
        db.MinimumWageConfigs.AddRange(
            new() { Id=Guid.NewGuid(), State="Maharashtra",  SkillCategoryId=Skill("Plumber").Id,        MinDailyWage=568m,  EffectiveFrom=new DateTime(2024,1,1), Reference="Mah Min Wage Notification 2024" },
            new() { Id=Guid.NewGuid(), State="Maharashtra",  SkillCategoryId=Skill("Electrician").Id,    MinDailyWage=610m,  EffectiveFrom=new DateTime(2024,1,1), Reference="Mah Min Wage Notification 2024" },
            new() { Id=Guid.NewGuid(), State="Delhi",        SkillCategoryId=Skill("Electrician").Id,    MinDailyWage=700m,  EffectiveFrom=new DateTime(2024,4,1), Reference="Delhi Min Wage 2024-25" },
            new() { Id=Guid.NewGuid(), State="Delhi",        SkillCategoryId=Skill("Security Guard").Id, MinDailyWage=625m,  EffectiveFrom=new DateTime(2024,4,1), Reference="Delhi Min Wage 2024-25" },
            new() { Id=Guid.NewGuid(), State="Karnataka",    SkillCategoryId=Skill("Welder").Id,         MinDailyWage=680m,  EffectiveFrom=new DateTime(2024,4,1), Reference="Karnataka Min Wage Apr 2024" },
            new() { Id=Guid.NewGuid(), State="Telangana",    SkillCategoryId=Skill("Labourer").Id,       MinDailyWage=478m,  EffectiveFrom=new DateTime(2024,1,1), Reference="Telangana Min Wage 2024" }
        );
        await db.SaveChangesAsync();

        // ?? Module 22: Onboarding Slides ??????????????????????????????????????
        db.OnboardingSlides.AddRange(
            new() { Id=Guid.NewGuid(), Title="Find Work Near You",          Description="Browse hundreds of verified jobs in your city. Plumber, Electrician, Carpenter, Driver and more.", ImageUrl="/images/onboarding/slide1.png", CtaText="Browse Jobs",      CtaLink="/Jobs/Index",      DisplayOrder=1, IsActive=true },
            new() { Id=Guid.NewGuid(), Title="Get Paid Faster",             Description="Receive your salary directly in your ShramSetu wallet. UPI and bank transfer available instantly.",   ImageUrl="/images/onboarding/slide2.png", CtaText="Set Up Wallet",   CtaLink="/Workers/Wallet",  DisplayOrder=2, IsActive=true },
            new() { Id=Guid.NewGuid(), Title="Build Your Reputation",       Description="Complete your KYC, earn verified badges and get 5-star reviews from employers you've worked with.",    ImageUrl="/images/onboarding/slide3.png", CtaText="Complete Profile",CtaLink="/Workers/Onboarding",DisplayOrder=3,IsActive=true },
            new() { Id=Guid.NewGuid(), Title="Hire Trusted Workers",        Description="All workers on ShramSetu are background-checked and skill-verified. Hire with confidence.",           ImageUrl="/images/onboarding/slide4.png", CtaText="Find Workers",    CtaLink="/Workers/Index",   DisplayOrder=4, IsActive=true }
        );
        await db.SaveChangesAsync();

        // ?? Module 23: Audit Logs ??????????????????????????????????????????????
        db.AuditLogs.AddRange(
            new() { Id=Guid.NewGuid(), UserId=adminUser!.Id,              Action=AuditAction.Created,       EntityType="SkillCategory", EntityId=Guid.NewGuid().ToString(),          NewValues="Seeded 10 skill categories",            OccurredAt=DateTime.UtcNow.AddDays(-61) },
            new() { Id=Guid.NewGuid(), UserId=workerEntities[0].UserId!,  Action=AuditAction.Created,       EntityType="Worker",        EntityId=workerEntities[0].Id.ToString(),    NewValues="Worker profile created",                OccurredAt=DateTime.UtcNow.AddDays(-60) },
            new() { Id=Guid.NewGuid(), UserId=employerEntities[0].UserId, Action=AuditAction.Created,       EntityType="Booking",       EntityId=bookings[0].Id.ToString(),          NewValues="Booking created for Ramesh Kumar",      OccurredAt=DateTime.UtcNow.AddDays(-42) },
            new() { Id=Guid.NewGuid(), UserId=adminUser!.Id,              Action=AuditAction.StatusChanged, EntityType="Worker",        EntityId=workerEntities[0].Id.ToString(),    NewValues="KYC status changed to Verified",        OccurredAt=DateTime.UtcNow.AddDays(-30) },
            new() { Id=Guid.NewGuid(), UserId=employerEntities[1].UserId, Action=AuditAction.Created,       EntityType="JobPost",       EntityId=jobPosts[1].Id.ToString(),          NewValues="Job posted: Electrician for wiring",    OccurredAt=DateTime.UtcNow.AddDays(-7)  },
            new() { Id=Guid.NewGuid(), UserId=workerEntities[1].UserId!,  Action=AuditAction.LoginSuccess,  EntityType="IdentityUser",  EntityId=workerEntities[1].UserId!,          NewValues="Worker logged in from Mumbai",           OccurredAt=DateTime.UtcNow.AddDays(-1)  }
        );
        await db.SaveChangesAsync();

        // ?? Module 24: Saved Workers ???????????????????????????????????????????
        db.SavedWorkers.AddRange(
            new() { Id=Guid.NewGuid(), EmployerId=employerEntities[0].Id, WorkerId=workerEntities[0].Id, SavedAt=DateTime.UtcNow.AddDays(-10) },
            new() { Id=Guid.NewGuid(), EmployerId=employerEntities[0].Id, WorkerId=workerEntities[4].Id, SavedAt=DateTime.UtcNow.AddDays(-8)  },
            new() { Id=Guid.NewGuid(), EmployerId=employerEntities[1].Id, WorkerId=workerEntities[1].Id, SavedAt=DateTime.UtcNow.AddDays(-6)  },
            new() { Id=Guid.NewGuid(), EmployerId=employerEntities[4].Id, WorkerId=workerEntities[5].Id, SavedAt=DateTime.UtcNow.AddDays(-4)  }
        );
        await db.SaveChangesAsync();
    }
}
