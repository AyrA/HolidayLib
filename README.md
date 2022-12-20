# HolidayLib

This is a library for storing holidays in a manner that allows automated computations

# Types

Different types of various complexity are supported.
They all support XML serialization out of the box.

## Holiday

This is the base type you cannot directly use.

It defines a few properties common to all holidays
and defines functions that derived types must implement.

### Base Properties

- **Id**: This is used to tell holidays with the same name apart. You usually do not want to change this
- **Name**: The human readable holiday name
- **ActiveFromYear**: First year this holiday happens. Usually unset
- **ActiveToYear**: Last year this holiday happens. Usually unset
- **Optional**: Indicates that this is a special day but mostly treated as a regular work day if set
- **StartTime**: Usually midnight, but can be set for holidays that do not start with the day
- **Duration**: Usually one day, but can be set for holidays that do not end after one day

Note that the properties are not actually used by the derived types.
It's the programmers responsibility to properly use `StartTime` and `Duration`,
the caluclation routine will always just return the date.

### Functions

#### CompareBaseValues(Holiday)

- Availability: Derived types only
- Implementation: Given in base type

Compares the base properties (see above) of two holiday types for equality.

#### GetBaseHashCode

- Availability: Derived types only
- Implementation: Given in base type

Like object.GetHashCode() but will use the hash code from the base properties.
This can be used by derived types to not have to manually implement hash code computations for them.

#### Compute(int)

- Availability: Public
- Implementation: Required in derived type

Computes the date of a holiday for the given year.
If the year is not appropriate, an exception should be thrown.

Note: For consistency reasons,
do not try to be smart with holidays that on occasion might cross the 31st December.
If you configure a holiday to be the Wednesday before the first Thursday in January
it can fall into December. Do not try to fiddle with the year and just leave it as is.
This configuration might lead to the same holiday appearing twice in the same year,
and if you mess with the year you will render one of the two occurences unobtainable.
In other words, no two year arguments should ever lead to the same holiday date.


#### Compute(int,int)

- Availability: Public
- Implementation: Optional in derived type

This calls `Compute(int)` for the given year range and returns the dates in the order of the years.
The base implementation simply calls `Compute(int)` in a loop for the given year range.
You can provide your own implementation for your derived types if you believe there's a better way of doing it,
one example would be if your holiday type uses a database as backing store for date values,
in this case you want to use a single query with a year range instead of 100 individual queries.

#### Equals(object)

- Availability: Public
- Implementation: Required in derived type

This is an override of `object.Equals(object)` and you must provide an implementation to compare your derived types.
Equality for a holiday means it's the same derived type and all public properties have the same value.

#### GetHashCode

- Availability: Public
- Implementation: Required in derived type

This is an override of `object.GetHashCode` and you must provide an implementation to compare your derived types.
How you implement this is up to you, a common approach is to XOR all hash codes of your public properties together,
and optionally XOR them with a randomly chosen constant value
to avoid the chance for two different types to be considered equal.
See `GetBaseHashCode()` if you follow this approach.

## ConstantDayHoliday

This is the simplest form for a holiday.
It occurs on the same date every year.
New years eve and x-mas fall into this category for example.

### Properties

- **DayOfMonth**: Day in a month where this holiday happens
- **Month**: Month when this holiday happens

Note: They both together must form a valid day and month combination.

## ConstantWeekdayHoliday

This holiday occurs on the same weekday in a month.
This is also suitable for holidays that occur a certain number of days before/after a given weekday.

### Properties

- **Month**: The month to base the calculation on
- **Weekday**: The weekday to base the computation on
- **WeekdayIndex**: Specifies the nth weekday in a month. Negative calculates from the end of the month backwards (1=first from start, -1=first from end)
- **WeekdayOffset**: Days to offset the computed date

Note: These values can be set in a way that the date happens outside of the given month.
For example, 15 is a valid value for **WeekdayIndex**, and will add around 3 months to the calculation.
Same with the offset. The offset can be more than 6 days into the future or past from the computed day.

## OffsetHoliday

This is a holiday that is offset a given number of days from a different holiday

### Properties

- **BaseHoliday**: The holiday to base the offset on
- **RecursionLimit**: Recursion limit (static value, default: 10)

### About recursion

The recursion limit limits how many holidays can be stacked,
specifically, it prevents deep nestings of OffsetHoliday types, or even loops of them.
You normally do not need to raise the limit.
Raising the limit beyond reasonable values will eventually throw a `StackOverflowException` by the runtime.

Note: The limit also applies to `GetHashCode` and `Equals`

## ComputedHoliday

This is the most complex holiday type shipped with HolidayLib.
This type is useful when the holiday is based on a mathematical formula.
Example: Easter

### Properties

- **Computation**: This is a string array with steps for the builtin RPN calculator (see below)

Note: The calculation will start with the year supplied by the `Compute()` call already on the stack.

## UniqueHoliday

This holiday type is appropriate for holidays so complex they need to be manually calculated and hardcoded,
or for a holiday that happens only once.

### Properties

- **Date**: Date the holiday occurs

Note: This type will properly set `ActiveFromYear` and `ActiveToYear` values when setting the date value.

`Compute(year)` throws if you try to use a different year
than what has been defined in the given date value.

