@set progName=ScriptingSurvey
@set output=.\output\%progName%_linux
@rd /S /Q %output%

dotnet publish "%progName%.csproj" --framework net6.0 --runtime linux-x64 -c debug --self-contained true -o "%output%\bin" /p:NoBuild=false
:: xcopy /Y /E "config" %output%\config\

@if not "%1" == "nopause" @pause