//using Microsoft.AspNetCore.Identity;
//using risk.control.system.Data;
//using risk.control.system.Helpers;
//using risk.control.system.Models;
//using static risk.control.system.Helpers.Permissions;
//using System.Security.Claims;

//namespace risk.control.system.Seeds
//{
//    public static class CountryStatePincodeUserSeed
//    {
//        public static Country India { get; set; }
//        public static State UP { get; set; }
//        public static State ONTARIO { get; set; }
//        public static State DELHI { get; set; }
//        public static State VICTORIA { get; set; }
//        public static State TASMANIA { get; set; }
//        public static State NEWDELHI { get; set; }
//        public static PinCode NorthDelhi { get; set; }
//        public static PinCode Indirapuram { get; set; }
//        public static PinCode Bhelupur { get; set; }
//        public static PinCode ForestHill { get; set; }
//        public static PinCode Vermont { get; set; }
//        public static PinCode TasmaniaCity { get; set; }
//        public static PinCode Toronto { get; set; }
//        public static Country Australia { get; set; }
//        public static Country Canada { get; set; }
//        public static async Task Seed(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
//        {
//            #region COUNTRY STATE PINCODE
//            var india = new Country
//            {
//                Name = "INDIA",
//                Code = "IND",
//            };
//            var indiaCountry = await context.Country.AddAsync(india);
//            India = indiaCountry.Entity;

//            var australia = new Country
//            {
//                Name = "AUSTRALIA",
//                Code = "AUS",
//            };

//            var australiaCountry = await context.Country.AddAsync(australia);
//            Australia = australiaCountry.Entity;

//            var canada = new Country
//            {
//                Name = "CANADA",
//                Code = "CAN",
//            };

//            var canadaCountry = await context.Country.AddAsync(canada);
//            Canada = canadaCountry.Entity;
//            var up = new State
//            {
//                CountryId = indiaCountry.Entity.CountryId,
//                Name = "UTTAR PRADESH",
//                Code = "UP"
//            };

//            var upState = await context.State.AddAsync(up);
//            UP = upState.Entity;

//            var ontario = new State
//            {
//                CountryId = canadaCountry.Entity.CountryId,
//                Name = "ONTARIO",
//                Code = "ON"
//            };

//            var ontarioState = await context.State.AddAsync(ontario);
//            ONTARIO = ontarioState.Entity;

//            var delhi = new State
//            {
//                CountryId = indiaCountry.Entity.CountryId,
//                Name = "NEW DELHI",
//                Code = "NDL"
//            };

//            var delhiState = await context.State.AddAsync(delhi);
//            DELHI = delhiState.Entity;

//            var victoria = new State
//            {
//                CountryId = australiaCountry.Entity.CountryId,
//                Name = "VICTORIA",
//                Code = "VIC"
//            };

//            var victoriaState = await context.State.AddAsync(victoria);
//            VICTORIA = victoriaState.Entity;

//            var tasmania = new State
//            {
//                CountryId = australiaCountry.Entity.CountryId,
//                Name = "TASMANIA",
//                Code = "TAS"
//            };

//            var tasmaniaState = await context.State.AddAsync(tasmania);
//            TASMANIA = tasmaniaState.Entity;

//            var newDelhi = new PinCode
//            {
//                Name = "NEW DELHI",
//                District = "110001",
//                State = delhiState.Entity,
//                Country = indiaCountry.Entity
//            };

//            var newDelhiPinCode = await context.PinCode.AddAsync(newDelhi);

//            var northDelhi = new PinCode
//            {
//                Name = "NORTH DELHI",
//                District = "110002",
//                State = delhiState.Entity,
//                Country = indiaCountry.Entity
//            };

//            var northDelhiPinCode = await context.PinCode.AddAsync(northDelhi);
//            NorthDelhi = northDelhiPinCode.Entity;

//            var indirapuram = new PinCode
//            {
//                Name = "INDIRAPURAM",
//                District = "201014",
//                State = upState.Entity,
//                Country = indiaCountry.Entity
//            };

//            var indiraPuramPinCode = await context.PinCode.AddAsync(indirapuram);
//            Indirapuram = indiraPuramPinCode.Entity;

//            var bhelupur = new PinCode
//            {
//                Name = "BHELUPUR",
//                District = "221001",
//                State = upState.Entity,
//                Country = indiaCountry.Entity
//            };

