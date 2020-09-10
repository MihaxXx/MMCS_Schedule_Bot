FROM mcr.microsoft.com/dotnet/core/sdk:2.1 as build

WORKDIR /src

COPY MMCS_Schedule_Bot.sln MMCS_Schedule_Bot.sln
COPY MMCS_Schedule_Bot/MMCS_Schedule_Bot.csproj MMCS_Schedule_Bot/MMCS_Schedule_Bot.csproj

RUN dotnet restore

COPY . .

RUN dotnet build -c Release --no-restore
RUN dotnet publish -c Release --output "/dist"

FROM mcr.microsoft.com/dotnet/core/runtime:2.1 as deploy

RUN sudo ln -sf /usr/share/zoneinfo/Europe/Moscow /etc/localtime

COPY --from=build /dist /dist
WORKDIR /dist

CMD ["dotnet", "MMCS_Schedule_Bot.dll"]