## EmptyHoliday

This type only exists as a placeholder for when OffsetHoliday is instantiated
to ensure that the `BaseHoliday` property always has a value assigned.

This type has no properties, and always throws when attempting to calculate.
This type cannot be XML serialized either.

## RPN Calculator

The library comes with an RPN style calculator for the ComputedHoliday type.

RPN stands for "Reverse Polish Notation".
In this notation, the arguments are typed before the operand.
It's one of three used notation types.

Adding 3 and 4 in different notations:

- Prefix notation (polish notation): +, 3, 4
- Infix notation (standard notation): 3, +, 4
- Postfix notation (reverse polish notation): 3, 4, +

RPN was popularized in the scientific and engineering community in the 70s
by HP with their RPN style scientific and financial calculator line.

Reverse polish notation has the advantage that it's very easy to parse,
because by the time the operator is specified, all operands are specified already.
It also makes brackets unnecessary.
`(2+3)*7` can be entered as `2 3 + 7 *` or `7 2 3 + *` if you prefer all numbers first.
RPN operates based on a stack. Every number is put on a stack,
and operators consume them, and put the result back on the stack.

Note: In regards to operands order,
this implementation wants them in the order you would write them using infix notation.
That is, `3/4` is entered as `3,4,/` and not in reverse `4,3,/`,
even though a stack would suggest it to be done this way.
This is done to make using the calculator easier for people unfamiliar with RPN.

## Stack

The stack in this implementation is only limited by available memory.
Any number entered is pushed onto the stack,
and any operator entered will consume a certain number of items from the stack,
and then put the result back on the stack.
If the stack does not contain enough values,
(for example `+` requires two but only one is present) the calculator will throw.

## Result

The calculator will return the top value of the stack as result.
The ComputedHoliday type expects this result to be a 3 or 4 digit number,
in either `dmm` or `ddmm` format.
The simplest way to achieve this is to multiply the day with 100, then adding the month to it.

Note: The stack should be empty when the calculation routine has ended and the top value is removed for returning.
A non-empty stack is usually the sign of an operational error by the user.

## Operators

It's best to look at the source code for this, but in general,
all common operators are supported:

- `+-*/`: Basic maths
- `\`: behaves like integer division (result rounded towards zero)
- `%`: Modulo. Can also use `mod` as an alias
- `**`: Power

Furthermore, `ceil`,`floor`, and `round` are supported for rounding
(round takes two arguments, second is decimal places count).
Mathematical comparison operators are available as well (C# syntax),
they put 1 on the stack if the comparison succeeds, 0 otherwise.

`dup` and `swap` will duplicate the top value or swap the top two values respectively.

## Commands

These are advanced commands that go further than basic RPN.

- `STO:x`: Saves the top value in storage slot `x`, the slot designation can be any single character. The top value from the stack is consumed.
- `RCL:x`: Recalls a value from memory onto the top of the stack. The memory will persist.
- `DEL:x`: Deletes a storage slot. Not necessary, you can just overwrite with another STO. Memory is cleared automatically at the end.

## Example: Easter

Easter is one of those holidays that follows a formula. The formula is given below.

You can use it in the test application by simply typing "easter" when asked for the formula.
The lines below include C style comments with the appropriate infix notation

```C#
//Easter in RPN as per https://de.wikipedia.org/wiki/Spencers_Osterformel
var easter =
	//a = year mod 19
	"DUP,19,MOD,STO:A," +
	//b = floor(year / 100)
	"DUP,100,/,FLOOR,STO:B," +
	//c = year mod 100
	"100,MOD,STO:C," +
	//d = floor(b / 4)
	"RCL:B,4,/,FLOOR,STO:D," +
	//e = b mod 4
	"RCL:B,4,MOD,STO:E," +
	//f = floor((b + 8) / 25)
	"RCL:B,8,+,25,/,FLOOR,STO:F," +
	//g = floor((b - f + 1) / 3)
	"RCL:B,RCL:F,-,1,+,3,/,FLOOR,STO:G," +
	//h = (19a + b - d - g + 15) mod 30
	"RCL:A,19,*,RCL:B,+,RCL:D,-,RCL:G,-,15,+,30,MOD,STO:H," +
	//i = floor(c / 4)
	"RCL:C,4,/,FLOOR,STO:I," +
	//k = c mod 4
	"RCL:C,4,MOD,STO:K," +
	//l = (32 + e2 + 2i - h - k) mod 7
	"32,2,RCL:E,*,+,2,RCL:I,*,+,RCL:H,-,RCL:k,-,7,MOD,STO:L," +
	//m = floor((1 + 11h + 22l) / 451)
	"RCL:A,11,RCL:H,*,+,22,RCL:L,*,+,451,/,FLOOR,STO:M," +
	//x = h + l - 7m + 114
	"RCL:H,RCL:L,+,7,RCL:M,*,-,114,+,STO:X," +
	//n = floor(x / 31)
	"RCL:X,31,/,FLOOR,STO:N," +
	//o = x mod 31
	"RCL:X,31,MOD,STO:O," +
	//ddmm = ((o + 1) * 100) + n
	"RCL:O,1,+,100,*,RCL:N,+";
```
