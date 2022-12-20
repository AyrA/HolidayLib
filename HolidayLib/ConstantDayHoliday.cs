using System;

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

        /// <summary>
        /// Gets or sets the day of month
        /// </summary>
        public int DayOfMonth { get; set; }
        /// <summary>
        /// Gets or sets the month
        /// </summary>
        public int Month { get; set; }

        public override DateTime Compute(int year)
        {
            return new DateTime(year, Month, DayOfMonth);
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
    }
}
