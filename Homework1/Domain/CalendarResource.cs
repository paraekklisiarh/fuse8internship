namespace Fuse8_ByteMinds.SummerSchool.Domain;

/// <summary>
/// Значения ресурсов для календаря
/// </summary>
public class CalendarResource
{
	public static readonly CalendarResource Instance;

	public static readonly string January;
	public static readonly string February;

	private static readonly string[] MonthNames;

	static CalendarResource()
	{
		Instance = new CalendarResource();
		MonthNames = new[]
		{
			"Январь",
			"Февраль",
			"Март",
			"Апрель",
			"Май",
			"Июнь",
			"Июль",
			"Август",
			"Сентябрь",
			"Октябрь",
			"Ноябрь",
			"Декабрь",
		};
		February = GetMonthByNumber(1);
		January = GetMonthByNumber(0);
	}

	private static string GetMonthByNumber(int number)
	{
		if (number < 0 || number > MonthNames.Length)
		{
			throw new ArgumentOutOfRangeException();
		}

		return MonthNames[number];
	}

	// Индексатор для получения названия месяца по перечислению Month
	public string this[Month index] => GetMonthByNumber((int)index);
}

public enum Month
{
	January,
	February,
	March,
	April,
	May,
	June,
	July,
	August,
	September,
	October,
	November,
	December,
}