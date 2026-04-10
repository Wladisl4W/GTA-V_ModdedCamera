ScriptCamTool
=============

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
  ДЛЯ ПОЛЬЗОВАТЕЛЕЙ — папка "Ready To Use"
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Требования:
  • Script Hook V            — https://dev-c.com/gtav/scripthookv/
  • Script Hook V .NET 3     — https://github.com/scripthookvdotnet/scripthookvdotnet-nightly/releases
  • LemonUI.SHVDN3.dll       — https://gta5-mods.com/tools/lemonui

Установка:
  1. Установите Script Hook V и Script Hook V .NET в корень GTA V
  2. Скачайте LemonUI и скопируйте LemonUI.SHVDN3.dll в GTA V\scripts\
  3. Скопируйте ModdedCamera.dll и Newtonsoft.Json.dll в GTA V\scripts\
  4. Папка paths создаётся автоматически при первом сохранении пути

В GTA V\scripts\ должно быть:
  • ModdedCamera.dll
  • Newtonsoft.Json.dll
  • LemonUI.SHVDN3.dll

==========================
  ИСПОЛЬЗОВАНИЕ
==========================

Горячие клавиши:
  • T — открыть/закрыть главное меню
  • Backspace — навигация назад в подменю

Главное меню (Script Cam Tool):
  • Start Rendering — запустить камеру по заданному пути
  • Stop Rendering — остановить камеру
  • Setup Nodes — режим расстановки нод (ЛКМ = добавить, ПКМ = выход)
  • Save Current Path — сохранить текущий путь
  • Load Path — загрузить сохранённый путь
  • Camera Options — настройки (скорость, FOV, интерполяция)
  • Reset All Cams — сбросить все камеры

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
  ДЛЯ РАЗРАБОТЧИКОВ — папка "Source Code"
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Исходный код проекта, решение Visual Studio и зависимости.
Сборка через ModdedCamera.sln в Visual Studio (x64, .NET Framework 4.8).


