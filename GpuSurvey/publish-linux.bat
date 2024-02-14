@set progName=GpuSurvey
@set output=.\output\%progName%_linux
@rd /S /Q %output%

dotnet publish "%progName%\%progName%.csproj" --framework net6.0 --runtime linux-x64 -c debug --self-contained true -o "%output%" /p:NoBuild=false

@if not "%1" == "nopause" @pause