namespace Fuse8_ByteMinds.SummerSchool.Domain;

/// <summary>
/// Контейнер для значения, с отложенным получением
/// </summary>
public class Lazy<TValue>
{
	public Lazy(Func<TValue> value)
	{
		Value = value.Invoke();
	}

	public TValue? Value { get; }
}