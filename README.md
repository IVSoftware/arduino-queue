You want tasks to execute in multiple stages, but _specifically_ in the context of Arduino, and I know something about your command structure from your other [question]() and my [answer](). To take it to the next level, a `Queue` structure would allow you to load up a command sequence and by taking advantage of [polymorphism](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/object-oriented/polymorphism) the commands themselves can know what their waiting for. For example you said offline that a `Home` command waits for "home done" whereas an XY seeking command waits for "x done" _and_ "y done" which can occur in either order. So in this particular thought experiment, we might define these command classes:



