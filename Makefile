# Makefile для сборки решения GameTracker и создания инсталлятора.

# --- Конфигурация ---

# Целевые версии .NET для клиента и сервера. Они отличаются.
CLIENT_TARGET_FRAMEWORK = net8.0-windows
SERVER_TARGET_FRAMEWORK = net8.0

# Путь к компилятору Inno Setup. Убедитесь, что он правильный.
# По умолчанию он устанавливается сюда.
ISCC = ISCC.exe

# --- Переменные проекта ---
SOLUTION_DIR = .
CLIENT_PROJ = $(SOLUTION_DIR)/GameTrackerClient/GameTrackerClient.csproj
# Убедитесь, что имя проекта сервера и его расположение верны.
SERVER_PROJ = $(SOLUTION_DIR)/GameTrackerService/GameTrackerService.csproj

CLIENT_PUBLISH_DIR = $(SOLUTION_DIR)/GameTrackerClient/bin/Release/$(CLIENT_TARGET_FRAMEWORK)/publish
SERVER_PUBLISH_DIR = $(SOLUTION_DIR)/GameTrackerService/bin/Release/$(SERVER_TARGET_FRAMEWORK)/publish

OUTPUT_DIR = $(SOLUTION_DIR)/dist
ISS_SCRIPT = $(SOLUTION_DIR)/setup.iss

# --- Команды ---
.PHONY: all build installer clean

# Цель по умолчанию: собрать всё и создать инсталлятор.
all: installer

# Увеличение patch-версии
bump:
	@echo "--> Bumping patch version..."
	@old_version=$$(cat VERSION); \
	IFS='.' read -r major minor patch <<< $$old_version; \
	new_patch=$$((patch + 1)); \
	new_version="$$major.$$minor.$$new_patch"; \
	echo $$new_version > VERSION; \
	echo "Version bumped to $$new_version"

# Публикация обоих проектов в режиме Release.
# 'dotnet publish' собирает проект и копирует все зависимости в одну папку.
build:
	@echo "--> Publishing C# projects..."
	dotnet publish $(CLIENT_PROJ) -c Release -f $(CLIENT_TARGET_FRAMEWORK) -o $(CLIENT_PUBLISH_DIR)
	dotnet publish $(SERVER_PROJ) -c Release -f $(SERVER_TARGET_FRAMEWORK) -o $(SERVER_PUBLISH_DIR)

# Создание инсталлятора с помощью Inno Setup.
installer: bump build
	@echo "--> Creating installer with Inno Setup..."
	"$(ISCC)" $(ISS_SCRIPT)

# Очистка артефактов сборки.
clean:
	@echo "--> Cleaning build artifacts..."
	-if exist $(OUTPUT_DIR) rmdir /s /q $(OUTPUT_DIR)
	-if exist $(SOLUTION_DIR)/GameTrackerClient/bin rmdir /s /q $(SOLUTION_DIR)/GameTrackerClient/bin
	-if exist $(SOLUTION_DIR)/GameTrackerClient/obj rmdir /s /q $(SOLUTION_DIR)/GameTrackerClient/obj
	-if exist $(SOLUTION_DIR)/GameTrackerService/bin rmdir /s /q $(SOLUTION_DIR)/GameTrackerService/bin
	-if exist $(SOLUTION_DIR)/GameTrackerService/obj rmdir /s /q $(SOLUTION_DIR)/GameTrackerService/obj