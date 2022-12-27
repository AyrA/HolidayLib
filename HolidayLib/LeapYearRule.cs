namespace HolidayLib
{
    /// <summary>
    /// Handling for 29 February outside of leap years
    /// </summary>
    public enum LeapYearRule
    {
        /// <summary>
        /// No handling
        /// </summary>
        /// <remarks>
        /// Methods will throw in this case
        /// </remarks>
        None = 0,
        /// <summary>
        /// Use 28 February
        /// </summary>
        PreviousDay = 1,
        /// <summary>
        /// Use 1 March
        /// </summary>
        NextDay = 2
    }
}
