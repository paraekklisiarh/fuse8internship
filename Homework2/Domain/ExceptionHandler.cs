using System.Net;

namespace Fuse8_ByteMinds.SummerSchool.Domain;

public static class ExceptionHandler
{
	/// <summary>
	/// Обрабатывает исключение, которое может возникнуть при выполнении <paramref name="action"/>
	/// </summary>
	/// <param name="action">Действие, которое может породить исключение</param>
	/// <returns>Сообщение об ошибке</returns>
	public static string? Handle(Action action)
	{
		// TODO Реализовать обработку исключений
		try
		{
			action();
			return null;
		}
		catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.NotFound)
		{
			return "Ресурс не найден";
		}
		catch (HttpRequestException e)
		{
			return e.StatusCode.ToString();
		}
		catch (NegativeRubleCountException)
		{
			return "Число рублей не может быть отрицательным";
		}
		catch (NotValidKopekCountException)
		{
			return "Количество копеек должно быть больше 0 и меньше 99";
		}
		catch (MoneyException e)
		{
			return e.Message;
		}
		catch (Exception)
		{
			return "Произошла непредвиденная ошибка";
		}
	}
}

public class MoneyException : Exception
{
	public MoneyException()
	{
	}

	public MoneyException(string? message)
		: base(message)
	{
	}
}

public class NotValidKopekCountException : MoneyException
{
	public NotValidKopekCountException()
	{
	}
}

public class NegativeRubleCountException : MoneyException
{
	public NegativeRubleCountException()
	{
	}
}