FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build-env
WORKDIR /app
COPY HueShift2/*.sln ./
COPY HueShift2/HueShift2/*.csproj ./HueShift2/
COPY HueShift2/HueShift2Tests/*.csproj ./HueShift2Tests/
RUN dotnet restore

COPY Hueshift2/* ./
RUN dotnet build
FROM build-env as test-runner
WORKDIR /app/HueShift2Tests/
CMD ["dotnet", "test", "--logger:trx"]

FROM build-env as unit-test
WORKDIR /app/HueShift2Tests/
RUN dotnet test --logger:trx


FROM build-env as publish
COPY /app/HueShift2/. ./
WORKDIR /app
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/runtime:5.0 AS runtime
WORKDIR /app
RUN mkdir -p config
VOLUME /config
RUN mkdir -p log
VOLUME /log
ENV UDPPORT 6454
EXPOSE ${UDPPORT}
EXPOSE ${UDPPORT}/udp
COPY --from=build-env /app/out ./
ENTRYPOINT ["dotnet", "HueShift2.dll", "--config-file", "/config/hueshift2-config.json"]