//            var bhelupurPinCode = await context.PinCode.AddAsync(bhelupur);
//            Bhelupur = bhelupurPinCode.Entity;

//            var forestHill = new PinCode
//            {
//                Name = "FOREST HILL",
//                District = "3131",
//                State = victoriaState.Entity,
//                Country = australiaCountry.Entity
//            };

//            var forestHillPinCode = await context.PinCode.AddAsync(forestHill);
//            ForestHill = forestHillPinCode.Entity;

//            var vermont = new PinCode
//            {
//                Name = "VERMONT",
//                District = "3133",
//                State = victoriaState.Entity,
//                Country = australiaCountry.Entity
//            };

//            var vermontPinCode = await context.PinCode.AddAsync(vermont);
//            Vermont = vermontPinCode.Entity;

//            var tasmaniaCity = new PinCode
//            {
//                Name = "TASMANIA CITY",
//                District = "7000",
//                State = tasmaniaState.Entity,
//                Country = australiaCountry.Entity
//            };

//            var tasmaniaCityCode = await context.PinCode.AddAsync(tasmaniaCity);
//            TasmaniaCity = tasmaniaCityCode.Entity;

//            var torontoCity = new PinCode
//            {
//                Name = "TORONTO",
//                District = "9101",
//                State = ontarioState.Entity,
//                Country = canadaCountry.Entity
//            };

//            var torontoCityCode = await context.PinCode.AddAsync(tasmaniaCity);
//            Toronto = torontoCityCode.Entity;

//            #endregion
//            #region APPLICATION USERS ROLES

//            //Seed portal admin
//            var portalAdmin = new ApplicationUser()
//            {
//                UserName = "portal-admin@admin.com",
//                Email = "portal-admin@admin.com",
//                FirstName = "Portal",
//                LastName = "Admin",
//                Password = Applicationsettings.Password,
//                EmailConfirmed = true,
//                PhoneNumberConfirmed = true,
//                State = upState.Entity,
//                Country = indiaCountry.Entity,
//                PinCode = indiraPuramPinCode.Entity,
//                ProfilePictureUrl = "img/superadmin.jpg"
//            };
//            if (userManager.Users.All(u => u.Id != portalAdmin.Id))
//            {
//                var user = await userManager.FindByEmailAsync(portalAdmin.Email);
//                if (user == null)
//                {
//                    await userManager.CreateAsync(portalAdmin, Applicationsettings.Password);
//                    await userManager.AddToRoleAsync(portalAdmin, AppRoles.PortalAdmin.ToString());
//                    await userManager.AddToRoleAsync(portalAdmin, AppRoles.ClientAdmin.ToString());
//                    await userManager.AddToRoleAsync(portalAdmin, AppRoles.ClientCreator.ToString());
//                    await userManager.AddToRoleAsync(portalAdmin, AppRoles.ClientAssigner.ToString());
//                    await userManager.AddToRoleAsync(portalAdmin, AppRoles.ClientAssessor.ToString());
//                    await userManager.AddToRoleAsync(portalAdmin, AppRoles.VendorAdmin.ToString());
//                    await userManager.AddToRoleAsync(portalAdmin, AppRoles.VendorSupervisor.ToString());
//                    await userManager.AddToRoleAsync(portalAdmin, AppRoles.VendorAgent.ToString());
//                }

//                ////////PERMISSIONS TO MODULES

//                var adminRole = await roleManager.FindByNameAsync(AppRoles.PortalAdmin.ToString());
//                var allClaims = await roleManager.GetClaimsAsync(adminRole);

//                //ADD PERMISSIONS

//                //var allPermissions = Permissions.GeneratePermissionsForModule(nameof(Permissions.Products));
//                //foreach (var permission in allPermissions)
//                //{
//                //    if (!allClaims.Any(a => a.Type == Applicationsettings.PERMISSION && a.Value == permission))
//                //    {
//                //        await roleManager.AddClaimAsync(adminRole, new Claim(Applicationsettings.PERMISSION, permission));
//                //    }
//                //}

//                var moduleList = new List<string> { nameof(Products), nameof(CaseClaims) };

//                foreach (var module in moduleList)
//                {
//                    var modulePermissions = Permissions.GeneratePermissionsForModule(module);

