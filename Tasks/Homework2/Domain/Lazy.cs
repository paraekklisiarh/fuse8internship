namespace Fuse8_ByteMinds.SummerSchool.Domain;

/// <summary>
/// Контейнер для значения, с отложенным получением
/// </summary>
public class Lazy<TValue>
{
    private TValue? _value;
    
    // Я попытался проверять _value на null, но это не сработало со значимыми типами
    // и я не смог найти способ сделать genetic nullable
    private bool _isValueInitialized;
    
    private readonly Func<TValue> _valueFactory;

    public Lazy(Func<TValue> valueFactory)
    {
        _valueFactory = valueFactory;
        _isValueInitialized = false;
    }

    public TValue? Value
    {
        get
        {
            if (_isValueInitialized) return _value;

            _value = _valueFactory.Invoke();
            _isValueInitialized = true;
            return _value;
        }
    }
}