# ExchangeRates
 WebAPI enabling you to easily query exchange rates based on ECB data.
# How does it work?
This API returns exchange rates grouped by currencies and dates. For the first time each value is missing in the database it reaches the ECB public API to get the rates and then saves them in the database. If the rates are requested in the future the API will only use the local database in order to accelerate results. Each unique (considering URL parameters) request is saved in memory cache, accelerating even more subsequent identical queries.  
Sample API calls are availble in the Postman file located in the root of this repo.
# Architecture
.NET Core 3.1 - last .NET with "Core" in its name. The solution should work on any OS and CPU.  
The default connection string tries to connect to MS SQL localdb using Windows authentication. This can be changes in the settings file.  
The solution uses NuGet packages for:
* database modelling (Microsoft.EntityFrameworkCore.*)
* code generation (Microsoft.VisualStudio.Web.CodeGeneration.*)
* documentation (Swashbuckle.AspNetCore.*)
All packages were updated to the latest verions supporting the platform.  
The default webpage ("/") shown after runninng the project is Swagger.
