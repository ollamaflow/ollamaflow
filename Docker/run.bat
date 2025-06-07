@echo off

IF "%1" == "" GOTO :Usage

if not exist ollamaflow.json (
  echo Configuration file ollamaflow.json not found.
  exit /b 1
)

REM Items that require persistence
REM   ollamaflow.json
REM   logs/

REM Argument order matters!

docker run ^
  -p 43411:43411 ^
  -t ^
  -i ^
  -e "TERM=xterm-256color" ^
  -v .\ollamaflow.json:/app/ollamaflow.json ^
  -v .\logs\:/app/logs/ ^
  jchristn/ollamaflow:%1

GOTO :Done

:Usage
ECHO Provide one argument indicating the tag. 
ECHO Example: dockerrun.bat v2.0.0
:Done
@echo on
