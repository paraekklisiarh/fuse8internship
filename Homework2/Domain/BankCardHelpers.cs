using System.Reflection;

namespace Fuse8_ByteMinds.SummerSchool.Domain;

public static class BankCardHelpers
{
	/// <summary>
	/// Получает номер карты без маски
	/// </summary>
	/// <param name="card">Банковская карта</param>
	/// <returns>Номер карты без маски</returns>
	public static string GetUnmaskedCardNumber(BankCard card)
	{
		// С помощью рефлексии получить номер карты без маски
		
		var numberInfo = typeof(BankCard).GetField("_number", BindingFlags.Instance | BindingFlags.NonPublic);
		if (numberInfo == null) return string.Empty;
		if (numberInfo.GetValue(card) == null) return string.Empty;
		
		return (string)numberInfo.GetValue(card);
	}
}