//                    foreach (var modulePermission in modulePermissions)
//                    {
//                        if (!allClaims.Any(a => a.Type == Applicationsettings.PERMISSION && a.Value == modulePermission))
//                        {
//                            await roleManager.AddClaimAsync(adminRole, new Claim(Applicationsettings.PERMISSION, modulePermission));
//                        }
//                    }
//                }
//            }

//            //Seed client admin
//            var clientAdmin = new ApplicationUser()
//            {
//                UserName = "client-admin@admin.com",
//                Email = "client-admin@admin.com",
//                FirstName = "Client",
//                LastName = "Admin",
//                EmailConfirmed = true,
//                PhoneNumberConfirmed = true,
//                Password = Applicationsettings.Password,
//                isSuperAdmin = true,
//                State = ontarioState.Entity,
//                Country = canadaCountry.Entity,
//                PinCode = torontoCityCode.Entity,
//                ProfilePictureUrl = "img/admin.png"
//            };
//            if (userManager.Users.All(u => u.Id != clientAdmin.Id))
//            {
//                var user = await userManager.FindByEmailAsync(clientAdmin.Email);
//                if (user == null)
//                {
//                    await userManager.CreateAsync(clientAdmin, Applicationsettings.Password);
//                    await userManager.AddToRoleAsync(clientAdmin, AppRoles.ClientAdmin.ToString());
//                    await userManager.AddToRoleAsync(clientAdmin, AppRoles.ClientCreator.ToString());
//                    await userManager.AddToRoleAsync(clientAdmin, AppRoles.ClientAssigner.ToString());
//                    await userManager.AddToRoleAsync(clientAdmin, AppRoles.ClientAssessor.ToString());
//                    await userManager.AddToRoleAsync(clientAdmin, AppRoles.VendorAdmin.ToString());
//                    await userManager.AddToRoleAsync(clientAdmin, AppRoles.VendorSupervisor.ToString());
//                    await userManager.AddToRoleAsync(clientAdmin, AppRoles.VendorAgent.ToString());
//                }
//            }

//            //Seed client creator
//            var clientCreator = new ApplicationUser()
//            {
//                UserName = "client-creator@admin.com",
//                Email = "client-creator@admin.com",
//                FirstName = "Client",
//                LastName = "Creator",
//                EmailConfirmed = true,
//                Password = Applicationsettings.Password,
//                PhoneNumberConfirmed = true,
//                isSuperAdmin = true,
//                State = upState.Entity,
//                Country = indiaCountry.Entity,
//                PinCode = indiraPuramPinCode.Entity,
//                ProfilePictureUrl = "img/creator.jpg"
//            };
//            if (userManager.Users.All(u => u.Id != clientCreator.Id))
//            {
//                var user = await userManager.FindByEmailAsync(clientCreator.Email);
//                if (user == null)
//                {
//                    await userManager.CreateAsync(clientCreator, Applicationsettings.Password);
//                    await userManager.AddToRoleAsync(clientCreator, AppRoles.ClientCreator.ToString());
//                }
//            }

//            //Seed client assigner
//            var clientAssigner = new ApplicationUser()
//            {
//                UserName = "client-assigner@admin.com",
//                Email = "client-assigner@admin.com",
//                FirstName = "Client",
//                LastName = "Assigner",
//                EmailConfirmed = true,
//                PhoneNumberConfirmed = true,
//                Password = Applicationsettings.Password,
//                isSuperAdmin = true,
//                PinCode = northDelhiPinCode.Entity,
//                State = delhiState.Entity,
//                Country = indiaCountry.Entity,
//                ProfilePictureUrl = "img/assigner.png"
//            };
//            if (userManager.Users.All(u => u.Id != clientAssigner.Id))
//            {
//                var user = await userManager.FindByEmailAsync(clientAssigner.Email);
//                if (user == null)
//                {
//                    await userManager.CreateAsync(clientAssigner, Applicationsettings.Password);
//                    await userManager.AddToRoleAsync(clientAssigner, AppRoles.ClientAssigner.ToString());
//                }
//            }

