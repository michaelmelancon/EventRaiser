EventRaiser
===========

A functional library for raising .net events where a monadic structure is built around EventHandler&lt;T>.

```c#
class Sample : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged; 

    private string _property;
    public string Property
    {
        get
        {
            return _property;
        }
        set
        {
            _property = value;
            PropertyChanged
                .ToHandlerOf<PropertyChangedEventArgs>() // To EventHandler<T>
                .Resilient() // prevent a single handler from stopping the process
                .Parallel() // invoke all handlers in parallel.
                .Async() // invoke asynchronously
                .Raise(this, new PropertyChangedEventArgs("Property")); // raise the event with our specs.
        }
    }
}
```