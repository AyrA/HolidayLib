using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
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
        /// Serializes the values of the base Holiday type
        /// </summary>
        /// <typeparam name="T">Derived type</typeparam>
        /// <returns>Serialized values</returns>
        protected byte[] SerializeBaseValues<T>()
        {
            using var MS = new MemoryStream();
            using var BW = new BinaryWriter(MS, Encoding.UTF8);
            BW.Write(HashcodeOffset);
            BW.Write(typeof(T).FullName);
            BW.Write(Id.ToByteArray());
            BW.Write(Name);
            Helpers.SerializeNullable(BW, ActiveFromYear);
            Helpers.SerializeNullable(BW, ActiveToYear);
            BW.Write(Optional);
            BW.Write(StartTime.Ticks);
            BW.Write(Duration.Ticks);
            BW.Flush();
            return MS.ToArray();
        }

        /// <summary>
        /// Deserializes base holiday values
        /// and verifies that <typeparamref name="T"/> matches the supplied data
        /// </summary>
        /// <typeparam name="T">Derived type</typeparam>
        /// <param name="s">Stream with values</param>
        /// <exception cref="ArgumentException">The given data is not of the derived type <typeparamref name="T"/></exception>
        /// <exception cref="InvalidDataException">The given data is of the proper type but a decoded value is invalid</exception>
        protected void DeserializeBaseValues<T>(Stream s)
        {
            if (s is null)
            {
                throw new ArgumentNullException(nameof(s));
            }
            if (!s.CanRead)
            {
                throw new ArgumentException("Stream not marked as readable", nameof(s));
            }

            using var BR = new BinaryReader(s, Encoding.UTF8, true);
            var hashcode = BR.ReadInt32();
            if (hashcode != HashcodeOffset)
            {
                throw new InvalidDataException($"Invalid magic number. Expected '{HashcodeOffset}' but got '{hashcode}'.");
            }
            var decoded = BR.ReadString();
            var expected = typeof(T).FullName;
            if (decoded != expected)
            {
                throw new ArgumentException($"Wrong type <{nameof(T)}>. Decoded holiday type '{decoded}' does not match expected type '{expected}'");
            }
            //Read values into a copy first
            var newValues = new
            {
                Id = new Guid(BR.ReadBytes(16)),
                Name = BR.ReadString(),
                ActiveFromYear = Helpers.DeserializeNullableInt(BR),
                ActiveToYear = Helpers.DeserializeNullableInt(BR),
                Optional = BR.ReadBoolean(),
                StartTime = TimeSpan.FromTicks(BR.ReadInt64()),
                Duration = TimeSpan.FromTicks(BR.ReadInt64()),
            };
            //Validation
            if (Id == Guid.Empty)
            {
                throw new InvalidDataException("Id is empty");
            }
            if (ActiveFromYear.HasValue && ActiveToYear.HasValue && ActiveToYear.Value < ActiveFromYear.Value)
            {
                throw new InvalidDataException("End year is before start year");
            }
            if (newValues.Duration.Ticks < 0)
            {
                throw new InvalidDataException("Deserialized duration is negative");
            }

            //Copy after success
            Id = newValues.Id;
            Name = newValues.Name;
            ActiveFromYear = newValues.ActiveFromYear;
            ActiveToYear = newValues.ActiveToYear;
            Optional = newValues.Optional;
            StartTime = newValues.StartTime;
            Duration = newValues.Duration;
        }

        /// <summary>
        /// Derived types should call this in <see cref="Compute"/> to validate the year argument
        /// </summary>
        /// <param name="year">Year</param>
        /// <exception cref="InvalidOperationException">Year outside of bounds</exception>
        protected void EnsureValidYear(int year)
        {
            if (ActiveFromYear.HasValue && ActiveFromYear.Value > year)
            {
                throw new InvalidOperationException($"Cannot calculate holiday because {year}<{ActiveFromYear}");
            }
            if (ActiveToYear.HasValue && ActiveToYear.Value < year)
            {
                throw new InvalidOperationException($"Cannot calculate holiday because {year}>{ActiveToYear}");
            }
        }

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

        /// <summary>
        /// Deserializes data previously serialized with <see cref="Serialize"/>
        /// and fills in all appropriate properties
        /// </summary>
        /// <param name="data">Data previously serialized with <see cref="Serialize"/></param>
        public abstract void Deserialize(byte[] data);

        /// <summary>
        /// Serializes data into binary data
        /// </summary>
        /// <returns>
        /// Serialized data that can later be used with <see cref="Deserialize(byte[])"/>
        /// or <see cref="Deserialize{T}(byte[])"/>
        /// </returns>
        public abstract byte[] Serialize();

        /// <summary>
        /// Creates a holiday instance of type <typeparamref name="T"/>
        /// and calls <see cref="Deserialize(byte[])"/> on it with <paramref name="data"/>
        /// </summary>
        /// <typeparam name="T">Derived holiday type to deserialize</typeparam>
        /// <param name="data">Serialized data</param>
        /// <returns>Instantiated and deserialized instance</returns>
        public static T Deserialize<T>(byte[] data) where T : Holiday
        {
            var instance = (T)typeof(T)
                .GetConstructor(Array.Empty<Type>())
                .Invoke(Array.Empty<object>());
            instance.Deserialize(data);
            return instance;
        }

        /// <summary>
        /// Deserializes a type that implements <see cref="Holiday"/>
        /// without prior knowledge about the exact type
        /// </summary>
        /// <param name="data">Serialized data</param>
        /// <returns>Deserialized instance</returns>
        /// <exception cref="InvalidDataException">Data is not a valid serialized holiday instance</exception>
        /// <exception cref="InvalidOperationException">Failed to create instance from given data</exception>
        public static Holiday DeserializeGenericHoliday(byte[] data)
        {
            //Get type name
            using var MS = new MemoryStream(data, false);
            using var BR = new BinaryReader(MS, Encoding.UTF8);
            var hashcode = BR.ReadInt32();
            if (hashcode != HashcodeOffset)
            {
                throw new InvalidDataException($"Invalid magic number. Expected '{HashcodeOffset}' but got '{hashcode}'.");
            }
            var decoded = BR.ReadString();

            //Try to get type from given name
            var t = Type.GetType(decoded) ?? throw new InvalidDataException($"Unknown type '{decoded}'");
            
            //Find a constructor without arguments
            var c = t.GetConstructor(Array.Empty<Type>()) ?? throw new InvalidOperationException($"Type '{t.FullName}' has no parameterless constructor");
            
            //Try to create an instance
            var h = c.Invoke(null) as Holiday ?? throw new InvalidDataException($"Type '{t.FullName}' does not derives from '{nameof(Holiday)}'");
            
            //Deserialize instance data
            h.Deserialize(data);

            return h;
        }
    }
}
