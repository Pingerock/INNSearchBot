# INNSearchBot
# Description
Russian Telegram Bot for searching company's name and address by their INN(Taxpayer Personal Identification Number - "INN" in russian)

# APIs
This bot uses next APIs: <br/>
-Telegram for working with bot itself <br/>
-Dadata to search information about company by it's INN (https://dadata.ru/api/) <br/>

# Storing API keys
You must to create "api_keys.ini" file for storing your api keys.
In the first line you write down the token for the telegram bot. In the second line you write the token from the site dadata.ru
Save this .ini file in folder with your application.

# Setting up
This bot is written in .NET Core 6 so you can host it on any VPS server (Windows, Linux, MacOS).
