using System.Diagnostics;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Serilog;
using System.Text;
using System;
using System.IO;

namespace CSV_Checker
{
    public class Customer
    {
        [Name("First Name")]
        public string FirstName { get; set; }
        [Name("Last Name")]
        public string LastName { get; set; }
        [Name("Street Number")]
        public string StreetNumber { get; set; }
        [Name("Street")]
        public string Street { get; set; }
        [Name("City")]
        public string City { get; set; }
        [Name("Province")]
        public string Province { get; set; }
        [Name("Postal Code")]
        public string PostalCode { get; set; }
        [Name("Country")]
        public string Country { get; set; }
        [Name("Phone Number")]
        public string PhoneNumber { get; set; }
        [Name(name: "email Address")]
        public string emailAddress { get; set; }


        public bool IsValidCustomer(out string validationMessage, string day, string month, string year)
        {
            validationMessage = string.Empty;
            var reasons = new List<string>();

            if (!IsValidFirstName())
                reasons.Add("First Name is invalid");
            if (!IsValidLastName())
                reasons.Add("Last Name is invalid");
            if (!IsValidStreetNumber())
                reasons.Add("Street Number is invalid");
            if (!IsValidStreet())
                reasons.Add("Street is invalid");
            if (!IsValidCity())
                reasons.Add("City is invalid");
            if (!IsValidProvince())
                reasons.Add("Province is invalid");
            if (!IsValidPostalCode())
                reasons.Add("Postal Code is invalid");
            if (!IsValidCountry())
                reasons.Add("Country is invalid");
            if (!IsValidPhoneNumber())
                reasons.Add("Phone Number is invalid");
            if (!IsValidEmailAddress())
                reasons.Add("Email Address is invalid");

            if (reasons.Count > 0)
            {
                var errorLog = new StringBuilder("");

                foreach (var prop in typeof(Customer).GetProperties())
                {
                    var propName = prop.Name;
                    var propValue = prop.GetValue(this);
                    errorLog.Append($"{propName}: {propValue}, ");
                }
                errorLog.Append($"Date: {year}/{month}/{day}, ");
                errorLog.Append("Reason: " + string.Join(", ", reasons));

                validationMessage = errorLog.ToString();
                return false;
            }

            return true;
        }


        public bool IsValidFirstName()
        {
            return !string.IsNullOrWhiteSpace(FirstName) && FirstName.All(char.IsLetter);
        }

        public bool IsValidLastName()
        {
            return string.IsNullOrWhiteSpace(LastName) || LastName.All(char.IsLetter);
        }

        public bool IsValidStreetNumber()
        {
            return !string.IsNullOrWhiteSpace(StreetNumber) && StreetNumber.All(char.IsDigit);
        }

        public bool IsValidStreet()
        {
            return !string.IsNullOrWhiteSpace(Street) && Street.All(c => char.IsLetterOrDigit(c) || c == ' ' || c == '-' || c == ',' || c == '.' || c == '\'');
        }

        public bool IsValidCity()
        {
            return !string.IsNullOrWhiteSpace(City) && City.All(c => char.IsLetter(c) || c == ' ' || c == '-' || c == '.' || c == '\'');
        }

        public bool IsValidProvince()
        {
            return !string.IsNullOrWhiteSpace(Province) && Province.All(c => char.IsLetter(c) || c == ' ');
        }

        public bool IsValidPostalCode()
        {
            var cleanedPostalCode = PostalCode?.Replace(" ", "");
            string postalCodePattern = @"^[A-Z]\d[A-Z]\d[A-Z]\d$";
            return !string.IsNullOrWhiteSpace(cleanedPostalCode) && System.Text.RegularExpressions.Regex.IsMatch(cleanedPostalCode, postalCodePattern);
        }

        public bool IsValidCountry()
        {
            return !string.IsNullOrWhiteSpace(Country) && Country.All(char.IsLetter);
        }

