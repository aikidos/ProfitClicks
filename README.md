## Введение
В данном репозитории содержится решение тестового задания для компании "ПРОФИТКЛИКС".  

**Цель задания:**  
*Реализовать систему очередей задач (библиотекой).*  
*Разруливать задачи должен `TaskManager`, задачи могут быть различных типов.  
У задач могут быть различные уровни приоритетов:*  
1. *очень низкий*  
1. *низкий*  
1. *средний*  
1. *высокий*  
1. *очень высокий*  

*У задач есть события:*
- *Создание*
- *Выполнение*
- *Завершение*
- *Ошибка выполнения*

*На любое событие можно вешать от одного и более обработчиков, они должны
выполняться по очередности **FIFO**.*  
*Наличие тестов обязательное, и документирование в стиле `godoc` плюсом.*  
*Использовать готовые решения и библиотеки запрещено.*

## Для решения были использованы
- .NET Standard 2.0
- .NET Core 2.2 (запуск тестов)
- [xUnit](https://xunit.net/)
- [Moq](https://github.com/moq/moq4)

## API

Реализация задачи `DownloadContentJob`:

```csharp
sealed class DownloadContentJob : IJob
{
    public string Url { get; }

    public string Content { get; private set; }

    public DownloadContentJob(string url)
    {
        Url = url;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        using (var client = new WebClient())
            Content = await client.DownloadStringTaskAsync(Url);
    }
}
```

Запуск задач:

```csharp
using (var manager = new TaskManager(maxActiveJobs: 2))
{
    manager.Add(new DownloadContentJob("https://google.com/"), JobPriority.High);
    manager.Add(new DownloadContentJob("https://duckduckgo.com/"), JobPriority.VeryHigh);
    manager.Add(new DownloadContentJob("https://bing.com/"), JobPriority.VeryLow);
    manager.Add(new DownloadContentJob("https://yandex.ru/")); // По умолчанию приоритет - `JobPriority.Normal`

    manager.JobStarted += (sender, e) =>
    {
        if (!(e.Job is DownloadContentJob downloadJob))
            return;
    
        Console.WriteLine($"STARTED ({e.Priority}): `{downloadJob.Url}`");
    };
    
    manager.JobCompleted += (sender, e) =>
    {
        if (!(e.Job is DownloadContentJob downloadJob))
            return;
    
        Console.WriteLine($"COMPLETED: `{downloadJob.Url}` (Length: {downloadJob.Content.Length})");
    };
    
    manager.JobFailed += (sender, e) =>
    {
        if (!(e.Job is DownloadContentJob downloadJob))
            return;
    
        Console.WriteLine($"FAILED: `{downloadJob.Url}` (Exception: {e.Exception})");
    };


    await manager.StartAsync();

    Console.ReadLine();
}
```

Результат вывода:
> STARTED (VeryHigh): `https://duckduckgo.com/`  
  STARTED (High): `https://google.com/`  
  COMPLETED: `https://duckduckgo.com/` (Length: 5414)  
  STARTED (Normal): `https://yandex.ru/`  
  COMPLETED: `https://google.com/` (Length: 51336)  
  STARTED (VeryLow): `https://bing.com/`  
  COMPLETED: `https://yandex.ru/` (Length: 164022)  
  COMPLETED: `https://bing.com/` (Length: 89787)
  
## FAQ
- **Что можно было бы улучшить?**  
Реализовать метод `WaitForAllJobsCompleted`. Это позволило бы, к примеру, не использовать `Task.Delay` в тестах.  
Реализовать метод `TaskManager.AddRange`, `TaskManager.Count` и т.д.