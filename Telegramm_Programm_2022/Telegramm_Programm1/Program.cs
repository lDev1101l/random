using System;
using System.Collections.Generic;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using System.Threading;
using System.Text;

namespace ShoppingListBot
{
    enum StateBot
    {
        Start,
        NewList,
        Archive,
        Share,
        AddItems
    }

    class Program
    {
        private static StateBot _state;

        private static List<string> shoppingList = new(); // TODO: сделать уникальный список для каждого пользовтеля

        static void Main(string[] args)
        {
            // Создаем экземпляр бота с помощью токена
            var botClient = new TelegramBotClient("YOUR_TOKEN_HERE");

            // Отправляем запрос на сервер телеграмма, чтобы бот начал получать обновления
            botClient.StartReceiving(Update, Error);

            Console.WriteLine("Bot has started");

            Console.ReadLine();

            Console.WriteLine("Bot has stopped");
        }

        async static Task Update(ITelegramBotClient botClient, Update update, CancellationToken arg3)
        {
            var user = update.Message.From; // получили текущего пользователя
            var chatId = update.Message.Chat.Id; // ID чата

            // получили уникальный идентификатор пользователя !!! На который надо ориентироваться, чтобы всё у всех было разное
            long userId = user.Id; 

            string fname = userId.ToString() + ".dat";
            if (System.IO.File.Exists(fname))
            {
                // если пользователь ранее был, то считываем из файла текущее его состояние
                string str = System.IO.File.ReadAllText(fname); // читаем весь файл в переменную
                _state = (StateBot)int.Parse(str); // преобразовываем в число и приводим к enum !! TODO: сделать проверку на преобразование
            }
            else
            {
                // иначе назначем ему самое первое состояние
                _state = StateBot.Start;
            }
            
            string msgText = update.Message.Text;

            // В этом блоке мы обрабатываем команду назад (/back)
            if (msgText == "/back")
            {
                _state = StateBot.Start;
                msgText = "/start";
            }

            Console.WriteLine($"State bot is {_state}");
            Console.WriteLine($"Receive message type: {update.Message.Type}");
            Console.WriteLine($"Received a '{msgText}' message in chat {chatId} from @{user!.Username}");

            if (update.Message.Type == MessageType.Text)
            {
                switch (_state)
                {
                    case StateBot.Start:
                        //Добавляем клавиатуру в бота чтобы жмакать кнопки, а не писать команды в ручную
                        var keyboard = new ReplyKeyboardMarkup(new[]
                        {
                            new[]
                            {
                                new KeyboardButton("/start"),
                                new KeyboardButton("/new list")
                            },
                            new[]
                            {
                                new KeyboardButton("/archive"),
                                new KeyboardButton("/share")
                            }
                        });

                        switch (msgText)
                        {
                            //Команда "старт" (начало очевидно:) )
                            case "/start":
                                await botClient.SendTextMessageAsync(
                                    chatId: chatId,
                                    text: "Привет! Я бот для списка покупок. Чтобы начать, нажми /new list",
                                    replyMarkup: keyboard);
                                break;

                            //Команда "новый список"
                            case "/new list":
                                keyboard = new ReplyKeyboardMarkup(new[] { new[] { new KeyboardButton("/back"), } });
                                await botClient.SendTextMessageAsync(
                                    chatId: chatId,
                                    text: "Введите название списка:");

                                _state = StateBot.NewList;
                                break;

                            //Команда "архив". Здесь будут храниться все списки созданные ранее
                            case "/archive":
                                keyboard = new ReplyKeyboardMarkup(new[] { new[] { new KeyboardButton("/back"), } });

                                
                                //else
                                await botClient.SendTextMessageAsync(
                                    chatId: chatId,
                                    text: "Архив пуст",
                                    replyMarkup: keyboard);
                                _state = StateBot.Archive;

                                break;

                            //Команда "Поделиться". Должна будет форматировать выбранный список и делать из него красивый, чтобы можно было переслать.
                            case "/share":
                                await botClient.SendTextMessageAsync(
                                    chatId: chatId,
                                    text: $"Список {shoppingList} :",
                                    replyMarkup: keyboard);
                                _state = StateBot.Share;
                                break;

                            default:
                                keyboard = new ReplyKeyboardMarkup(new[] { new[] { new KeyboardButton("/back"), } });

                                await botClient.SendTextMessageAsync(
                                        chatId: chatId,
                                        text: "Не понял о чем вы говорите. Попробуйте снова :/",
                                        replyMarkup: keyboard);
                                _state = StateBot.Start;
                                break;
                        }

                        break;

                    case StateBot.NewList:

                        var listName = msgText;

                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: $"Вы ввели название списка {listName}. Далее вводим построчно пункты списка. Чтобы закончить наберите /done");

                        // TODO: сохранение названия списка для каждого пользователя

                        shoppingList = new List<string>(); // создание списка относительно userId и введённого названия
                        _state = StateBot.AddItems;
                        break;

                    case StateBot.AddItems:

                        var itemName = msgText;

                        if (itemName != "/done")
                        {
                            shoppingList.Add(itemName);
                            await botClient.SendTextMessageAsync(chatId: chatId, text: $"{itemName} добавлен в ваш список");
                            await botClient.SendTextMessageAsync(chatId: chatId, text: "Введите следующий пункт или /done для завершения");
                        }
                        else
                        {
                            // TODO: сделать сохранение для каждого пользователя ориентируясь на userId

                            foreach(string str in shoppingList) {
                                await botClient.SendTextMessageAsync(chatId: chatId, text: str);
                            }
                            await botClient.SendTextMessageAsync(chatId: chatId, text: "Список сохранен");
                            _state = StateBot.Start;
                        }

                        break;
                }


            }
            else if (update.Message.Type == MessageType.ChatMemberLeft)
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"Пользователь {update.Message.LeftChatMember.FirstName} {update.Message.LeftChatMember.LastName} покинул чат");
            }

            System.IO.File.WriteAllText(fname, ((int)_state).ToString());
        }

        
        public static async Task SaveShoppingList(long chatId, string listName, List<string> shoppingList)
        {
            var listBuilder = new StringBuilder();
            foreach (var item in shoppingList)
            {
                listBuilder.AppendLine(item);
            }
            var fileName = $"{chatId}_{listName}.txt";
            await System.IO.File.WriteAllTextAsync(fileName, listBuilder.ToString());
        }
        

        private static Task Error(ITelegramBotClient arg1, Exception arg2, CancellationToken arg3)
        {
            throw new NotImplementedException();
        }

    }
}
