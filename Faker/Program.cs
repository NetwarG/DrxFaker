﻿using Bogus;
using Bogus.DataSets;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Npgsql;
using CommandLine;
using System.Reflection;

namespace DrxFaker
{
    class Program
    {
        static string postgresConnStr = "Server=192.168.3.237;Port=5432;Database=directum;Uid=directum;Pwd=1Qwerty";
        static string msConnStr = "Server=192.168.3.237;Initial Catalog=directum;User Id=directum;Password=1Qwerty";
        static string connectionString;

        static void Main()
        {
            bool keepLooping = true;
            while (keepLooping)
            {
                var args = Console.ReadLine().Split();
                var parser = Parser.Default.ParseArguments<Options>(args)
                    .WithParsed(RunOptions)
                    .WithNotParsed(HandleParseError);

                if (Console.ReadKey().Key == ConsoleKey.Escape)
                    keepLooping = false;
            }

            //var employeesCount = 5;
            //var businessCount = 2;
            //var departmentsCount = 3;

            //try
            //{
            //    var persons = GeneratePersons(employeesCount);
            //    if (persons == null)
            //    {
            //        Console.WriteLine("Error while creating Persons");
            //        return;
            //    }

            //    var loginsIds = GenerateLogins(employeesCount, persons);
            //    if (loginsIds == null)
            //    {
            //        Console.WriteLine("Error while creating Logins");
            //        return;
            //    }

            //    var businesUnitsIds = GenerateBusinessUnits(businessCount);
            //    if (businesUnitsIds == null)
            //    {
            //        Console.WriteLine("Error while creating Business Units");
            //        return;
            //    }

            //    var departmentsIds = GenerateDepartments(departmentsCount, businesUnitsIds);
            //    if (departmentsIds == null)
            //    {
            //        Console.WriteLine("Error while creating Departments");
            //        return;
            //    }

            //    GenerateEmployees(employeesCount, persons, loginsIds, departmentsIds);
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine($"\nError: {ex.Message}");
            //}
        }

        static void RunOptions(Options opts)
        {
            var types = opts.GetType().GetProperties();
            foreach (var type in types)
                Console.WriteLine(type.GetValue(opts));
        }

        static void HandleParseError(IEnumerable<Error> errs)
        {
            //Console.WriteLine("Enter all required attributes.");
        }

        #region Создание записей
        static List<Person> GeneratePersons(int count)
        {
            var tableName = "sungero_parties_counterparty";
            int maxId = GetMaxId(tableName);
            if (maxId == 0)
                return null;

            var genders = new List<Name.Gender>() { Name.Gender.Male, Name.Gender.Female };

            var newPersons = new Faker<Person>("ru")
            .RuleFor(u => u.Id, (f, u) => f.IndexFaker + maxId)
            .RuleFor(u => u.Phone, (f, u) => "+7" + f.Phone.PhoneNumber().ToString())
            .RuleFor(u => u.Code, (f, u) => f.Random.Number(10, 100000))
            .RuleFor(u => u.Sex, f => f.PickRandom(genders))
            .RuleFor(u => u.Lastname, (f, u) => f.Name.LastName(u.Sex))
            .RuleFor(u => u.Firstname, (f, u) => f.Name.FirstName(u.Sex))
            .RuleFor(u => u.Name, (f, u) => u.Lastname + " " + u.Firstname)
            .RuleFor(u => u.Dateofbirth, (f, u) => f.Date.Between(DateTime.Today.AddYears(-60), DateTime.Today.AddYears(-18)))
            .RuleFor(u => u.Shortname, (f, u) => u.Lastname + " " + u.Firstname.Substring(0, 1) + " ")
            .RuleFor(u => u.Login, (f, u) => f.Internet.UserName(u.Firstname, u.Lastname))
            .RuleFor(u => u.Email, (f, u) => f.Internet.Email(u.Firstname, u.Lastname));

            var query = $"INSERT INTO {tableName}" +
            "(Id, discriminator, status, name, phones, code, lastname, firstname, dateofbirth, sex, shortname)" +
            "VALUES ";

            var persons = newPersons.Generate(count);

            foreach (var person in persons)
            {
                query += $"\n({person.Id}, '{person.Discriminator}', '{person.Status}', '{person.Name}', '{person.Phone}', "
                    + $"'{person.Code}', '{person.Lastname}', '{person.Firstname}', '{person.Dateofbirth}', '{person.Sex}', '{person.Shortname}'),";
            }

            query = query.Substring(0, query.Length - 1);

            if (InsertData(query))
                Console.WriteLine($"Success creating {count} Persons");
            else
                return null;

            return persons;
        }