//            //Seed client assessor
//            var clientAssessor = new ApplicationUser()
//            {
//                UserName = "client-assessor@admin.com",
//                Email = "client-assessor@admin.com",
//                FirstName = "Client",
//                LastName = "Assessor",
//                EmailConfirmed = true,
//                PhoneNumberConfirmed = true,
//                Password = Applicationsettings.Password,
//                isSuperAdmin = true,
//                PinCode = northDelhiPinCode.Entity,
//                State = delhiState.Entity,
//                Country = indiaCountry.Entity,
//                ProfilePictureUrl = "img/assessor.png"
//            };
//            if (userManager.Users.All(u => u.Id != clientAssessor.Id))
//            {
//                var user = await userManager.FindByEmailAsync(clientAssessor.Email);
//                if (user == null)
//                {
//                    await userManager.CreateAsync(clientAssessor, Applicationsettings.Password);
//                    await userManager.AddToRoleAsync(clientAssessor, AppRoles.ClientAssessor.ToString());
//                }
//            }

//            //Seed Vendor Admin
//            var vendorAdmin = new ApplicationUser()
//            {
//                UserName = "vendor-admin@admin.com",
//                Email = "vendor-admin@admin.com",
//                FirstName = "Vendor",
//                LastName = "Admin",
//                EmailConfirmed = true,
//                PhoneNumberConfirmed = true,
//                Password = Applicationsettings.Password,
//                isSuperAdmin = true,
//                PinCode = indiraPuramPinCode.Entity,
//                State = upState.Entity,
//                Country = indiaCountry.Entity,
//                ProfilePictureUrl = "img/vendor-admin.png"
//            };
//            if (userManager.Users.All(u => u.Id != vendorAdmin.Id))
//            {
//                var user = await userManager.FindByEmailAsync(vendorAdmin.Email);
//                if (user == null)
//                {
//                    await userManager.CreateAsync(vendorAdmin, Applicationsettings.Password);
//                    await userManager.AddToRoleAsync(vendorAdmin, AppRoles.VendorAdmin.ToString());
//                    await userManager.AddToRoleAsync(vendorAdmin, AppRoles.VendorSupervisor.ToString());
//                    await userManager.AddToRoleAsync(vendorAdmin, AppRoles.VendorAgent.ToString());
//                }
//            }

//            //Seed Vendor Admin
//            var vendorSupervisor = new ApplicationUser()
//            {
//                UserName = "vendor-supervisor@admin.com",
//                Email = "vendor-supervisor@admin.com",
//                FirstName = "Vendor",
//                LastName = "Supervisor",
//                EmailConfirmed = true,
//                PhoneNumberConfirmed = true,
//                Password = Applicationsettings.Password,
//                isSuperAdmin = true,
//                PinCode = indiraPuramPinCode.Entity,
//                State = upState.Entity,
//                Country = indiaCountry.Entity,
//                ProfilePictureUrl = "img/supervisor.png"
//            };
//            if (userManager.Users.All(u => u.Id != vendorSupervisor.Id))
//            {
//                var user = await userManager.FindByEmailAsync(vendorSupervisor.Email);
//                if (user == null)
//                {
//                    await userManager.CreateAsync(vendorSupervisor, Applicationsettings.Password);
//                    await userManager.AddToRoleAsync(vendorSupervisor, AppRoles.VendorSupervisor.ToString());
//                    await userManager.AddToRoleAsync(vendorSupervisor, AppRoles.VendorAgent.ToString());
//                }
//            }

//            //Seed Vendor Admin
//            var vendorAgent = new ApplicationUser()
//            {
//                UserName = "vendor-agent@admin.com",
//                Email = "vendor-agent@admin.com",
//                FirstName = "Vendor",
//                LastName = "Agent",
//                EmailConfirmed = true,
//                PhoneNumberConfirmed = true,
//                Password = Applicationsettings.Password,
//                isSuperAdmin = true,
//                PinCode = indiraPuramPinCode.Entity,
//                State = upState.Entity,
//                Country = indiaCountry.Entity,
//                ProfilePictureUrl = "img/agent.jpg"
//            };
//            if (userManager.Users.All(u => u.Id != vendorAgent.Id))
//            {
//                var user = await userManager.FindByEmailAsync(vendorAgent.Email);
//                if (user == null)
//                {
//                    await userManager.CreateAsync(vendorAgent, Applicationsettings.Password);
//                    await userManager.AddToRoleAsync(vendorAgent, AppRoles.VendorAgent.ToString());
//                }
//            }
//            #endregion
//        }
//    }
//}