        public bool IsValidPhoneNumber()
        {
            return !string.IsNullOrWhiteSpace(PhoneNumber) && PhoneNumber.All(c => char.IsDigit(c) || c == '-' || c == ',' || c == ' ' || c == '(' || c == ')');
        }

        public bool IsValidEmailAddress()
        {
            if (string.IsNullOrWhiteSpace(emailAddress))
            {
                return false;
            }
            string emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
            return System.Text.RegularExpressions.Regex.IsMatch(emailAddress, emailPattern);
        }

    }

    class Program
    {
        static void Main(string[] args)
        {
            int Valid_Row_Count = 0;
            int Skipped_Row_Count = 0;
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            Console.Write("Enter the CSV directory path: ");
            string CSV_Directory = Console.ReadLine();

            while (string.IsNullOrWhiteSpace(CSV_Directory) || !Directory.Exists(CSV_Directory))
            {
                Console.Write("Enter a valid CSV directory path: ");
                CSV_Directory = Console.ReadLine();
            }
                string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string targetDirectory = Path.Combine(currentDirectory, "..", "..", "..", "..");

                string outputFolder = Path.Combine(targetDirectory, "Output");
                Directory.CreateDirectory(outputFolder);

                string logsFolder = Path.Combine(targetDirectory, "Logs");
                Directory.CreateDirectory(logsFolder);

                string OutputFilePath = Path.Combine(outputFolder, "all_valid_customers.csv");

                Log.Logger = new LoggerConfiguration()
                    .WriteTo.File(
                    Path.Combine(logsFolder, "log.txt"),
                    outputTemplate: "{Message:lj}{NewLine}{Exception}"
                    )
                    .CreateLogger();


                var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    Delimiter = ",",
                    HasHeaderRecord = true
                };

                using (var outputWriter = new StreamWriter(OutputFilePath))
                using (var csvOutput = new CsvWriter(outputWriter, configuration))
                {
                    csvOutput.WriteHeader<Customer>();
                    csvOutput.WriteField("Date");
                    csvOutput.NextRecord();
                }

                foreach (var csvFile in Directory.GetFiles(CSV_Directory, "*.csv", SearchOption.AllDirectories))
                {
                    var csvFileInfo = new FileInfo(csvFile);
                    var dayFolder = csvFileInfo.Directory;
                    var monthFolder = dayFolder.Parent;
                    var yearFolder = monthFolder.Parent;
                    var year = yearFolder.Name;
                    var month = monthFolder.Name;
                    var day = dayFolder.Name;

                    using (var reader = new StreamReader(csvFile))
                    using (var csv = new CsvReader(reader, configuration))
                    {
                        while (csv.Read())
                        {
                            try
                            {
                                var record = csv.GetRecord<Customer>();
                                string validationMessage = string.Empty;

                                if (record != null && record.IsValidCustomer(out validationMessage, day, month, year))
                                {
                                    Valid_Row_Count++;
                                    using (var outputWriter = new StreamWriter(OutputFilePath, true))
                                    using (var csvOutput = new CsvWriter(outputWriter, configuration))
                                    {

                                        csvOutput.WriteRecord(record);
                                        csvOutput.WriteField($"{year}/{month}/{day}");

                                        csvOutput.NextRecord();
                                    }
                                }
                                else
                                {
                                    Skipped_Row_Count++;
                                    Log.Error(validationMessage);
                                }
                            }
                            catch (CsvHelperException ex)
                            {
                                Skipped_Row_Count++;
                                Log.Error($"Error reading CSV: {ex.Message}");
                            }
                        }
                    }
                }
         
            stopwatch.Stop();
            TimeSpan executionTime = stopwatch.Elapsed;

            Log.Information($"Total Execution Time: {executionTime}");
            Log.Information($"Total Valid Rows: {Valid_Row_Count}");
            Log.Information($"Total Skipped Rows: {Skipped_Row_Count}");
            Log.CloseAndFlush();
        }

    }

}