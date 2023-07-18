namespace Fuse8_ByteMinds.SummerSchool.Domain;

// Проблемы подхода:
// 1) Следует использовать не int, а short или любой другой только положительный тип.

// Проблемы при решении задачи:
// 1) оператор вычитания можно реализовать с использованием унарного `-`
//      (в нём либо создавать новый объект -- безопасно и затратно,
//      либо добавить приватный сеттер для isNegative -- небезопасно, но дешевле), либо без его использования.
//      Реализация без использования унарного `-` выглядит громоздко и болезненно.

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
        if (kopecks is < 0 or > 99
            || rubles < 0
            || (isNegative && rubles == 0 && kopecks == 0))
            throw new ArgumentException();

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

    /// <summary>
    /// Сложение двух Money. При достижении макс. значения копеек - добавляется 1 руб.
    /// </summary>
    /// <param name="money1"></param>
    /// <param name="money2"></param>
    /// <returns>Объект Money - сумма двух объектов</returns>
    public static Money operator +(Money money1, Money money2)
    {
        return money1.IsNegative switch
        {
            // -m1 + m2 == m2 - |m1|
            true when !money2.IsNegative => money2 - -money1,
            // m1 + (-m2) == m1 - |m2|
            false when money2.IsNegative => money1 - -money2,
            // m1 + m2 && -m1 + -m2
            true when money2.IsNegative => -ModularAddition(money1, money2),
            _ => ModularAddition(money1, money2)
        };
    }

    /// <summary>
    /// Внутренний метод для сложения двух положительных или двух отрицательных объектов Money
    /// </summary>
    /// <param name="money1"></param>
    /// <param name="money2"></param>
    /// <returns>Объект Money - сумма обоих объектов</returns>
    private static Money ModularAddition(Money money1, Money money2)
    {
        // Я предполагаю, что хранить int дешевле, чем дважды вычислять сумму копеек в return.
        // Как минимум, это красивее!
        int kopecks = money1.Kopecks + money2.Kopecks;

        return new Money(
            money1.Rubles + money2.Rubles + kopecks / 100,
            kopecks % 100);
    }

    public static Money operator -(Money money)
    {
        return new Money(!money.IsNegative, money.Rubles, money.Kopecks);
    }

    /// <summary>
    /// Вычитание двух объектов Money
    /// </summary>
    /// <param name="money1">Уменьшаемое</param>
    /// <param name="money2">Вычитаемое</param>
    /// <returns>Объект Money - разность обоих объектов.</returns>
    public static Money operator -(Money money1, Money money2)
    {
        if (money1 == money2) return Zero();

        if (money2 == Zero()) return money1;

        switch (money1.IsNegative)
        {
            case true when !money2.IsNegative:
                // -m1 - m2 = -|m1 + m2|
                return -ModularAddition(money1, money2);
            case false when money2.IsNegative:
                // m1 - -m2 = m1+m2
                return money1 + -money2;
        }

        if (money1.IsNegative && money2.IsNegative)
        {
            // -m1 - -m2 
            if (money1 > money2)
                //= -|money1 - money2|
                return -(-money1 - -money2);
            else
                // = -|money2 - money1|
                return -money2 - -money1;
        }

        // !money1.isNegative && !money2.isNegative
        if (money1 >= money2)
        {
            // money1 - money2
            int resultKopecks = money1.Rubles * 100 + money1.Kopecks - (money2.Rubles * 100 + money2.Kopecks);

            return new Money(
                false,
                resultKopecks / 100,
                resultKopecks % 100
            );
        }
        else
        {
            // -|money2 - money1|
            int resultKopecks = money2.Rubles * 100 + money2.Kopecks - (money1.Rubles * 100 + money1.Kopecks);

            return new Money(
                true,
                resultKopecks / 100,
                resultKopecks % 100
            );
        }
    }


    /// <summary>
    /// Метод выполняет сравнение двух чисел без учета знака
    /// </summary>
    /// <param name="money1"></param>
    /// <param name="money2"></param>
    /// <returns>money1 > money2.</returns>
    private static bool GreaterModulusComparison(Money money1, Money money2)
    {
        return money1.Rubles > money2.Rubles ||
               (money1.Rubles == money2.Rubles && money1.Kopecks > money2.Kopecks);
    }

    public static bool operator >(Money money1, Money money2)
    {
        return money1.IsNegative switch
        {
            true when !money2.IsNegative => false,
            false when money2.IsNegative => true,
            true when money2.IsNegative => !GreaterModulusComparison(money1, money2),
            _ => GreaterModulusComparison(money1, money2)
        };
    }

    public static bool operator >=(Money money1, Money money2)
    {
        return money1 > money2 || money1 == money2;
    }

    public static bool operator <(Money money1, Money money2)
    {
        return money1.IsNegative switch
        {
            // -m1 < m2
            true when !money2.IsNegative => true,
            // m1 < -m2
            false when money2.IsNegative => false,
            // -m1 < -m2
            true when money2.IsNegative => GreaterModulusComparison(money1, money2),
            // 0.0 < ##, при чем !IsNegative по условию в конструкторе
            _ when money1 is { Kopecks: 0, Rubles: 0 } => false,
            // m1 < m2
            _ => !GreaterModulusComparison(money1, money2)
        };
    }

    public static bool operator <=(Money money1, Money money2)
    {
        return money1 < money2 || money1.Equals(money2);
    }

    /// <summary>
    /// Определяет, равен ли указанный объект текущему объекту Money.
    /// </summary>
    /// <param name="obj">Объект для сравнения с текущим объектом Money.</param>
    /// <returns>true, если объекты считаются равными; в противном случае - false.</returns>
    public override bool Equals(object? obj)
    {
        return (obj != null && ReferenceEquals(this, obj)) || Equals((obj as Money)!);
    }

    /// <summary>
    /// Определяет, равен ли указанный Money текущему объекту Money.
    /// </summary>
    /// <param name="other">Money для сравнения с текущим Money.</param>
    /// <returns>true, если объекты считаются равными; в противном случае - false.</returns>
    public bool Equals(Money other)
    {
        return ReferenceEquals(this, other) ||
               (IsNegative == other.IsNegative &&
                Kopecks == other.Kopecks &&
                Rubles == other.Rubles);
    }

    /// <summary>
    /// Определяет, равны ли два объекта Money.
    /// </summary>
    /// <param name="money1"></param>
    /// <param name="money2"></param>
    /// <returns>true, если объекты считаются равными; в противном случае - false.</returns>
    public static bool Equals(Money money1, Money money2)
    {
        // Наверное, сравнить два bool будет менее затратно, можно было бы поменять местами с RefecenceEquals...
        return ReferenceEquals(money1, money2) ||
               (money1.IsNegative == money2.IsNegative &&
                money1.Kopecks == money2.Kopecks &&
                money1.Rubles == money2.Rubles);
    }

    public static bool operator ==(Money money1, Money money2)
    {
        return Equals(money1, money2);
    }

    public static bool operator !=(Money money1, Money money2)
    {
        return !(money1 == money2);
    }

    /// <summary>
    /// Трансформирует объект Money в строку
    /// </summary>
    /// <returns>Рублей.Копеек</returns>
    public override string ToString()
    {
        return $"{(IsNegative ? "-" : "")}{Rubles}.{Kopecks:D2}";
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Rubles, Kopecks);
    }

    public static Money Zero()
    {
        return new Money(false, 0, 0);
    }
}