        static List<int> GenerateLogins(int count, List<Person> persons)
        {
            var tableName = "sungero_core_login";
            int maxId = GetMaxId(tableName);
            if (maxId == 0)
                return null;

            var newLogin = new Faker<Login>("ru")
            .RuleFor(u => u.Id, (f, u) => f.IndexFaker + maxId);

            var query = $"INSERT INTO {tableName}" +
            "(Id, discriminator, status, typeauthentication, loginname)" +
            "VALUES ";

            var logins = newLogin.Generate(count);

            for (var i = 0; i < logins.Count(); i++)
            {
                var login = logins[i];
                query += $"\n({login.Id}, '{login.Discriminator}', '{login.Status}', '{login.TypeAuthentication}', '{persons[i].Login}'),";
            }

            query = query.Substring(0, query.Length - 1);

            if (InsertData(query))
                Console.WriteLine($"Success creating {count} Logins");
            else
                return null;

            return logins.Select(_ => _.Id).ToList();
        }

        static List<int> GenerateBusinessUnits(int count)
        {
            var tableName = "sungero_core_recipient";
            int maxId = GetMaxId(tableName);
            if (maxId == 0)
                return null;

            var newBusinessUnit = new Faker<BusinessUnit>("ru")
            .RuleFor(u => u.Id, (f, u) => f.IndexFaker + maxId)
            .RuleFor(u => u.Sid, (f, u) => f.Random.Uuid().ToString())
            .RuleFor(u => u.Name, (f, u) => f.Company.CompanyName())
            .RuleFor(u => u.LegalName, (f, u) => u.Name)
            .RuleFor(u => u.Code, (f, u) => f.Random.Number(10, 100000))
            .RuleFor(u => u.Phone, (f, u) => "+7" + f.Phone.PhoneNumber().ToString())
            .RuleFor(u => u.TIN, (f, u) => f.Random.Number(10, 100000).ToString())
            .RuleFor(u => u.TRRC, (f, u) => f.Random.Number(10, 100000).ToString())
            .RuleFor(u => u.PSRN, (f, u) => f.Random.Number(10, 100000).ToString());

            var query = $"INSERT INTO {tableName}" +
                "(Id, sid, discriminator, status, name, legalname_company_sungero, code_company_sungero, phone_company_sungero, "
                + "tin_company_sungero, trrc_company_sungero, psrn_company_sungero)" +
                "VALUES ";

            var businessUnits = newBusinessUnit.Generate(count);

            foreach (var businessUnit in businessUnits)
                query += $"\n({businessUnit.Id}, '{businessUnit.Sid}', '{businessUnit.Discriminator}', '{businessUnit.Status}', '{businessUnit.Name}', "
                    + $"'{businessUnit.LegalName}', '{businessUnit.Code}', '{businessUnit.Phone}', '{businessUnit.TIN}', "
                    + $"'{businessUnit.TRRC}', '{businessUnit.PSRN}'),";

            query = query.Substring(0, query.Length - 1);

            if (InsertData(query))
                Console.WriteLine($"Success creating {count} Business Units");
            else
                return null;

            return businessUnits.Select(_ => _.Id).ToList();
        }

