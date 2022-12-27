using System;
using System.IO;
using System.Linq;
using System.Text;

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
        /// Hashcode offset
        /// </summary>
        private const int HashcodeOffset = 0x3FE95DBA;

        /// <summary>
        /// Computation routine of the holiday in RPN.
        /// This routine should return the date in a single number as "ddmm".
        /// A leading zero is optional for days 1-9
        /// </summary>
        /// <remarks>See readme for the RPN instructions</remarks>
        public string[] Computation { get; set; } = Array.Empty<string>();

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
            EnsureValidComputation();
            EnsureValidYear(year);
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
            return HashcodeOffset ^ GetBaseHashCode() ^ Computation.GetHashCode();
        }

        public override void Deserialize(byte[] data)
        {
            using var MS = new MemoryStream(data, false);
            DeserializeBaseValues<ComputedHoliday>(MS);
            using var BR = new BinaryReader(MS);
            var count = BR.ReadInt32();
            if (count < 0)
            {
                throw new InvalidDataException("Negative array member count");
            }
            var comp = Enumerable
                .Range(0, count)
                .Select(m => BR.ReadString())
                .ToArray();
            EnsureValidComputation(comp);
            Computation = comp;
        }

        public override byte[] Serialize()
        {
            EnsureValidComputation();
            using var MS = new MemoryStream();
            using var BW = new BinaryWriter(MS, Encoding.UTF8);
            BW.Write(SerializeBaseValues<ComputedHoliday>());
            BW.Write(Computation.Length);
            foreach (var comp in Computation)
            {
                BW.Write(comp);
            }
            BW.Flush();
            return MS.ToArray();
        }

        private void EnsureValidComputation()
        {
            EnsureValidComputation(Computation);
        }

        public static void EnsureValidComputation(string[] computation)
        {
            if (computation == null || computation.Length == 0)
            {
                throw new InvalidOperationException("Computation has not been set.");
            }
            if (computation.Contains(null))
            {
                throw new ArgumentException("Computation contains null entries");
            }
            if (computation.Any(string.IsNullOrWhiteSpace))
            {
                throw new ArgumentException("Computation contains empty entries");
            }
            try
            {
                Helpers.EnsureValidRPN(computation);
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Invalid RPN instructions present. See inner exception for details", ex);
            }
        }
    }
}
