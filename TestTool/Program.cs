using HolidayLib;
using System.Globalization;

namespace TestTool
{
    public class Program
    {
        private const string HolidayFile = "holiday.xml";
        private static readonly List<Holiday> holidays = new();
        private static readonly string holidayFilePath;

        static Program()
        {
            holidayFilePath = Path.Combine(Environment.CurrentDirectory, HolidayFile);
        }

        static void Main(string[] args)
        {
            if (File.Exists(holidayFilePath))
            {
                holidays.AddRange(Tools.Deserialize<Holiday[]>(File.ReadAllText(holidayFilePath)));
            }
            Menu();
        }

        /// <summary>
        /// Main menu
        /// </summary>
        static void Menu()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("Holidays on file: {0}", holidays.Count);
                Console.WriteLine("[1] List holidays");
                Console.WriteLine("[2] Add holiday");
                Console.WriteLine("[3] Show calendar");
                Console.WriteLine("[Q] Quit");
                var opt = ReadKey("123Q");
                if (opt == 'Q')
                {
                    return;
                }
                switch (opt)
                {
                    case '1':
                        HolidayList();
                        break;
                    case '2':
                        HolidayAdd();
                        break;
                    case '3':
                        CalendarShow();
                        break;
                    default:
                        throw new NotImplementedException();
                }
                Console.WriteLine("Press any key to return");
                WaitForKey();
            }
        }

        /// <summary>
        /// Lists all holidays
        /// </summary>
        private static void HolidayList()
        {
            while (true)
            {
                Console.Clear();
                if (holidays.Count == 0)
                {
                    Console.WriteLine("No holidays defined");
                    return;
                }
                var numLength = holidays.Count.ToString().Length;
                var padding = string.Empty.PadRight(numLength, '0');
                Console.WriteLine("[{0:" + padding + "}]: -- Back To Menu --", 0);
                for (var i = 0; i < holidays.Count; i++)
                {
                    Console.WriteLine("[{0:" + padding + "}]: {1}", i + 1, holidays[i].Name);
                }
                var num = ReadNumber();
                if (num < 1 || num > holidays.Count)
                {
                    return;
                }
                var h = holidays[num - 1];
                if (h == null)
                {
                    return;
                }
                Console.WriteLine("Name: {0}", h.Name);
                Console.WriteLine("Type: {0}", h.GetType().Name);
                try
                {
                    Console.WriteLine("Date: {0}", h.Compute(DateTime.Now.Year).ToShortDateString());
                }
                catch
                {
                    Console.WriteLine("Date: Does not occur this year");
                }
                Console.WriteLine();
                Console.WriteLine("[1] Edit");
                Console.WriteLine("[2] Delete");
                Console.WriteLine("[Q] Back to menu");
                switch (ReadKey("12Q"))
                {
                    case '1':
                        //TODO
                        break;
                    case '2':
                        Console.WriteLine("Are you sure [Y/N]?");
                        if (ReadKey("YN") == 'Y')
                        {
                            if (holidays.Remove(h))
                            {
                                SaveList();
                                Console.WriteLine("Holiday deleted");
                            }
                            else
                            {
                                Console.WriteLine("Holiday not found");
                            }
                            WaitForKey();
                        }
                        break;
                    case 'Q':
                        return;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        /// <summary>
        /// Adds a new holiday
        /// </summary>
        private static void HolidayAdd()
        {
            // https://de.wikipedia.org/wiki/Spencers_Osterformel
            const string EASTER =
                //a = year mod 19
                "DUP,19,MOD,STO:A," +
                //b = floor(year / 100)
                "DUP,100,/,FLOOR,STO:B," +
                //c = year mod 100
                "100,MOD,STO:C," +
                //d = floor(b / 4)
                "RCL:B,4,/,FLOOR,STO:D," +
                //e = b mod 4
                "RCL:B,4,MOD,STO:E," +
                //f = floor((b + 8) / 25)
                "RCL:B,8,+,25,/,FLOOR,STO:F," +
                //g = floor((b - f + 1) / 3)
                "RCL:B,RCL:F,-,1,+,3,/,FLOOR,STO:G," +
                //h = (19a + b - d - g + 15) mod 30
                "RCL:A,19,*,RCL:B,+,RCL:D,-,RCL:G,-,15,+,30,MOD,STO:H," +
                //i = floor(c / 4)
                "RCL:C,4,/,FLOOR,STO:I," +
                //k = c mod 4
                "RCL:C,4,MOD,STO:K," +
                //l = (32 + e2 + 2i - h - k) mod 7
                "32,2,RCL:E,*,+,2,RCL:I,*,+,RCL:H,-,RCL:k,-,7,MOD,STO:L," +
                //m = floor((1 + 11h + 22l) / 451)
                "RCL:A,11,RCL:H,*,+,22,RCL:L,*,+,451,/,FLOOR,STO:M," +
                //x = h + l - 7m + 114
                "RCL:H,RCL:L,+,7,RCL:M,*,-,114,+,STO:X," +
                //n = floor(x / 31)
                "RCL:X,31,/,FLOOR,STO:N," +
                //o = x mod 31
                "RCL:X,31,MOD,STO:O," +
                //ddmm = ((o + 1) * 100) + n
                "RCL:O,1,+,100,*,RCL:N,+";

            var name = ReadLine("Name");
            Console.WriteLine("Holiday type:");
            Console.WriteLine("[1]: Constant day");
            Console.WriteLine("     This holiday is always on the same day and month");
            Console.WriteLine("[2]: Constant weekday");
            Console.WriteLine("     This holiday is always on the same nth weekday of a month");
            Console.WriteLine("[3]: Offset from holiday");
            Console.WriteLine("     This holiday is always the same days after/before another holiday");
            Console.WriteLine("[4]: Computed holiday");
            Console.WriteLine("     This holiday has a complicated formula (for example easter)");
            Console.WriteLine("[5]: Unique holiday");
            Console.WriteLine("     This holiday occurs on a specific date and then never again");
            Console.WriteLine("[Q]: Back to menu");
            var k = ReadKey("12345Q");
            if (k == 'Q')
            {
                return;
            }
            switch (k)
            {
                case '1':
                    var constantMonth = ReadNumber("Month");
                    var constantDay = ReadNumber("Day");
                    holidays.Add(new ConstantDayHoliday()
                    {
                        DayOfMonth = constantDay,
                        Month = constantMonth,
                    });
                    break;
                case '2':
                    var weekMonth = ReadNumber("Month");

                    Console.WriteLine();
                    Console.WriteLine("Enter the weekday to base the offset on. 1=Monday, ..., 7=Sunday");
                    var weekDay = ReadNumber("Weekday");
                    weekDay = weekDay == 7 ? 0 : weekDay;
                    Console.WriteLine();
                    Console.WriteLine("Enter which weekday of the month to use. 1=First, 2=Second, etc.");
                    Console.WriteLine("Enter a negative number to count from the end. -1=Last, -2=Second last, etc.");
                    var weekDayNth = ReadNumber("Nth weekday");

                    Console.WriteLine();
                    Console.WriteLine("Enter how many days to offset from the calculated day.");
                    Console.WriteLine("Enter zero for no offset");
                    var weekDayOffset = ReadNumber("Offset");
                    holidays.Add(new ConstantWeekdayHoliday()
                    {
                        Month = weekMonth,
                        Weekday = (DayOfWeek)weekDay,
                        WeekdayIndex = weekDayNth,
                        WeekdayOffset = weekDayOffset
                    });
                    break;
                case '3':
                    if (holidays.Count == 0)
                    {
                        Console.WriteLine("No holidays in list");
                        return;
                    }
                    var offsetName = ReadLine("Enter name of holiday to offset");
                    var offsetHoliday = holidays.FirstOrDefault(m => m.Name.ToLower().Trim() == offsetName.ToLower().Trim());
                    if (offsetHoliday == null)
                    {
                        Console.WriteLine("Holiday not found");
                        return;
                    }
                    var offsetDays = ReadNumber("Days to offset");
                    holidays.Add(new OffsetHoliday()
                    {
                        BaseHoliday = offsetHoliday,
                        OffsetDays = offsetDays
                    });
                    break;
                case '4':
                    var computation = ReadLine("Enter RPN computation");
                    if (computation.ToLower() == "easter")
                    {
                        computation = EASTER;
                    }
                    holidays.Add(new ComputedHoliday()
                    {
                        Computation = computation.Split(',')
                    });
                    break;
                case '5':
                    var date = ReadLine("Enter Date in YYYY-MM-DD");
                    if (!DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal, out DateTime constantDateParsed))
                    {
                        Console.WriteLine("Failed to parse date");
                        return;
                    }
                    holidays.Add(new UniqueHoliday()
                    {
                        Date = constantDateParsed.Date
                    });
                    break;
                default:
                    throw new NotImplementedException();
            }
            //Name the last added holiday
            holidays[^1].Name = name;
            SaveList();
            Console.WriteLine("Holiday added");
        }

        /// <summary>
        /// Shows a calendar view of a given year and month
        /// </summary>
        private static void CalendarShow()
        {
            Console.Clear();
            if (holidays.Count == 0)
            {
                Console.WriteLine("No holidays defined");
                return;
            }
            Console.Write("Year: ");
            var y = ReadNumber();
            if (y < 1 || y > 9999)
            {
                Console.WriteLine("Year must be in the range of 1-9999");
                return;
            }
            Console.Write("Month: ");
            var m = ReadNumber();
            if (m < 1 || m > 12)
            {
                Console.WriteLine("Month must be in the range of 1-12");
                return;
            }
            Console.WriteLine("Holidays in {0:0000}-{1:00}", y, m);
            var hInMonth = holidays.Where(h => h.Compute(y).Month == m).ToList();
            if (hInMonth.Count == 0)
            {
                Console.WriteLine("No holidays in this month");
            }
            else
            {
                var days = new List<int>();
                foreach (var h in hInMonth)
                {
                    var date = h.Compute(y);
                    days.Add(date.Day);
                    Console.WriteLine("{0:yyyy-MM-dd}: {1}", date, h.Name);
                }
                PrintCalendar(y, m, days.ToArray());
            }
        }

        /// <summary>
        /// Saves holiday list to file
        /// </summary>
        private static void SaveList()
        {
            File.WriteAllText(holidayFilePath, Tools.Serialize(holidays));
        }

        /// <summary>
        /// Reads a line from the console
        /// </summary>
        /// <param name="query">Prompt to show</param>
        /// <returns>Read line</returns>
        private static string ReadLine(string? query = null)
        {
            if (query != null)
            {
                Console.Write("{0}: ", query);
            }
            return Console.ReadLine() ?? throw new Exception("Abort");
        }

        /// <summary>
        /// Reads a number from the console
        /// </summary>
        /// <param name="query">Prompt to show</param>
        /// <returns>Read number</returns>
        private static int ReadNumber(string? query = null)
        {
            var pos = new
            {
                X = Console.CursorLeft,
                Y = Console.CursorTop
            };

            while (true)
            {
                if (query != null)
                {
                    Console.Write("{0}: ", query);
                }
                if (int.TryParse(Console.ReadLine(), out int num))
                {
                    return num;
                }
                Console.Beep();
                ClearRegion(pos.X, pos.Y);
            }
        }

        /// <summary>
        /// Reads a single key from console
        /// </summary>
        /// <param name="chars">List of accepted keys</param>
        /// <returns>Pressed key char</returns>
        private static char ReadKey(string chars)
        {
            chars = chars.ToUpper();
            while (true)
            {
                var c = Console.ReadKey(true).KeyChar.ToString().ToUpper()[0];
                if (chars.Contains(c))
                {
                    return c;
                }
                Console.Beep();
            }
        }

        /// <summary>
        /// Flushes keyboard buffer and waits for any key press
        /// </summary>
        private static void WaitForKey()
        {
            //Clear buffer
            while (Console.KeyAvailable)
            {
                Console.ReadKey();
            }
            Console.ReadKey();
        }

        /// <summary>
        /// Clears a text region of the console between the given parameters and the cursor
        /// </summary>
        /// <param name="fromX">X position</param>
        /// <param name="fromY">Y position</param>
        private static void ClearRegion(int fromX, int fromY) => ClearRegion(fromX, fromY, Console.CursorLeft, Console.CursorTop);

        /// <summary>
        /// Clears a text region between two coordinates
        /// </summary>
        /// <param name="fromX">X position 1</param>
        /// <param name="fromY">Y position 1</param>
        /// <param name="toX">X position 2</param>
        /// <param name="toY">Y position 2</param>
        /// <exception cref="ArgumentException"></exception>
        private static void ClearRegion(int fromX, int fromY, int toX, int toY)
        {
            if (fromY > toY)
            {
                throw new ArgumentException("In Y pos: From > To");
            }
            else if (fromY == toY && fromX > toX)
            {
                throw new ArgumentException("Region length is negative");
            }
            int charCount = (toY - fromY) * Console.BufferWidth + toX + Console.BufferWidth - fromX;
            if (charCount == 0)
            {
                return;
            }
            var pos = new
            {
                X = Console.CursorLeft,
                Y = Console.CursorTop
            };
            Console.SetCursorPosition(fromX, fromY);
            Console.Write(string.Empty.PadRight(charCount));
            Console.SetCursorPosition(pos.X, pos.Y);
        }

        /// <summary>
        /// Prints a graphical calendar of a month
        /// </summary>
        /// <param name="year">Year</param>
        /// <param name="month">Month</param>
        /// <param name="highlight">Highlighted days</param>
        private static void PrintCalendar(int year, int month, params int[] highlight)
        {
            //Get list with days of month
            var days = Enumerable
                .Range(1, DateTime.DaysInMonth(year, month))
                .Select(m => m.ToString()).ToList();

            var dt = new DateTime(year, month, 1);
            var weekday = dt.DayOfWeek;
            //Insert blank spaces to account for months not starting on a monday
            while (weekday != DayOfWeek.Monday)
            {
                days.Insert(0, "");
                weekday = (DayOfWeek)((int)(weekday - 1) % 7);
                highlight = highlight.Select(m => m + 1).ToArray();
            }
            while (days.Count % 7 > 0)
            {
                days.Add("");
            }

            //Calendar header
            Console.WriteLine(@"
╔════╤════╤════╤════╤════╤════╤════╗
║ Mo │ Tu │ We │ Th │ Fr │ Sa │ Su ║
╠════╪════╪════╪════╪════╪════╪════╣");

            int i = 0; //We also need this later
            for (; i < days.Count - 7; i += 7)
            {
                Console.Write("║");
                for (var j = 0; j < 7; j++)
                {
                    //Sunday
                    if (j == 6)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                    }
                    if (highlight.Contains(j + i + 1))
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                    }
                    Console.Write(" {0,2} ", days[i + j]);
                    Console.ResetColor();
                    Console.Write("{0}", j == 6 ? "" : "│");
                }
                Console.WriteLine("║");
                Console.WriteLine("╟────┼────┼────┼────┼────┼────┼────╢");
            }

            //Last calendar row
            Console.Write("║");
            for (var j = 0; j < 7; j++)
            {
                //Sunday
                if (j == 6)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                }
                if (highlight.Contains(j + i + 1))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                }
                Console.Write(" {0,2} ", days[i + j]);
                Console.ResetColor();
                Console.Write("{0}", j == 6 ? "" : "│");
            }
            Console.WriteLine("║");
            Console.WriteLine("╙────┴────┴────┴────┴────┴────┴────╜");
        }
    }
}