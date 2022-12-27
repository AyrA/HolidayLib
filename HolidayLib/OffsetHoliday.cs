using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace HolidayLib
{
    [Serializable]
    public class OffsetHoliday : Holiday
    {
        /// <summary>
        /// Hashcode offset
        /// </summary>
        private const int HashcodeOffset = 0x304D075F;

        /// <summary>
        /// The default recursion limit of <see cref="OffsetHoliday"/> types
        /// </summary>
        public static readonly int DefaultRecursionLimit = 10;

        /// <summary>
        /// Gets or sets the recursion limit
        /// </summary>
        /// <remarks>
        /// <see cref="BaseHoliday"/> can also be an <see cref="OffsetHoliday"/> type,
        /// which can be used to build recursive or deeply nested structures.
        /// To avoid dangerous <see cref="StackOverflowException"/>,
        /// the recursion limit can be set here.
        /// The default value upon startup is <see cref="DefaultRecursionLimit"/>.
        /// This value is not serialized
        /// </remarks>
        [XmlIgnore]
        public static int RecursionLimit { get; set; } = DefaultRecursionLimit;

        /// <summary>
        /// Gets or sets the holiday to base the computation on
        /// </summary>
        public Holiday BaseHoliday { get; set; }

        /// <summary>
        /// Gets or sets the days to offset from <see cref="BaseHoliday"/>
        /// </summary>
        public int OffsetDays { get; set; }

        public OffsetHoliday(Holiday baseHoliday)
        {
            BaseHoliday = baseHoliday;
        }

        public OffsetHoliday()
        {
            BaseHoliday = new EmptyHoliday();
        }

        /// <summary>
        /// Calls <see cref="EnsureRecursionLimit(OffsetHoliday, int)"/> with <see cref="RecursionLimit"/>
        /// </summary>
        /// <param name="instance"><see cref="OffsetHoliday"/> instance</param>
        public static void EnsureRecursionLimit(OffsetHoliday instance) => EnsureRecursionLimit(instance, RecursionLimit);

        /// <summary>
        /// Ensures the supplied instance doesn't exceeds the permitted recursion limit
        /// </summary>
        /// <param name="instance"><see cref="OffsetHoliday"/> instance</param>
        /// <param name="limit">Recursion limit. Must be at least 1</param>
        /// <exception cref="ArgumentOutOfRangeException">Invalid value for <paramref name="limit"/></exception>
        /// <exception cref="Exception">Recursion depth higher than <paramref name="limit"/> allows</exception>
        public static void EnsureRecursionLimit(OffsetHoliday instance, int limit)
        {
            if (limit <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(limit), "Limit too low. Checking recursion limit not possible");
            }
            int count = 0;
            var h = instance;
            while (h != null)
            {
                if (++count > limit)
                {
                    throw new Exception($"Operation aborted. Too many {nameof(OffsetHoliday)} types referenced");
                }
                h = h.BaseHoliday as OffsetHoliday;
            }
        }

        /// <summary>
        /// Ensures that the nesting of <see cref="BaseHoliday"/> does not exceed <see cref="RecursionLimit"/>
        /// </summary>
        /// <exception cref="Exception">Too many nested <see cref="BaseHoliday"/> values</exception>
        private void EnsureRecursionLimit()
        {
            EnsureRecursionLimit(this, RecursionLimit);
        }

        public override DateTime Compute(int year)
        {
            EnsureValidYear(year);
            if (BaseHoliday is EmptyHoliday)
            {
                throw new InvalidOperationException($"{nameof(BaseHoliday)} has not been assigned a proper holiday yet");
            }
            EnsureRecursionLimit();
            return ComputeInternal(year);

        }

        private DateTime ComputeInternal(int year)
        {
            if (BaseHoliday is OffsetHoliday oh)
            {
                return oh.ComputeInternal(year).AddDays(OffsetDays);
            }
            return BaseHoliday.Compute(year).AddDays(OffsetDays);
        }

        public override bool Equals(object o)
        {
            EnsureRecursionLimit();
            return EqualsInternal(o);
        }

        private bool EqualsInternal(object o)
        {
            if (o is null)
            {
                return false;
            }
            if (!(o is OffsetHoliday h))
            {
                return false;
            }
            if (!h.CompareBaseValues(this))
            {
                return false;
            }
            if (OffsetDays != h.OffsetDays)
            {
                return false;
            }
            //Special handling to prevent recursion
            if (h.BaseHoliday is OffsetHoliday refOh && BaseHoliday is OffsetHoliday selfOh)
            {
                return refOh.EqualsInternal(selfOh);
            }
            return BaseHoliday.Equals(h.BaseHoliday);
        }

        public override int GetHashCode()
        {
            return GetHashCodeInternal();
        }

        private int GetHashCodeInternal()
        {
            EnsureRecursionLimit();
            int baseHashCode;
            if (BaseHoliday is OffsetHoliday oh)
            {
                baseHashCode = oh.GetHashCodeInternal();
            }
            else
            {
                baseHashCode = BaseHoliday.GetHashCode();
            }
            return HashcodeOffset
                ^ GetBaseHashCode()
                ^ OffsetDays.GetHashCode()
                ^ baseHashCode;
        }

        public override void Deserialize(byte[] data)
        {
            using var MS = new MemoryStream(data, false);
            DeserializeBaseValues<OffsetHoliday>(MS);
            using var BR = new BinaryReader(MS);
            var serializedH = BR.ReadBytes(BR.ReadInt32());
            var h = DeserializeGenericHoliday(serializedH);
            var oDays = BR.ReadInt32();

            var prev = new
            {
                BaseHoliday,
                OffsetDays
            };
            try
            {
                BaseHoliday = h;
                OffsetDays = oDays;
                EnsureRecursionLimit();
            }
            catch
            {
                //Restore
                BaseHoliday = prev.BaseHoliday;
                OffsetDays = prev.OffsetDays;
                throw;
            }
        }

        public override byte[] Serialize()
        {
            EnsureRecursionLimit();
            using var MS = new MemoryStream();
            using var BW = new BinaryWriter(MS, Encoding.UTF8);
            BW.Write(SerializeBaseValues<OffsetHoliday>());
            var serialized = BaseHoliday.Serialize();
            BW.Write(serialized.Length);
            BW.Write(serialized);
            BW.Write(OffsetDays);
            BW.Flush();
            return MS.ToArray();
        }
    }
}
