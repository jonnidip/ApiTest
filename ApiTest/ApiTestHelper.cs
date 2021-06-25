#nullable enable
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace ApiTest
{
    public static class ApiTestHelper
    {
        /// <summary>
        /// Creates a new host for testing
        /// </summary>
        /// <typeparam name="TStartup">The startup class</typeparam>
        /// <param name="configuration">Test configuration, as <c>Dictionary&lt;string, string&gt;</c></param>
        /// <returns></returns>
        public static IHost GetTestHost<TStartup>(IDictionary<string, string> configuration) where TStartup : class
            => GetTestHost<TStartup>(new ConfigurationBuilder()
                .AddInMemoryCollection(configuration)
                .Build());

        /// <summary>
        /// Creates a new host for testing
        /// </summary>
        /// <typeparam name="TStartup">The startup class</typeparam>
        /// <param name="configuration">Test configuration, as IConfiguration. Use ConfigurationBuilder to create an in-memory collection</param>
        /// <returns></returns>
        public static IHost GetTestHost<TStartup>(IConfiguration configuration) where TStartup : class
            => GetTestHost<TStartup>(configuration, null);

        /// <summary>
        /// Creates a new host for testing
        /// </summary>
        /// <typeparam name="TStartup">The startup class</typeparam>
        /// <param name="configuration">Test configuration, as IConfiguration. Use ConfigurationBuilder to create an in-memory collection</param>
        /// <param name="servicesConfiguration">Method that makes needed services substitution</param>
        /// <example>
        /// <code>
        /// void ServicesConfiguration(IServiceCollection services)
        /// {
        ///     services.Replace&lt;IConfigReader, ConfigReaderMock&gt;();
        ///     services.Replace&lt;IDbConnectionFactory, DbConnectionFactoryMock&gt;();
        ///     services.Replace&lt;IDbRepository, FakeDbRepository&gt;();
        ///     services.Replace&lt;IMemoryCache, FakeMemoryCache&gt;();
        /// }
        /// </code>
        /// </example>
        /// <returns></returns>
        public static IHost GetTestHost<TStartup>(IConfiguration configuration, Action<IServiceCollection>? servicesConfiguration) where TStartup : class
        {
            var host = new HostBuilder()
                .ConfigureWebHost(hostBuilder =>
                {
                    hostBuilder.UseTestServer();
                    hostBuilder.ConfigureAppConfiguration(builder =>
                    {
                        builder.Sources.Clear();
                        builder.AddConfiguration(configuration);
                    });
                    hostBuilder.UseStartup<TStartup>();

                    if (servicesConfiguration != null)
                        hostBuilder.ConfigureTestServices(servicesConfiguration);
                }).Build();

            host.Start();

            return host;
        }

        public static HttpResponseMessage GetHttpResponseMessage(this IHost host, string requestUri, HttpVerbs verb, HttpContent? content = null)
        {
            using var client = host.GetTestClient();
            return verb switch
            {
                HttpVerbs.Get => client.GetAsync(requestUri).Result,
                HttpVerbs.Put => client.PutAsync(requestUri, content ?? new StringContent(string.Empty)).Result,
                HttpVerbs.Post => client.PostAsync(requestUri, content ?? new StringContent(string.Empty)).Result,
                HttpVerbs.Delete => client.DeleteAsync(requestUri).Result,
                HttpVerbs.Head => throw new NotImplementedException($"{verb} not implemented"),
                HttpVerbs.Patch => throw new NotImplementedException($"{verb} not implemented"),
                HttpVerbs.Options => throw new NotImplementedException($"{verb} not implemented"),
                _ => throw new ArgumentOutOfRangeException(nameof(verb), verb, $"{verb} is unexpected.")
            };
        }

        // Type: System.Web.Mvc.HttpVerbs
        // Assembly: System.Web.Mvc, Version=5.2.7.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
        /// <summary>Enumerates the HTTP verbs.</summary>
        [Flags]
        public enum HttpVerbs
        {
            /// <summary>Retrieves the information or entity that is identified by the URI of the request.</summary>
            Get = 1,
            /// <summary>Posts a new entity as an addition to a URI.</summary>
            Post = 2,
            /// <summary>Replaces an entity that is identified by a URI.</summary>
            Put = 4,
            /// <summary>Requests that a specified URI be deleted.</summary>
            Delete = 8,
            /// <summary>Retrieves the message headers for the information or entity that is identified by the URI of the request.</summary>
            Head = 16, // 0x00000010
            /// <summary>Requests that a set of changes described in the   request entity be applied to the resource identified by the Request-   URI.</summary>
            Patch = 32, // 0x00000020
            /// <summary>Represents a request for information about the communication options available on the request/response chain identified by the Request-URI.</summary>
            Options = 64 // 0x00000040
        }

        public static void Replace<TService, TImplementation>(this IServiceCollection services) where TImplementation : class, TService
        {
            var serviceDescriptors = services.Where(x => x.ServiceType == typeof(TService)).ToList();
            foreach (var serviceDescriptor in serviceDescriptors)
            {
                services.Remove(serviceDescriptor);
                switch (serviceDescriptor.Lifetime)
                {
                    case ServiceLifetime.Singleton:
                        services.AddSingleton(typeof(TService), typeof(TImplementation));
                        break;
                    case ServiceLifetime.Scoped:
                        services.AddScoped(typeof(TService), typeof(TImplementation));
                        break;
                    case ServiceLifetime.Transient:
                        services.AddTransient(typeof(TService), typeof(TImplementation));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(serviceDescriptor.Lifetime), serviceDescriptor.Lifetime, $"{serviceDescriptor.Lifetime} was not expected.");
                }
            }
        }
    }
}