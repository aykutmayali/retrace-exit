FROM mcr.microsoft.com/dotnet/aspnet:8.0
ENV CORECLR_PROFILER={cf0d821e-299b-5307-a3d8-b283c03916da}
ENV CORECLR_ENABLE_PROFILING=1
ENV CORECLR_PROFILER_PATH=/usr/local/stackify/stackify-dotnet-apm/StackifyProfiler.so
WORKDIR /app
COPY . .
ENTRYPOINT ["dotnet", "Sample.AspNetCoreShop.dll"]
