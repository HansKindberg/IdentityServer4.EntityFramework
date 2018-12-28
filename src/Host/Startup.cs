// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServer4.Models;
using IdentityServer4.Quickstart.UI;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Host
{
	public class Startup
	{
		#region Fields

		private readonly IConfiguration _config;
		private readonly IHostingEnvironment _env;

		#endregion

		#region Constructors

		public Startup(IConfiguration config, IHostingEnvironment env)
		{
			this._config = config;
			this._env = env;
		}

		#endregion

		#region Methods

		public void Configure(IApplicationBuilder app)
		{
			app.UseDeveloperExceptionPage();

			app.UseIdentityServer();

			app.UseStaticFiles();
			app.UseMvcWithDefaultRoute();

			this.InitializeDatabase(app);
		}

		public IServiceProvider ConfigureServices(IServiceCollection services)
		{
			const string connectionString = @"Data Source=(LocalDb)\MSSQLLocalDB;database=IdentityServer4.EntityFramework-2.0.0;trusted_connection=yes;";
			var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;

			services.AddIdentityServer()
				.AddDeveloperSigningCredential()
				.AddTestUsers(TestUsers.Users)
				// this adds the config data from DB (clients, resources, CORS)
				.AddConfigurationStore(options =>
				{
					options.ResolveDbContextOptions = (provider, builder) =>
					{
						builder.UseSqlServer(connectionString,
							sql => sql.MigrationsAssembly(migrationsAssembly));
					};
					//options.ConfigureDbContext = builder =>
					//    builder.UseSqlServer(connectionString,
					//        sql => sql.MigrationsAssembly(migrationsAssembly));
				})
				// this adds the operational data from DB (codes, tokens, consents)
				.AddOperationalStore(options =>
				{
					options.ConfigureDbContext = builder =>
						builder.UseSqlServer(connectionString,
							sql => sql.MigrationsAssembly(migrationsAssembly));

					// this enables automatic token cleanup. this is optional.
					options.EnableTokenCleanup = true;
					options.TokenCleanupInterval = 10; // interval in seconds, short for testing
				});
			//.AddConfigurationStoreCache();

			services.AddMvc();

			return services.BuildServiceProvider(validateScopes: true);
		}

		protected internal virtual IEnumerable<ApiResource> GetApiResources()
		{
			return new List<ApiResource>
			{
				new ApiResource("api", "Demo API")
				{
					ApiSecrets = {new Secret("secret".Sha256())}
				}
			};
		}

		protected internal virtual IEnumerable<Client> GetClients()
		{
			return new List<Client>
			{
				// native clients
				new Client
				{
					ClientId = "native.hybrid",
					ClientName = "Native Client (Hybrid with PKCE)",

					RedirectUris = {"https://notused"},
					PostLogoutRedirectUris = {"https://notused"},

					RequireClientSecret = false,

					AllowedGrantTypes = GrantTypes.Hybrid,
					RequirePkce = true,
					AllowedScopes = {"openid", "profile", "email", "api"},

					AllowOfflineAccess = true,
					RefreshTokenUsage = TokenUsage.ReUse
				},
				new Client
				{
					ClientId = "server.hybrid",
					ClientName = "Server-based Client (Hybrid)",

					RedirectUris = {"https://notused"},
					PostLogoutRedirectUris = {"https://notused"},

					ClientSecrets = {new Secret("secret".Sha256())},

					AllowedGrantTypes = GrantTypes.Hybrid,
					AllowedScopes = {"openid", "profile", "email", "api"},

					AllowOfflineAccess = true,
					RefreshTokenUsage = TokenUsage.ReUse
				},
				new Client
				{
					ClientId = "native.code",
					ClientName = "Native Client (Code with PKCE)",

					RedirectUris = {"https://notused"},
					PostLogoutRedirectUris = {"https://notused"},

					RequireClientSecret = false,

					AllowedGrantTypes = GrantTypes.Code,
					RequirePkce = true,
					AllowedScopes = {"openid", "profile", "email", "api"},

					AllowOfflineAccess = true,
					RefreshTokenUsage = TokenUsage.ReUse
				},
				new Client
				{
					ClientId = "server.code",
					ClientName = "Service Client (Code)",

					RedirectUris = {"https://notused"},
					PostLogoutRedirectUris = {"https://notused"},

					ClientSecrets = {new Secret("secret".Sha256())},

					AllowedGrantTypes = GrantTypes.Code,
					AllowedScopes = {"openid", "profile", "email", "api"},

					AllowOfflineAccess = true,
					RefreshTokenUsage = TokenUsage.ReUse
				},

				// server to server
				new Client
				{
					ClientId = "client",
					ClientSecrets = {new Secret("secret".Sha256())},

					AllowedGrantTypes = GrantTypes.ClientCredentials,
					AllowedScopes = {"api"},
				},

				// implicit (e.g. SPA or OIDC authentication)
				new Client
				{
					ClientId = "implicit",
					ClientName = "Implicit Client",
					AllowAccessTokensViaBrowser = true,

					RedirectUris = {"https://notused"},
					PostLogoutRedirectUris = {"https://notused"},
					FrontChannelLogoutUri = "http://localhost:5000/signout-idsrv", // for testing identityserver on localhost

					AllowedGrantTypes = GrantTypes.Implicit,
					AllowedScopes = {"openid", "profile", "email", "api"},
				},

				// implicit using reference tokens (e.g. SPA or OIDC authentication)
				new Client
				{
					ClientId = "implicit.reference",
					ClientName = "Implicit Client using reference tokens",
					AllowAccessTokensViaBrowser = true,

					AccessTokenType = AccessTokenType.Reference,

					RedirectUris = {"https://notused"},
					PostLogoutRedirectUris = {"https://notused"},

					AllowedGrantTypes = GrantTypes.Implicit,
					AllowedScopes = {"openid", "profile", "email", "api"},
				},

				// implicit using reference tokens (e.g. SPA or OIDC authentication)
				new Client
				{
					ClientId = "implicit.shortlived",
					ClientName = "Implicit Client using short-lived tokens",
					AllowAccessTokensViaBrowser = true,

					AccessTokenLifetime = 70,

					RedirectUris = {"https://notused"},
					PostLogoutRedirectUris = {"https://notused"},

					AllowedGrantTypes = GrantTypes.Implicit,
					AllowedScopes = {"openid", "profile", "email", "api"},
				}
			};
		}

		protected internal virtual IEnumerable<IdentityResource> GetIdentityResources()
		{
			return new List<IdentityResource>
			{
				new IdentityResources.OpenId(),
				new IdentityResources.Profile(),
				new IdentityResources.Email(),
			};
		}

		/// <summary>
		/// http://docs.identityserver.io/en/release/quickstarts/8_entity_framework.html
		/// https://github.com/IdentityServer/IdentityServer4.Demo/blob/master/src/IdentityServer4Demo/Config.cs
		/// https://github.com/IdentityServer/IdentityServer4.Samples
		/// </summary>
		private void InitializeDatabase(IApplicationBuilder app)
		{
			using(var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
			{
				serviceScope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>().Database.Migrate();

				var context = serviceScope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
				context.Database.Migrate();
				if(!context.Clients.Any())
				{
					foreach(var client in this.GetClients())
					{
						context.Clients.Add(client.ToEntity());
					}

					context.SaveChanges();
				}

				if(!context.IdentityResources.Any())
				{
					foreach(var resource in this.GetIdentityResources())
					{
						context.IdentityResources.Add(resource.ToEntity());
					}

					context.SaveChanges();
				}

				if(!context.ApiResources.Any())
				{
					foreach(var resource in this.GetApiResources())
					{
						context.ApiResources.Add(resource.ToEntity());
					}

					context.SaveChanges();
				}
			}
		}

		#endregion
	}
}