using System.Collections;

namespace Fuse8_ByteMinds.SummerSchool.Domain;

public static class DomainExtensions
{
    /// <summary>
    /// Проверяет, содержит ли последовательность какие-либо элементы
    /// </summary>
    /// <param name="enumerable">Объект <see cref="IEnumerable" />, проверяемый на наличие элементов.</param>
    /// <typeparam name="T">Тип элементов <paramref name="enumerable"/></typeparam>
    /// <returns>true, если переданная коллекция равна null или не содержит элементов</returns>
    public static bool IsNullOrEmpty<T>(this IEnumerable<T>? enumerable)
    {
        return enumerable == null || !enumerable.Any();
    }

    //Метод JoinToString от IEnumerable<T>.
    //Должен дополнительно принимать строку разделитель и возвращать единую строку,
    //	состоящую из значений коллекции соединенных с помощью разделителя. (string.Join)

    /// <summary>
    /// Превращение коллекции в строку
    /// </summary>
    /// <param name="enumerable"></param>
    /// <param name="separator">Разделитель</param>
    /// <returns>Строка из элементов коллекции, разделенных разделителем</returns>
    public static string JoinToString(this IEnumerable enumerable, string separator)
    {
        return string.Join(separator, enumerable);
    }

    //Метод DaysCountBetween от DateTimeOffset.
    //Должен дополнительно принимать второй DateTimeOffset и возвращать количество дней между двумя датами

    /// <summary>
    /// Вычисление количества дней между двумя датами
    /// </summary>
    /// <param name="date1"></param>
    /// <param name="date2"></param>
    /// <returns>Число дней между двумя датами</returns>
    public static int DaysCountBetween(this DateTimeOffset date1, DateTimeOffset date2)
    {
        return Convert.ToInt32((date1 - date2).TotalDays);
    }
}