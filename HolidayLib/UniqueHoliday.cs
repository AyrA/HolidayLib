using System;

namespace HolidayLib
{
    /// <summary>
    /// Represents a holiday that occurs only once,
    /// or is too complex to be represented by any other type and has to be specified manually
    /// </summary>
    [Serializable]
    public class UniqueHoliday : Holiday
    {
        /// <summary>
        /// Hashcode offset
        /// </summary>
        private const int HashcodeOffset = 0x14BAC30F;

        private DateTime date;

        /// <summary>
        /// Gets or sets the date
        /// </summary>
        /// <remarks>
        /// Also sets the <see cref="Holiday.ActiveFromYear"/>
        /// and <see cref="Holiday.ActiveToYear"/> properties appropriately
        /// </remarks>
        public DateTime Date
        {
            get
            {
                return date;
            }
            set
            {
                date = value;
                ActiveFromYear = value.Year;
                ActiveToYear = value.Year;
            }
        }

        public override DateTime Compute(int year)
        {
            var dt = Date.Date;
            if (year != dt.Year)
            {
                throw new ArgumentException($"Only {dt.Year} is valid for this holiday as it's unique to that year", nameof(year));
            }
            return dt;
        }

        public override bool Equals(object o)
        {
            if (o is null)
            {
                return false;
            }
            if (!(o is UniqueHoliday h))
            {
                return false;
            }
            if (!h.CompareBaseValues(this))
            {
                return false;
            }
            return Date == h.Date;

        }

        public override int GetHashCode()
        {
            return HashcodeOffset
                ^ GetBaseHashCode()
                ^ Date.GetHashCode();
        }
    }
}
