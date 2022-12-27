using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

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
        /// Gets the year of <see cref="Date"/>
        /// or throws if attempting to set this value to 
        /// anything other than the year of <see cref="Date"/>
        /// </summary>
        [NotNull]
        public new int? ActiveFromYear
        {
            get => date.Year;
            set
            {
                if (value.HasValue && value == date.Year)
                {
                    return;
                }
                throw new InvalidOperationException($"{nameof(ActiveFromYear)} cannot be set to anything other than {date.Year} in this {nameof(UniqueHoliday)} instance");
            }
        }

        /// <summary>
        /// Gets the year of <see cref="Date"/>
        /// or throws if attempting to set this value to
        /// anything other than the year of <see cref="Date"/>
        /// </summary>
        [NotNull]
        public new int? ActiveToYear
        {
            get => date.Year;
            set
            {
                if (value.HasValue && value == date.Year)
                {
                    return;
                }
                throw new InvalidOperationException($"{nameof(ActiveToYear)} cannot be set to anything other than {date.Year} in this {nameof(UniqueHoliday)} instance");
            }
        }

        /// <summary>
        /// Hashcode offset
        /// </summary>
        private const int HashcodeOffset = 0x14BAC30F;

        private DateTime date;

        /// <summary>
        /// Gets or sets the date
        /// </summary>
        /// <remarks>
        /// Also sets the <see cref="ActiveFromYear"/>
        /// and <see cref="ActiveToYear"/> properties appropriately.
        /// The time component of the supplied date is cut off.
        /// <see cref="DateTime.Kind"/> information is retained.
        /// </remarks>
        public DateTime Date
        {
            get
            {
                return date;
            }
            set
            {
                date = value.Date;
                base.ActiveFromYear = ActiveFromYear = value.Year;
                base.ActiveToYear = ActiveToYear = value.Year;
            }
        }

        public override DateTime Compute(int year)
        {
            EnsureValidYear(year);
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

        public override void Deserialize(byte[] data)
        {
            using var MS = new MemoryStream(data, false);
            DeserializeBaseValues<UniqueHoliday>(MS);
            using var BR = new BinaryReader(MS);

            //No need to validate the second argument. The constructor does this for us
            var newDate = new DateTime(BR.ReadInt64(), (DateTimeKind)BR.ReadByte());

            var prev = new
            {
                Date
            };
            try
            {
                Date = newDate;
            }
            catch
            {
                //Restore
                Date = prev.Date;
                throw;
            }
        }

        public override byte[] Serialize()
        {
            using var MS = new MemoryStream();
            using var BW = new BinaryWriter(MS, Encoding.UTF8);
            BW.Write(SerializeBaseValues<UniqueHoliday>());
            BW.Write(date.Ticks);
            BW.Write((byte)date.Kind);
            BW.Flush();
            return MS.ToArray();
        }
    }
}