        static List<int> GenerateDepartments(int count, List<int> businessUnitIds)
        {
            var tableName = "sungero_core_recipient";
            int maxId = GetMaxId(tableName);
            if (maxId == 0)
                return null;

            var newDepartment = new Faker<Department>("ru")
            .RuleFor(u => u.Id, (f, u) => f.IndexFaker + maxId)
            .RuleFor(u => u.Sid, (f, u) => f.Random.Uuid().ToString())
            .RuleFor(u => u.Name, (f, u) => f.Commerce.Department())
            .RuleFor(u => u.Code, (f, u) => f.Random.Number(10, 100000))
            .RuleFor(u => u.Phone, (f, u) => "+7" + f.Phone.PhoneNumber().ToString())
            .RuleFor(u => u.BusinessUnitId, (f, u) => f.PickRandom(businessUnitIds));

            var query = $"INSERT INTO {tableName}" +
                "(Id, sid, discriminator, status, name, code_company_sungero, phone_company_sungero, businessunit_company_sungero)" +
                "VALUES ";

            var departments = newDepartment.Generate(count);

            foreach (var department in departments)
                query += $"\n({department.Id}, '{department.Sid}', '{department.Discriminator}', '{department.Status}', '{department.Name}', "
                    + $"'{department.Code}', '{department.Phone}', '{department.BusinessUnitId}'),";

            query = query.Substring(0, query.Length - 1);

            if (InsertData(query))
                Console.WriteLine($"Success creating {count} Departments");
            else
                return null;

            return departments.Select(_ => _.Id).ToList();
        }

        static void GenerateEmployees(int count, List<Person> persons, List<int> loginsIds, List<int> departmentsIds)
        {
            var tableName = "sungero_core_recipient";
            int maxId = GetMaxId(tableName);
            if (maxId == 0)
                return;

            var newEmployee = new Faker<Employee>("ru")
            .RuleFor(u => u.Id, (f, u) => f.IndexFaker + maxId)
            .RuleFor(u => u.Sid, (f, u) => f.Random.Uuid().ToString())
            .RuleFor(u => u.TabNumber, (f, u) => f.Random.Number(10, 100000))
            .RuleFor(u => u.DepartmentId, (f, u) => f.PickRandom(departmentsIds));

            var query = $"INSERT INTO {tableName}" +
                "(Id, sid, discriminator, status, name, person_company_sungero, login, department_company_sungero, persnumber_company_sungero, email_company_sungero)" +
                "VALUES ";

            var employees = newEmployee.Generate(count).ToList();

            for (var i = 0; i < employees.Count(); i++)
            {
                var employee = employees[i];
                query += $"\n({employee.Id}, '{employee.Sid}', '{employee.Discriminator}', '{employee.Status}', '{persons[i].Name}', "
                    + $"'{persons[i].Id}', '{loginsIds[i]}', '{employee.DepartmentId}', '{employee.TabNumber}', '{persons[i].Email}'),";
            }

            query = query.Substring(0, query.Length - 1);

            if (InsertData(query))
                Console.WriteLine($"Success creating {count} Employees");
            else
                return;
        }
        #endregion

        #region Полезный функционал
        static int GetMaxId(string TableName)
        {
            int maxId = 0;
            using (var connection = new NpgsqlConnection(connectionString))
            {
                var command = new NpgsqlCommand($"select Max(Id) from {TableName}", connection);
                connection.Open();
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    if (!int.TryParse(reader[0].ToString(), out maxId))
                        return 0;
                }
                connection.Close();
            }

            return maxId + 1;
        }

        static bool InsertData(string query)
        {
            var result = false;

            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    var command = new NpgsqlCommand(query, connection);
                    connection.Open();
                    command.ExecuteNonQuery();
                    connection.Close();
                }

                result = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                result = false;
            }

            return result;
        }
        #endregion
    }
}
