using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Dadata;

//Set API keys from .ini file
var lines = System.IO.File.ReadAllLines("api_keys.ini");
string TELEGRAM_TOKEN = lines[0];
string DADATA_TOKEN = lines[1];

bool sentToMeMode  = false;

var botClient = new TelegramBotClient(TELEGRAM_TOKEN);
using var cts = new CancellationTokenSource();

var receiverOptions = new ReceiverOptions
{
    AllowedUpdates = { }
};
botClient.StartReceiving(
    HandleUpdateAsync,
    HandleErrorAsync,
    receiverOptions,
    cancellationToken : cts.Token
    );
var me = await botClient.GetMeAsync();
Console.WriteLine("Начинаем работу с @" + me.Username);
await Task.Delay(int.MaxValue);
cts.Cancel();

async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    #region [Главное меню]
    InlineKeyboardMarkup mainMenu = new InlineKeyboardMarkup(new[]
    {
        new[] { InlineKeyboardButton.WithCallbackData(text: "Автор бота", callbackData:"credit")},
        new[] { InlineKeyboardButton.WithCallbackData(text: "Что я умею?", callbackData:"help")},
        new[] { InlineKeyboardButton.WithCallbackData(text: "Поиск компании по ИНН", callbackData:"startSearch")}
    });
    #endregion
    InlineKeyboardMarkup backToMenu = new (new[]
    {
        new[] { InlineKeyboardButton.WithCallbackData(text: "В главное меню", callbackData:"toMenu")}
    });

    if (update.Type == UpdateType.Message && update.Message!.Type == MessageType.Text)
    {
        var chatId = update.Message.Chat.Id;
        var messageText = update.Message.Text;
        string firstName = update.Message.From.FirstName;
        Console.WriteLine($"Получено сообщение: '{firstName}' в чате {chatId}");
        #region [Первое сообщение]
        if (messageText == "/start")
        {
            Message sentMessage = await botClient.SendTextMessageAsync
            (
                chatId: chatId,
                text: $"Привет, {firstName}! \n\nЯ бот для поиска компаний по ИНН.\nЧто ты хочешь сделать?",
                replyMarkup: mainMenu,
                cancellationToken: cancellationToken
            );
        }
        #endregion

        if (sentToMeMode)
        {
            var api = new SuggestClientAsync(DADATA_TOKEN);
            string[] inns = messageText.Split(' ');
            foreach (string inn in inns)
            {
                if (ulong.TryParse(inn, out ulong value))
                {
                    var response = await api.FindParty(inn);
                    if (response.suggestions.Count >= 1)
                    {
                        var party = response.suggestions[0].data;
                        Message sentMessage = await botClient.SendTextMessageAsync
                        (
                            chatId: chatId,
                            text: $"ИНН: {response.suggestions[0].data.inn}\nНазвание компании: {response.suggestions[0].value}\nАдрес компании: {response.suggestions[0].data.address.data.source}",
                            cancellationToken: cancellationToken
                        );
                    }
                    else
                    {
                        Message sentMessage = await botClient.SendTextMessageAsync
                        (
                        chatId: chatId,
                        text: $"Не получается найти компанию по данному ИНН: {inn}\n\nДанной компании не существует.",
                        cancellationToken: cancellationToken
                        );
                    }
                }
                else
                {
                    Message sentMessage = await botClient.SendTextMessageAsync
                    (
                        chatId: chatId,
                        text: $"Не получается найти компанию по данному ИНН: {inn}\n\nНекорректный ввод номера ИНН.",
                        cancellationToken: cancellationToken
                    );
                }
            }
            Message menuMessage = await botClient.SendTextMessageAsync
            (
                chatId: chatId,
                text: "Чем ещё могу быть полезен?",
                replyMarkup: mainMenu,
                cancellationToken: cancellationToken
            );
            sentToMeMode = false;
        }
        else if (messageText != "/start")
        {
            Message sentMessage = await botClient.SendTextMessageAsync
            (
            chatId: chatId,
            text: "Чтобы я начал поиск по ИНН, нажмите на кнопку \"Поиск компании по ИНН\".",
            replyMarkup: mainMenu,
            cancellationToken: cancellationToken
            );
        }
    }

    if (update.CallbackQuery != null)
    {
        if (update.CallbackQuery.Data == "credit")
        {
            Message creditMessage = await botClient.EditMessageTextAsync
                (
                    messageId: update.CallbackQuery.Message.MessageId,
                    chatId: update.CallbackQuery.Message.Chat.Id,
                    text: "Автор бота: Киселев Игорь\nПочта: pingerock@mail.ru\nДата получения задания: 05.11.2023",
                    replyMarkup: backToMenu,
                    cancellationToken: cancellationToken
                );
            sentToMeMode = false;
        }
        if (update.CallbackQuery.Data == "help")
        {
            Message helpMessage = await botClient.EditMessageTextAsync
                (
                    messageId: update.CallbackQuery.Message.MessageId,
                    chatId: update.CallbackQuery.Message.Chat.Id,
                    text: "Я бот для поиска информации о компаниях по ИНН.\nДля работы со мной нужно нажать на кнопку \"Поиск компании по ИНН\" и ввести номер ИНН нужной компании.\n\nЯ умею искать данные нескольких компаний за одну команду. Нужно ввести номера ИНН одним сообщением, разделяя их пробелами.\n\nНадеюсь, я смогу вам помочь!",
                    replyMarkup: backToMenu,
                    cancellationToken: cancellationToken
                );
            sentToMeMode = false;
        }
        if (update.CallbackQuery.Data == "startSearch")
        {
            Message searchMessage = await botClient.EditMessageTextAsync
                (
                    messageId: update.CallbackQuery.Message.MessageId,
                    chatId: update.CallbackQuery.Message.Chat.Id,
                    text: "Введите один или несколько номеров ИНН нужной компании. Разделяйте номера ИНН пробелами.",
                    replyMarkup: backToMenu,
                    cancellationToken: cancellationToken
                );
            sentToMeMode = true;
        }
        if (update.CallbackQuery.Data == "toMenu")
        {
            Message menuMessage = await botClient.EditMessageTextAsync
                (
                    messageId: update.CallbackQuery.Message.MessageId,
                    chatId: update.CallbackQuery.Message.Chat.Id,
                    text: "Чем могу быть полезен?",
                    replyMarkup: mainMenu,
                    cancellationToken: cancellationToken
                );
            sentToMeMode = false;
        }
    }
    
}

Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
{
    var ErrorMessage = exception switch
    {
        ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
        _ => exception.ToString()
    };
    Console.WriteLine(ErrorMessage);
    return Task.CompletedTask;
}