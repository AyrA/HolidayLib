using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace HolidayLib
{
    /// <summary>
    /// Represents the base holiday type
    /// </summary>
    [Serializable]
    [XmlInclude(typeof(ComputedHoliday))]
    [XmlInclude(typeof(ConstantDayHoliday))]
    [XmlInclude(typeof(ConstantWeekdayHoliday))]
    [XmlInclude(typeof(EmptyHoliday))]
    [XmlInclude(typeof(OffsetHoliday))]
    [XmlInclude(typeof(UniqueHoliday))]
    public abstract class Holiday
    {
        /// <summary>
        /// Base hashcode offset
        /// </summary>
        private const int HashcodeOffset = 0x3DE9D6B5;

        /// <summary>
        /// Gets or sets the id of the holiday.
        /// </summary>
        /// <remarks>
        /// This is randomly generated when you create a holiday,
        /// and is retained through serialization.
        /// You usually should not manually change it,
        /// as it's the only value to separate otherwise identical holiday types
        /// </remarks>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Gets or sets the human readable name of the holiday
        /// </summary>
        public string Name { get; set; } = "Unnamed Holiday";

        /// <summary>
        /// Gets or sets the earliest year this holiday is active in
        /// </summary>
        /// <remarks>If null, no start is specified</remarks>
        public int? ActiveFromYear { get; set; }

        /// <summary>
        /// Gets or sets the latest year this holiday is active in
        /// </summary>
        /// <remarks>If null, no end is specified</remarks>
        public int? ActiveToYear { get; set; }

        /// <summary>
        /// Gets if this holiday is not mandatory to be observed
        /// </summary>
        public bool Optional { get; set; } = false;

        /// <summary>
        /// Gets the time of day this holiday starts
        /// </summary>
        /// <remarks>This defaults to midnight</remarks>
        public TimeSpan StartTime { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// Gets the duration of the holiday
        /// </summary>
        /// <remarks>This defaults to 1 day</remarks>
        public TimeSpan Duration { get; set; } = TimeSpan.FromDays(1);

        /// <summary>
        /// Compares the base properties of two holiday instances
        /// </summary>
        /// <param name="h">Holiday instance</param>
        /// <returns>true, if base properties are identical</returns>
        protected bool CompareBaseValues(Holiday h)
        {
            if (h == null)
            {
                return false;
            }
            return
                Id == h.Id
                && Name == h.Name
                && ActiveFromYear == h.ActiveFromYear
                && ActiveToYear == h.ActiveToYear
                && Optional == h.Optional
                && StartTime == h.StartTime
                && Duration == h.Duration;
        }

        /// <summary>
        /// Calculates the HashCode from the base values
        /// </summary>
        /// <returns>Hashcode</returns>
        protected int GetBaseHashCode()
        {
            var hc = HashcodeOffset;
            hc ^= Id.GetHashCode();
            hc ^= Name.GetHashCode();
            hc ^= ActiveFromYear == null ? 0 : ActiveFromYear.GetHashCode();
            hc ^= ActiveToYear == null ? 0 : ActiveToYear.GetHashCode();
            hc ^= Duration.GetHashCode();
            hc ^= Optional.GetHashCode();
            hc ^= StartTime.GetHashCode();
            hc ^= Duration.GetHashCode();
            return hc;
        }

        /// <summary>
        /// Computes the holiday for the given year
        /// </summary>
        /// <param name="year">year</param>
        /// <returns>Date the holiday occurs in that year</returns>
        /// <remarks>
        /// The holiday is not forced to actually be in that year if the configuration of the holiday makes it fall outside
        /// </remarks>
        public abstract DateTime Compute(int year);

        /// <summary>
        /// Checks if two instances are of the same type and have all properties set to the same values
        /// </summary>
        /// <param name="o">object</param>
        /// <returns>true, if identical holidays</returns>
        public override abstract bool Equals(object o);

        /// <summary>
        /// Gets the HashCode of this instance
        /// </summary>
        /// <returns>HashCode</returns>
        public override abstract int GetHashCode();

        /// <summary>
        /// Computes dates of this instance across multiple years
        /// </summary>
        /// <param name="yearFrom">First year (inclusive)</param>
        /// <param name="yearTo">Last year (inclusive)</param>
        /// <returns>dates</returns>
        public virtual IEnumerable<DateTime> Compute(int yearFrom, int yearTo)
        {
            while (yearFrom <= yearTo)
            {
                yield return Compute(yearFrom++);
            }
        }
    }
}
