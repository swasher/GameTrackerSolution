; Скрипт для Inno Setup для приложения Game Tracker
; Этот скрипт собирает клиент и сервер, и устанавливает сервер как службу Windows.

#define MyAppName "Game Tracker"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Your Company Name"
#define MyAppURL "https://github.com/swasher/GameTrackerSolution"
#define MyClientExeName "GameTrackerClient.exe"
#define MyServiceExeName "GameTrackerService.exe"
#define MyServiceName "GameTrackerService"
#define MyServiceDisplayName "Game Tracker Service"

; Целевые версии .NET для клиента и сервера. Они должны совпадать с Makefile.
#define ClientNetVersion "net8.0-windows"
#define ServerNetVersion "net8.0"

[Setup]
; Уникальный ID вашего приложения. Inno Setup может сгенерировать его автоматически.
AppId={{F29F4B87-8E3C-4B9A-9A6E-7C5A3A0E5F1B}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
; Имя выходного файла инсталлятора
OutputBaseFilename=GameTracker-{#MyAppVersion}-setup
; Каталог для скомпилированного инсталлятора (относительно этого .iss файла)
OutputDir=dist
; Иконка для инсталлятора и для "Установки и удаления программ"
SetupIconFile=GameTrackerClient\Assets\app.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern
; Запрос прав администратора необходим для установки службы
PrivilegesRequired=admin

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Копируем опубликованные файлы сервера.
; Путь Source должен соответствовать пути публикации в Makefile.
Source: "GameTrackerService\bin\Release\{#ServerNetVersion}\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
 
; Копируем опубликованные файлы клиента.
Source: "GameTrackerClient\bin\Release\{#ClientNetVersion}\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; Примечание: Флаг recursesubdirs важен, так как dotnet publish может создавать подпапки (например, runtimes).

[Icons]
; Ярлык в меню "Пуск" для клиента
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyClientExeName}"
; Ярлык для деинсталлятора
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
; Ярлык на рабочем столе (опционально, через задачу)
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyClientExeName}"; Tasks: desktopicon

[Run]
; После копирования файлов, регистрируем и запускаем службу.
; Используем утилиту sc.exe, которая всегда есть в Windows.
; 1. Создаем службу. binPath должен содержать пробел после =. start= auto - для автозапуска.
Filename: "sc.exe"; Parameters: "create {#MyServiceName} binPath= ""{app}\{#MyServiceExeName}"" start= auto"; Flags: runhidden

; 2. Добавляем описание службе (опционально, но полезно).
Filename: "sc.exe"; Parameters: "description {#MyServiceName} ""Tracks application usage time and provides statistics."""; Flags: runhidden

; 3. Запускаем службу.
Filename: "sc.exe"; Parameters: "start {#MyServiceName}"; Flags: runhidden

[UninstallRun]
; При деинсталляции необходимо корректно остановить и удалить службу.
; Выполняется перед удалением файлов.
; 1. Останавливаем службу.
Filename: "sc.exe"; Parameters: "stop {#MyServiceName}"; Flags: runhidden waituntilterminated; RunOnceId: "stop_service"

; 2. Удаляем службу.
Filename: "sc.exe"; Parameters: "delete {#MyServiceName}"; Flags: runhidden waituntilterminated; RunOnceId: "delete_service"

[Code]
// Этот раздел можно использовать для более сложных проверок, например, наличия .NET Runtime.
// Для простоты мы оставляем его пустым, но это хорошее место для будущих улучшений.

function NeedsRestart(): Boolean;
begin
  Result := false;
end;