using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace HolidayLib
{
    /// <summary>
    /// Represents a placeholder holiday
    /// </summary>
    /// <remarks>
    /// This type cannot be XML serialized and is not supposed to be used manually
    /// </remarks>
    public class EmptyHoliday : Holiday
    {
        /// <summary>
        /// Hashcode offset to make this differ from the base <see cref="Holiday"/> type
        /// </summary>
        private const int HashcodeOffset = 0x1F25E0CF;

        /// <summary>
        /// Throws <see cref="InvalidOperationException"/>
        /// </summary>
        /// <param name="year">Ignored</param>
        /// <returns>Never</returns>
        /// <exception cref="InvalidOperationException">Attempted to use <see cref="Compute(int)"/></exception>
        [DoesNotReturn]
        public override DateTime Compute(int year)
        {
            throw new InvalidOperationException($"{nameof(EmptyHoliday)} instances cannot be computed");
        }

        public override void Deserialize(byte[] data)
        {
            using var MS = new MemoryStream(data, false);
            DeserializeBaseValues<EmptyHoliday>(MS);
        }

        public override bool Equals(object o)
        {
            return o != null && o is EmptyHoliday;
        }

        public override int GetHashCode()
        {
            return HashcodeOffset ^ GetBaseHashCode();
        }

        public override byte[] Serialize() => SerializeBaseValues<EmptyHoliday>();
    }
}
