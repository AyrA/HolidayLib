using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;

namespace HolidayLib
{
    /// <summary>
    /// Represents a holiday that occurs on the nth weekday of a month,
    /// or at an offset of the nth weekday.
    /// </summary>
    /// <remarks>
    /// The offset calculation result can be outside of the specified month
    /// </remarks>
    [Serializable]
    public class ConstantWeekdayHoliday : Holiday
    {
        /// <summary>
        /// Hashcode offset
        /// </summary>
        private const int HashcodeOffset = 0x08E1FC8C;

        private DayOfWeek weekday = DayOfWeek.Monday;
        private int weekdayOffset = 0;
        private int weekdayIndex = 1;
        private int month = 1;

        /// <summary>
        /// Gets or sets the weekday to base the calculation on
        /// </summary>
        public DayOfWeek Weekday
        {
            get => weekday; set
            {
                if (!Helpers.IsDefined(value))
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                weekday = value;
            }
        }

        /// <summary>
        /// Gets or sets the days to offset from the calculated weekday
        /// </summary>
        public int WeekdayOffset { get => weekdayOffset; set => weekdayOffset = value; }

        /// <summary>
        /// Gets or sets which weekday of the month to use.
        /// </summary>
        /// <remarks>
        /// If this is positive, it means the nth occurence of the weekday from the start of the month.
        /// If this is negative, it means the nth occurence of the weekday from the end of the month.
        /// Zero is not a permitted value.
        /// </remarks>
        public int WeekdayIndex
        {
            get => weekdayIndex; set
            {
                if (value == 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Value cannot be zero");
                }
                weekdayIndex = value;
            }
        }

        /// <summary>
        /// Gets or sets the month to base the calculation on
        /// </summary>
        public int Month
        {
            get => month; set
            {
                if (value < 1 || value > 12)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                month = value;
            }
        }

        public override DateTime Compute(int year)
        {
            EnsureValidYear(year);
            if (WeekdayIndex == 0)
            {
                throw new InvalidOperationException(nameof(WeekdayIndex) + " has not been set");
            }
            if (!Weekday.IsDefined())
            {
                throw new InvalidOperationException(nameof(Weekday) + " has an unsupported value");
            }
            if (Month < 1 || Month > 12)
            {
                throw new InvalidOperationException(nameof(Month) + " is out of range");
            }
            //Create date at either end of the specified month
            //depending on whether WeekdayIndex is negative or not
            var dt = new DateTime(year, Month, WeekdayIndex < 0 ? DateTime.DaysInMonth(year, Month) : 1);
            //Keep adding or removing days until we're at the chosen week day
            while (dt.DayOfWeek != Weekday)
            {
                dt = dt.AddDays(Math.Sign(WeekdayIndex));
            }
            //Step forwards or backwards one less than as many weeks as specified.
            //One less because we're already at the first possible day that matches at this point
            dt = dt.AddDays(7 * (WeekdayIndex - Math.Sign(WeekdayIndex)));
            //Add final offset and return
            return dt.AddDays(WeekdayOffset);
        }

        public override void Deserialize(byte[] data)
        {
            using var MS = new MemoryStream(data, false);
            DeserializeBaseValues<ConstantWeekdayHoliday>(MS);
            using var BR = new BinaryReader(MS);
            var dow = (DayOfWeek)BR.ReadByte();
            var wOffset = BR.ReadInt32();
            var wIndex = BR.ReadInt32();
            var m = BR.ReadByte();
            if (m < 1 || m > 12)
            {
                throw new InvalidDataException($"Month outside of permitted range. Value was '{m}'");
            }
            if (wIndex == 0)
            {
                throw new InvalidDataException($"Weekday index cannot be zero. Value was '{wIndex}'");
            }
            var prev = new
            {
                Weekday,
                WeekdayOffset,
                WeekdayIndex,
                Month
            };
            try
            {
                Weekday = dow;
                WeekdayOffset = wOffset;
                WeekdayIndex = wIndex;
                Month = m;
            }
            catch
            {
                //Restore
                Weekday = prev.Weekday;
                WeekdayOffset = prev.WeekdayOffset;
                WeekdayIndex = prev.WeekdayIndex;
                Month = prev.Month;
                throw;
            }
        }

        public override bool Equals(object o)
        {
            if (o is null)
            {
                return false;
            }
            if (!(o is ConstantWeekdayHoliday h))
            {
                return false;
            }
            if (!h.CompareBaseValues(this))
            {
                return false;
            }
            return Weekday == h.Weekday
                && Month == h.Month
                && WeekdayIndex == h.WeekdayIndex
                && WeekdayOffset == h.WeekdayOffset;

        }

        public override int GetHashCode()
        {
            return HashcodeOffset
                ^ GetBaseHashCode()
                ^ Weekday.GetHashCode()
                ^ Month.GetHashCode()
                ^ WeekdayIndex.GetHashCode()
                ^ WeekdayOffset.GetHashCode();
        }

        public override byte[] Serialize()
        {
            using var MS = new MemoryStream();
            using var BW = new BinaryWriter(MS, Encoding.UTF8);
            BW.Write(SerializeBaseValues<ConstantWeekdayHoliday>());
            BW.Write((byte)Weekday);
            BW.Write(WeekdayOffset);
            BW.Write(WeekdayIndex);
            BW.Write((byte)Month);
            BW.Flush();
            return MS.ToArray();
        }
    }
}
