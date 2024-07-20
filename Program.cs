using Backend.Database;
using Backend.Enums;
using Backend.Utils;
using Microsoft.Extensions.Configuration;

namespace Pipeline
{
    public class Program
    {
        public static async Task Main()
        {
            Console.WriteLine("Start");
            Sys.LoadAllEmbeddedDll();

            #region Innit Database
            SQLiteDatabase<Student> _db = new();
            DatabaseManager.DatabaseName = "Data\\mydb.db";
            DatabaseManager.Add(_db);

            Console.WriteLine("Fetching data from database...");
            await DatabaseManager.FetchData();
            Console.WriteLine("Done!");
            #endregion

            #region Logging
            IConfigurationRoot configuration = new ConfigurationBuilder()
                        .AddJsonFile("appsettings.json")
                        .Build();

            Website website = new(configuration);

            Console.WriteLine("I am logging in");

            await website.LoginAsync();

            Console.WriteLine("I have logged in");
            #endregion

            Console.WriteLine("I am checking if there are any updates... this will take at least 2 minutes");

            await website.GetPageAndRecordCountAsync(); //check if the website has new records.

            #region If there are no updates.
            if (website.RecordCount == AppManager.RecordCount)
            {
                Console.WriteLine($"There are no new records");

                DoYouWantAnExcelReport:               
                Console.WriteLine("Do you want an Excel file with all Students details? Digit Y for Yes, N for No.");
                string? answer = Console.ReadLine();
                
                if (answer == null) 
                { 
                    Console.WriteLine("Invalid input");
                    goto DoYouWantAnExcelReport;
                }
                if (answer.ToLower().Trim().Equals("y")) goto PrintExcel;
                else if (answer.ToLower().Trim().Equals("n"))
                {
                    Console.WriteLine("okay bye.");
                    website.Dispose();
                    return; //Program Exit.
                }
                else 
                {
                    Console.WriteLine("Invalid input");
                    goto DoYouWantAnExcelReport;
                }
            }
            #endregion

            //How many pages are there?
            int pages = (website.PageCount > AppManager.PageCount) ? website.PageCount - AppManager.PageCount : 1;

            Console.WriteLine($"Reading {pages} page(s)... this might take few minutes");

            List<HtmlTablePage> HTML_Pages = []; //prepare a List to keep track of the pages to read.

            for (int i = 1; i <= pages; i++) //Add pages to read by using Query Parameters to filter specific results only.
                website.AddPageToRead(configuration, i);

            List<(string url, string content)> tuple = await website.ReadAllByBatchAsync(); //read all pages but 5 pages at the time. A batch is 5 pages.

            foreach (var tupla in tuple) 
                HTML_Pages.Add(new HtmlTablePage(tupla)); //Save the content HTML content of each read page.

            List<Student> new_students = []; //prepare a list of new students.
            List<Student> students = _db.MasterSource.Cast<Student>().ToList(); // get records from the database.

            foreach (HtmlTablePage page in HTML_Pages)
            {
                IEnumerable<IEnumerable<string>> table = page.ExtractTable(); //extract the <Table> attribute
                foreach (var row in table) // loop through the row of each table.
                {
                    Student student = new(row.ToArray()); // create a student object.
                    if (student.IsValid()) //if the student object contains valid information.
                        if (!students.Any(s => s.StudentID == student.StudentID)) //if the student is not present in the database
                            new_students.Add(student); //add the new student.
                }
            }

            Console.WriteLine($"I have found {new_students.Count} new student(s)"); // tell how many new students have been found.

            #region Perform INSERT INTO
            Console.WriteLine("Updating the database...");
            foreach (Student student in new_students)
            {
                _db.Model = student;
                _db.Crud(CRUD.INSERT);
            }
            #endregion

            #region Prepare Excel
            PrintExcel:
            students = _db.MasterSource.Cast<Student>().ToList();

            Console.WriteLine($"I am making an Excel File...");

            Excel excel = new();
            excel.CreateWorkSheet("Students");
            excel.SetHeaders("ID", "Full Name", "Contact", "Program/University");
            excel.SetAutoFilters(4);
            excel.AdjustColumnWidth();

            for (int i = 0; i < students.Count; i++)
            {
                excel.SetCellValue(i + 2, 1, students[i].StudentID);
                excel.SetCellValue(i + 2, 2, students[i].FullName);
                excel.SetCellValue(i + 2, 3, students[i].Contact);
                excel.SetCellValue(i + 2, 4, students[i].ProgramUniversity);
            }

            excel.SetColumnWidth(2, 30);
            excel.SetColumnWidth(3, 45);
            excel.SetColumnWidth(4, 80);

            Console.WriteLine($"I'm saving the Excel file on your Desktop...");
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            excel.Save(Path.Combine(desktopPath, "students.xlsx"));
            #endregion

            #region Finish
            Console.WriteLine($"Done!");
            Console.WriteLine($"student.xlsx is now available on your desktop!");

            excel.Dispose();
            website.Dispose();

            //update Settings.settings file
            AppManager.RecordCount = website.RecordCount;
            AppManager.PageCount = website.PageCount;
            #endregion
        }
    }
}