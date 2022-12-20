using System;
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

        public override DateTime Compute(int year)
        {
            if (BaseHoliday is EmptyHoliday)
            {
                throw new InvalidOperationException($"{nameof(BaseHoliday)} has not been assigned a proper holiday yet");
            }
            return ComputeInternal(year, 1);

        }

        private DateTime ComputeInternal(int year, int level)
        {
            if (level > RecursionLimit)
            {
                throw new Exception($"Operation aborted. Too many {nameof(OffsetHoliday)} types referenced");
            }
            if (BaseHoliday is OffsetHoliday oh)
            {
                return oh.ComputeInternal(year, level + 1).AddDays(OffsetDays);
            }
            return BaseHoliday.Compute(year).AddDays(OffsetDays);
        }

        public override bool Equals(object o)
        {
            return EqualsInternal(o, 1);
        }

        private bool EqualsInternal(object o, int level)
        {
            if (level > RecursionLimit)
            {
                throw new Exception($"Operation aborted. Too many {nameof(OffsetHoliday)} types referenced");
            }
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
                return refOh.EqualsInternal(selfOh, level + 1);
            }
            return BaseHoliday.Equals(h.BaseHoliday);
        }

        public override int GetHashCode()
        {
            return GetHashCodeInternal(1);
        }

        private int GetHashCodeInternal(int level)
        {
            if (level > RecursionLimit)
            {
                throw new Exception($"Operation aborted. Too many {nameof(OffsetHoliday)} types referenced");
            }
            int baseHashCode;
            if (BaseHoliday is OffsetHoliday oh)
            {
                baseHashCode = oh.GetHashCodeInternal(level + 1);
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
    }
}
