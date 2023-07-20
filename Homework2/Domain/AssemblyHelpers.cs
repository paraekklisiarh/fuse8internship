using System.Reflection;

namespace Fuse8_ByteMinds.SummerSchool.Domain;

public static class AssemblyHelpers
{
    /// <summary>
    ///     Получает информацию о базовых типах классов из namespace "Fuse8_ByteMinds.SummerSchool.Domain", у которых есть
    ///     наследники.
    /// </summary>
    /// <remarks>
    ///     Информация возвращается только по самым базовым классам.
    ///     Информация о промежуточных базовых классах не возвращается
    /// </remarks>
    /// <returns>Список типов с количеством наследников</returns>
    public static (string BaseTypeName, int InheritorCount)[]? GetTypesWithInheritors()
    {
        // Базовые классы нужно брать только из пространства "Fuse8_ByteMinds.SummerSchool.Domain"
        const string nameSpace = @"Fuse8_ByteMinds.SummerSchool.Domain";

        // Метод должен просканировать текущую Assembly, достать из неё все классы
        // * В задании явно не указано, что нужно учитывать только классы "верхнего уровня"
        //		(исключая вложенные, которые могут наследоваться, наследовать и реализовывать интерфейсы).
        var assemblyClassTypes = Assembly.GetAssembly(typeof(AssemblyHelpers))!
            .GetTypes()
            .Where(p => p.IsClass);

        Dictionary<string, int> result = new();

        foreach (var classType in assemblyClassTypes)
        {
            var baseType = GetBaseType(classType);

            if (baseType == null)
            {
                // Если класс сам является самым базовым, то есть наследуется только от Object, то добавляем его в словарь
                result.TryAdd(classType.Name, 0);
            }
            else
            {
                // Исключаем из наследников абстрактные классы
                if (classType.IsAbstract) continue;

                // Исключить непринадлежащие к целевому namespace базовые классы.
                if (baseType.Namespace != nameSpace) continue;

                // Если класс является производным, то увеличиваем количество наследников у его базового класса
                if (result.ContainsKey(baseType.Name))
                    result[baseType.Name] += 1;
                else
                    result.Add(baseType.Name, 1);
            }
        }

        return result.Where(kv => kv.Value >= 1).Select(keyValue => (keyValue.Key, keyValue.Value)).ToArray();
    }

    /// <summary>
    ///     Получает базовый тип для класса
    /// </summary>
    /// <param name="type">Тип, для которого необходимо получить базовый тип</param>
    /// <returns>
    ///     Первый тип в цепочке наследований. Если наследования нет, возвращает null
    /// </returns>
    /// <example>
    ///     Класс A, наследуется от B, B наследуется от C
    ///     При вызове GetBaseType(typeof(A)) вернется C
    ///     При вызове GetBaseType(typeof(B)) вернется C
    ///     При вызове GetBaseType(typeof(C)) вернется C
    /// </example>
    private static Type? GetBaseType(Type type)
    {
        var baseType = type;

        while (baseType.BaseType is not null && baseType.BaseType != typeof(object)) baseType = baseType.BaseType;

        return baseType == type
            ? null
            : baseType;
    }
}