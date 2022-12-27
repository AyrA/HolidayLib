using System;
using System.IO;
using System.Linq.Expressions;
using System.Text;

namespace HolidayLib
{
    /// <summary>
    /// Represents the simplest form of a holiday,
    /// one that occurs on the same month and day every year
    /// </summary>
    [Serializable]
    public class ConstantDayHoliday : Holiday
    {
        /// <summary>
        /// Hashcode offset
        /// </summary>
        private const int HashcodeOffset = 0x26E2A76E;
        private int dayOfMonth = 1;
        private int month = 1;
        private LeapYearRule leapYearRule;

        public LeapYearRule LeapYearRule
        {
            get => leapYearRule; set
            {
                if (!Helpers.IsDefined(value))
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                leapYearRule = value;
            }
        }

        /// <summary>
        /// Gets or sets the day of month
        /// </summary>
        public int DayOfMonth
        {
            get
            {
                return dayOfMonth;
            }
            set
            {
                if (value < 1 || value > DateTime.DaysInMonth(2000, month))
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                dayOfMonth = value;
            }
        }

        /// <summary>
        /// Gets or sets the month
        /// </summary>
        public int Month
        {
            get
            {
                return month;
            }
            set
            {
                if (value < 1 || value > 12)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                //Clamp day if necessary
                if (dayOfMonth > DateTime.DaysInMonth(2000, month))
                {
                    dayOfMonth = DateTime.DaysInMonth(2000, month);
                }
                month = value;
            }
        }

        public override DateTime Compute(int year)
        {
            EnsureValidYear(year);
            var m = month;
            var d = dayOfMonth;
            if (m == 2 && d == 29 && !DateTime.IsLeapYear(year))
            {
                switch (leapYearRule)
                {
                    case LeapYearRule.None:
                        throw new InvalidOperationException($"The day and month combination is only valid on leap years, and '{nameof(LeapYearRule)}' is set to '{leapYearRule}'");
                    case LeapYearRule.PreviousDay:
                        --d;
                        break;
                    case LeapYearRule.NextDay:
                        d = 1;
                        ++m;
                        break;
                    default:
                        throw new NotImplementedException($"Unknown leap year rule value: '{leapYearRule}'.");
                }
            }
            return new DateTime(year, m, d);
        }

        public override void Deserialize(byte[] data)
        {
            using var MS = new MemoryStream(data, false);
            DeserializeBaseValues<ConstantDayHoliday>(MS);
            using var BR = new BinaryReader(MS);
            var d = BR.ReadByte();
            var m = BR.ReadByte();
            var lyr = (LeapYearRule)BR.ReadByte();
            if (!Helpers.IsDefined(lyr))
            {
                throw new InvalidDataException($"Leap year rule value '{lyr}' is invalid.");
            }
            if (m < 1 || m > 12)
            {
                throw new InvalidDataException($"Month outside of permitted range. Value was '{m}'");
            }
            if (d < 1 || d > DateTime.DaysInMonth(2000, m))
            {
                throw new InvalidDataException($"Day outside of permitted range for month '{m}'. Value was '{d}'");
            }
            var prev = new
            {
                LeapYearRule,
                Month,
                DayOfMonth
            };
            try
            {
                LeapYearRule = lyr;
                Month = m;
                DayOfMonth = d;
            }
            catch
            {
                //Restore values
                LeapYearRule = prev.LeapYearRule; Month = prev.Month; DayOfMonth = prev.DayOfMonth;
                throw;
            }
        }

        public override bool Equals(object o)
        {
            if (o is null)
            {
                return false;
            }
            if (!(o is ConstantDayHoliday h))
            {
                return false;
            }
            if (!h.CompareBaseValues(this))
            {
                return false;
            }
            return DayOfMonth == h.DayOfMonth && Month == h.Month;

        }

        public override int GetHashCode()
        {
            return HashcodeOffset ^ GetBaseHashCode() ^ DayOfMonth.GetHashCode() ^ Month.GetHashCode();
        }

        public override byte[] Serialize()
        {
            using var MS = new MemoryStream();
            using var BW = new BinaryWriter(MS, Encoding.UTF8);
            BW.Write(SerializeBaseValues<ConstantDayHoliday>());
            BW.Write((byte)DayOfMonth);
            BW.Write((byte)Month);
            BW.Write((byte)leapYearRule);
            BW.Flush();
            return MS.ToArray();
        }
    }
}
