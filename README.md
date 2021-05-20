# ApiTest

We test our code, but how to deal with APIs?<br>
Should we hope that everything works, or can we *check* if everything works?<br>
This is a simple helper that can let you run an API in a controlled environment.<br>
<br>
Usage example:
```<language>
IConfiguration configuration = new ConfigurationBuilder()
	.AddInMemoryCollection(new Dictionary<string, string>
						   {
							   {"key1", "value1"},
							   {"key2", "value2"}
						   })
	.Build();
	
using var host = ApiTestHelper.GetTestHost<Startup>(configuration, ServicesConfiguration);
var response = host.GetHttpResponseMessage("/Get", ApiTestHelper.HttpVerbs.Get)
	.EnsureSuccessStatusCode();
```

Instances can be replaced directly in the test, by passing something like:
```
void ServicesConfiguration(IServiceCollection services)
{
    services.Replace<IConfigReader, ConfigReaderMock>();
    services.Replace<IDbConnectionFactory, DbConnectionFactoryMock>();
    services.Replace<IDbRepository, FakeDbRepository>();
    services.Replace<IMemoryCache, FakeMemoryCache>();
}
```
<br>
<br>

Feel free to contribute to this project.<br>
https://github.com/jonnidip/ApiTest