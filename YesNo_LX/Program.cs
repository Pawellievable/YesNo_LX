using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Exceptions;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace YesNo_LX
{
    public class Program
    {
        private static readonly HttpClient client = new HttpClient();
        static ITelegramBotClient botClient = new TelegramBotClient("5320287544:AAHGOJDsPdMmgku1H6MNoWCkMjmZEhd3eqk");

        public class ResponseMessageData
        {
            public string image { get; set; }
        }

        private static async Task<string> PostRequestAsync(string url, string json)
        {
            using HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
            using HttpResponseMessage response = await client.PostAsync(url, content).ConfigureAwait(false);
            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }

        private static async Task<string> GetRequestAsync(string url)
        {
            using HttpResponseMessage response = await client.GetAsync(url).ConfigureAwait(false);
            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }

        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(update));
            if (update.Type == UpdateType.Message)
            {
                var message = update.Message;
                if (message.Text.ToLower() != " ")
                {
                    if (message.Text.EndsWith("?"))
                    {
                        try
                        {
                            string jsonResponse = await GetRequestAsync("http://yesno.wtf/api");
                            ResponseMessageData responseMessage = JsonSerializer.Deserialize<ResponseMessageData>(jsonResponse);
                            await botClient.SendAnimationAsync(message.Chat, responseMessage.image);
                            Console.WriteLine("Image: {0}", responseMessage.image);
                            Console.WriteLine("Успешно");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            await botClient.SendTextMessageAsync(message.Chat, "Ответ от сервиса не получен");
                        }
                    }
                    else
                        await botClient.SendTextMessageAsync(message.Chat, "Задай любой вопрос, на который можно ответить Да или Нет :-)");
                }
            }
        }

        public static Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };
            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }

        static void Main(string[] args)
        {
            Console.WriteLine("====================================");
            Console.WriteLine("Запущен бот " + botClient.GetMeAsync().Result.FirstName);
            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            };
            botClient.StartReceiving(
                HandleUpdateAsync,
                HandlePollingErrorAsync,
                receiverOptions,
                cancellationToken
            );
            Console.WriteLine("====================================");
            Console.ReadLine();
        }
    }
}
