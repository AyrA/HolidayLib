using System;

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

        /// <summary>
        /// Gets or sets the weekday to base the calculation on
        /// </summary>
        public DayOfWeek Weekday { get; set; }

        /// <summary>
        /// Gets or sets the days to offset from the calculated weekday
        /// </summary>
        public int WeekdayOffset { get; set; }

        /// <summary>
        /// Gets or sets which weekday of the month to use.
        /// </summary>
        /// <remarks>
        /// If this is positive, it means the nth occurence of the weekday from the start of the month.
        /// If this is negative, it means the nth occurence of the weekday from the end of the month.
        /// Zero is not a permitted value.
        /// </remarks>
        public int WeekdayIndex { get; set; }

        /// <summary>
        /// Gets or sets the month to base the calculation on
        /// </summary>
        public int Month { get; set; }

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
    }
}
