@set progName=ImageProcessing
@set output=.\output\%progName%_win
@rd /S /Q %output%

dotnet publish "%progName%.csproj" -r win-x64 --self-contained true -o "%output%\bin" /p:NoBuild=false
xcopy /Y /E "config" %output%\config\

@if not "%1" == "nopause" @pause