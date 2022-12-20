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
            foreach (var h in hInMonth)
            {
                Console.WriteLine("{0:yyyy-MM-dd}: {1}", h.Compute(y), h.Name);
            }
        }

        private static void SaveList()
        {
            File.WriteAllText(holidayFilePath, Tools.Serialize(holidays));
        }

        private static string ReadLine(string? query = null)
        {
            if (query != null)
            {
                Console.Write("{0}: ", query);
            }
            return Console.ReadLine() ?? throw new Exception("Abort");
        }

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

        private static void WaitForKey()
        {
            //Clear buffer
            while (Console.KeyAvailable)
            {
                Console.ReadKey();
            }
            Console.ReadKey();
        }

        private static void ClearRegion(int fromX, int fromY) => ClearRegion(fromX, fromY, Console.CursorLeft, Console.CursorTop);

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
    }
}