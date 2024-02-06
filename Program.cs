using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CacheTips
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            const int MillisecondsDelayAfterAdd = 50;
            const int MillisecondsAbsoluteExpiration = 750;

            HostApplicationBuilder builder = new HostApplicationBuilder(args);

            builder.Services.AddMemoryCache();

            using IHost host = builder.Build();

            IMemoryCache cache = host.Services.GetRequiredService<IMemoryCache>();

            var addLetterToCacheTask = IterateAlphabetAsync(async letter =>
            {
                MemoryCacheEntryOptions options = new()
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMilliseconds(MillisecondsAbsoluteExpiration)
                };

                _ = options.RegisterPostEvictionCallback(OnPostEviction);

                AlphabetLetter alphabetLetter = cache.Set(letter, new AlphabetLetter(letter), options);

                Console.WriteLine($"Adicionando a letra {alphabetLetter.Letter} ao cache");

                await Task.Delay(TimeSpan.FromMilliseconds(MillisecondsDelayAfterAdd));
            });
            await addLetterToCacheTask;

            var readLettersFromCacheTask = IterateAlphabetAsync(async letter =>
            {
                if (cache.TryGetValue(letter, out object? value) && value is AlphabetLetter alphabetLetter)
                {
                    Console.WriteLine($"{letter} continua no cache. {alphabetLetter.Message}");
                }

                await Task.CompletedTask;
            });
            await readLettersFromCacheTask;

            await host.RunAsync();
        }

        static void OnPostEviction(object key, object? letter, EvictionReason reason, object? state)
        {
            if (letter is AlphabetLetter alphabetLetter)
            {
                Console.WriteLine($"A letra {alphabetLetter.Letter} foi removida do cache por {reason}");
            }
        }

        record AlphabetLetter(char Letter)
        {
            internal string Message =>
                $"A letra {Letter} é a {Letter - 64} letra do alfabeto";
        }

        static async Task IterateAlphabetAsync(Func<char, Task> function)
        {
            for (char letter = 'A'; letter <= 'Z'; ++letter)
            {
                await function(letter);
            }

            Console.WriteLine();
        }
    }
}
