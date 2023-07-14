namespace Fuse8_ByteMinds.SummerSchool.Domain;

/// <summary>
/// Модель для хранения денег
/// </summary>
public class Money
{
	public Money(int rubles, int kopecks)
		: this(false, rubles, kopecks)
	{
	}

	public Money(bool isNegative, int rubles, int kopecks)
	{
		IsNegative = isNegative;
		Rubles = rubles;
		Kopecks = kopecks;
	}

	/// <summary>
	/// Отрицательное значение
	/// </summary>
	public bool IsNegative { get; }

	/// <summary>
	/// Число рублей
	/// </summary>
	public int Rubles { get; }

	/// <summary>
	/// Количество копеек
	/// </summary>
	public int Kopecks { get; }
}