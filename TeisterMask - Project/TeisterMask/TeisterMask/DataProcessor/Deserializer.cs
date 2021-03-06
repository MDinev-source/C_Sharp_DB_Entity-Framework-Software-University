﻿namespace TeisterMask.DataProcessor
{
    using Data;
    using System;
    using System.Xml.Serialization;
    using System.Collections.Generic;
    using TeisterMask.DataProcessor.ImportDto;
    using System.ComponentModel.DataAnnotations;
    using ValidationContext = System.ComponentModel.DataAnnotations.ValidationContext;
    using System.IO;
    using TeisterMask.Data.Models;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using TeisterMask.Data.Models.Enums;
    using Newtonsoft.Json;

    public class Deserializer
    {
        private const string ErrorMessage = "Invalid data!";

        private const string SuccessfullyImportedProject
            = "Successfully imported project - {0} with {1} tasks.";

        private const string SuccessfullyImportedEmployee
            = "Successfully imported employee - {0} with {1} tasks.";

        public static string ImportProjects(TeisterMaskContext context, string xmlString)
        {
            var xmlSerializer = new XmlSerializer(typeof(ImportProjectsDto[]), new XmlRootAttribute("Projects"));
            var importProjects = (ImportProjectsDto[])xmlSerializer.Deserialize(new StringReader(xmlString));

            var projects = new List<Project>();

            var sb = new StringBuilder();

            foreach (var importProjectDto in importProjects)
            {
                var projectOpenDate = DateTime.ParseExact(importProjectDto.OpenDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);

                var projectDueDate = string.IsNullOrEmpty(importProjectDto.DueDate)
                    ? (DateTime?)null
                    : DateTime.ParseExact(importProjectDto.DueDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);

                if (!IsValid(importProjectDto))
                {
                    sb.AppendLine(ErrorMessage);
                    continue;
                }

                var project = new Project
                {
                    Name = importProjectDto.Name,
                    OpenDate = projectOpenDate,
                    DueDate = projectDueDate
                };

                foreach (var importTask in importProjectDto.Tasks)
                {
                    if (!IsValid(importTask))
                    {
                        sb.AppendLine(ErrorMessage);
                        continue;
                    }

                    var taskOpenDate = DateTime.ParseExact(importTask.OpenDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                    var taskDueDate = DateTime.ParseExact(importTask.DueDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);

                    var taskExecutionType = Enum.TryParse<ExecutionType>(importTask.ExecutionType, out ExecutionType executionType);
                    var taskLabelType = Enum.TryParse<LabelType>(importTask.LabelType, out LabelType labelType);

                    if (!taskExecutionType ||
                        !taskLabelType ||
                        taskOpenDate < project.OpenDate ||
                        taskDueDate > project.DueDate)
                    {
                        sb.AppendLine(ErrorMessage);
                        continue;
                    }

                    var task = new Task
                    {
                        Name = importTask.Name,
                        OpenDate = taskOpenDate,
                        DueDate = taskDueDate,
                        ExecutionType = executionType,
                        LabelType = labelType
                    };

                    project.Tasks.Add(task);
                }

                projects.Add(project);

                sb.AppendLine(string.Format(SuccessfullyImportedProject
                    , project.Name
                    , project.Tasks.Count));
            }

            context.Projects.AddRange(projects);
            context.SaveChanges();

            return sb.ToString().TrimEnd();
        }

        public static string ImportEmployees(TeisterMaskContext context, string jsonString)
        {
            var importEmployees = JsonConvert.DeserializeObject<ImportEmployeesDto[]>(jsonString);

            var employees = new List<Employee>();

            var sb = new StringBuilder();

            foreach (var employeeDto in importEmployees)
            {

                if (!IsValid(employeeDto))
                {
                    sb.AppendLine(ErrorMessage);
                    continue;
                }

                var employee = new Employee
                {
                    Username = employeeDto.Username,
                    Email = employeeDto.Email,
                    Phone = employeeDto.Phone
                };

                foreach (var taskId in employeeDto.Tasks.Distinct())
                {
                    if (!context.Tasks.Any(x => x.Id == taskId))
                    {
                        sb.AppendLine(ErrorMessage);
                        continue;
                    }

                    employee.EmployeesTasks.Add(new EmployeeTask
                    {
                        TaskId = taskId
                    });
                }

                employees.Add(employee);

                sb.AppendLine(string.Format(SuccessfullyImportedEmployee, employee.Username, employee.EmployeesTasks.Count));
            }

            context.Employees.AddRange(employees);
            context.SaveChanges();

            return sb.ToString().TrimEnd();
        }

        private static bool IsValid(object dto)
        {
            var validationContext = new ValidationContext(dto);
            var validationResult = new List<ValidationResult>();

            return Validator.TryValidateObject(dto, validationContext, validationResult, true);
        }
    }
}