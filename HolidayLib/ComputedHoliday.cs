using System;
using System.Linq;

namespace HolidayLib
{
    /// <summary>
    /// Represents a holiday that has a complicated computation to calculate the occurence.
    /// One such example is easter
    /// </summary>
    [Serializable]
    public class ComputedHoliday : Holiday
    {
        /// <summary>
        /// Computation routine of the holiday in RPN.
        /// This routine should return the date in a single number as "ddmm".
        /// A leading zero is optional for days 1-9
        /// </summary>
        /// <remarks>See readme for the RPN instructions</remarks>
        public string[] Computation { get; set; } = new string[0];

        public ComputedHoliday(string[] computation)
        {
            Computation = (string[])computation.Clone();
        }

        public ComputedHoliday()
        {
            Computation = Array.Empty<string>();
        }

        public override DateTime Compute(int year)
        {
            var ddmm = Helpers.RPN(Computation, year);
            var dd = (int)Math.Floor(ddmm / 100);
            var mm = (int)ddmm % 100;

            var dt = new DateTime(year, mm, 1);
            return dt.AddDays(dd - 1);
        }

        public override bool Equals(object o)
        {
            if (o is null)
            {
                return false;
            }
            if (!(o is ComputedHoliday h))
            {
                return false;
            }
            if (!h.CompareBaseValues(this))
            {
                return false;
            }
            if (Computation.Length != h.Computation.Length)
            {
                return false;
            }
            for (var i = 0; i < Computation.Length; i++)
            {
                if (Computation[i] != h.Computation[i])
                {
                    return false;
                }
            }
            return true;
        }

        public override int GetHashCode()
        {
            return GetBaseHashCode() ^ Computation.GetHashCode();
        }
    }
}
