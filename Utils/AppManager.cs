namespace Pipeline
{
    public class AppManager
    {
        public static void ExitOnError(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(msg);
            Environment.Exit(1);
        }

        public static int PageCount
        { 
            get => Settings.Default.PageCount;
            set
            { 
                Settings.Default.PageCount = value;
                Settings.Default.Save();
            }
        }

        public static int RecordCount
        {
            get => Settings.Default.RecordCount;
            set
            {
                Settings.Default.RecordCount = value;
                Settings.Default.Save();
            }
        }
    }
}