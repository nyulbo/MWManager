﻿using System;
using System.Collections.Generic;
using System.Configuration;
#if NETFX
using System.Configuration;
#endif
using MessageBus.ConnectionString;
using MessageBus.Loggers;

namespace MessageBus
{
    /// <summary>
    /// 메세지 연결 관리
    /// </summary>
    public static class ConnectionManager
    {
        private static Func<IContainer> _createContainerInternal = () => new DefaultServiceProvider();

        /// <summary>
        /// Set the container creation function. This allows you to replace EasyNetQ's default internal
        /// IoC container. Note that all components should be registered as singletons. EasyNetQ will
        /// also call Dispose on components that are no longer required.
        /// </summary>
        private static void SetContainerFactory(Func<IContainer> createContainer)
        {
            Preconditions.CheckNotNull(createContainer, "createContainer");

            _createContainerInternal = createContainer;
        }

#if NETFX
        /// <summary>
        /// Creates a new instance of <see cref="RabbitBus"/>.
        /// The RabbitMQ broker is defined in the connection string named 'rabbit'.
        /// </summary>
        /// <returns>
        /// A new <see cref="RabbitBus"/> instance.
        /// </returns>
        public static IBus CreateBus()
        {
            return CreateBus(c => {});
        }

        /// <summary>
        /// Creates a new instance of <see cref="RabbitBus"/>.
        /// The RabbitMQ broker is defined in the connection string named 'rabbit'.
        /// </summary>
        /// <param name="registerServices">
        /// Override default services. For example, to override the default <see cref="ILogger"/>:
        /// RabbitHutch.CreateBus("host=localhost", x => x.Register{ILogger}(_ => myLogger));
        /// </param>
        /// <returns>
        /// A new <see cref="RabbitBus"/> instance.
        /// </returns>
        public static IBus CreateBus(Action<IServiceRegister> registerServices)
        {
            return CreateBus(AdvancedBusEventHandlers.Default, registerServices);
        }

        /// <summary>
        /// Creates a new instance of <see cref="RabbitBus"/>.
        /// The RabbitMQ broker is defined in the connection string named 'rabbit'.
        /// </summary>
        /// <param name="advancedBusEventHandlers">
        /// An <see cref="AdvancedBusEventHandlers"/> instance which is used to add handlers
        /// to the events of the newly created <see cref="IBus.Advanced"/>.
        /// As <see cref="RabbitAdvancedBus"/> attempts to connect during instantiation, specifying a <see cref="AdvancedBusEventHandlers"/>
        /// before instantiation is the only way to catch the first <see cref="AdvancedBusEventHandlers.Connected"/> event.
        /// </param>
        /// <param name="registerServices">
        /// Override default services. For example, to override the default <see cref="ILogger"/>:
        /// RabbitHutch.CreateBus("host=localhost", x => x.Register{ILogger}(_ => myLogger));
        /// </param>
        /// <returns>
        /// A new <see cref="RabbitBus"/> instance.
        /// </returns>
        public static IBus CreateBus(AdvancedBusEventHandlers advancedBusEventHandlers, Action<IServiceRegister> registerServices)
        {
            string rabbitConnectionString;
            var rabbitConnection = ConfigurationManager.ConnectionStrings["rabbit"];
            if (rabbitConnection == null)
            {
                throw new EasyNetQException(
                    "Could not find a connection string for RabbitMQ. " +
                    "Please add a connection string in the <ConnectionStrings> section" +
                    "of the application's configuration file. For example: " +
                    "<add name=\"rabbit\" connectionString=\"host=localhost\" />");
            }
            rabbitConnectionString = rabbitConnection.ConnectionString;

            return CreateBus(rabbitConnectionString, advancedBusEventHandlers, registerServices);
        }

        /// <summary>
        /// Creates a new instance of <see cref="RabbitBus"/>.
        /// The RabbitMQ broker is defined in the connection string named 'rabbit'.
        /// </summary>
        /// <param name="advancedBusEventHandlers">
        /// An <see cref="AdvancedBusEventHandlers"/> instance which is used to add handlers
        /// to the events of the newly created <see cref="IBus.Advanced"/>.
        /// As <see cref="RabbitAdvancedBus"/> attempts to connect during instantiation, specifying a <see cref="AdvancedBusEventHandlers"/>
        /// before instantiation is the only way to catch the first <see cref="AdvancedBusEventHandlers.Connected"/> event.
        /// </param>
        /// <returns>
        /// A new <see cref="RabbitBus"/> instance.
        /// </returns>
        public static IBus CreateBus(AdvancedBusEventHandlers advancedBusEventHandlers)
        {
            return CreateBus(advancedBusEventHandlers, c => {});
        }
#endif
        /// <summary>
        /// 메세지 연결 생성
        /// </summary>
        /// <returns>IBus 객체</returns>
        public static IBus CreateBus()
        {   
            var connection = new ConnectionConfiguration();
            connection.UserName = ConfigurationManager.AppSettings["ID"];
            connection.Password = ConfigurationManager.AppSettings["PW"];
            
            connection.PersistentMessages = false;

            var host1 = new HostConfiguration();
            host1.Host = ConfigurationManager.AppSettings["HOSTNAME"];
            host1.Port = ushort.Parse(ConfigurationManager.AppSettings["PORT"]);

            connection.Hosts = new List<HostConfiguration> { host1 };
            connection.Validate();

            var container = _createContainerInternal();
            if (container == null)
            {
                throw new MessageBusException("Could not create container. " +
                    "Have you called SetContainerFactory(...) with a function that returns null?");
            }
            container.Register(_ => connection);
            container.Register(_ => AdvancedBusEventHandlers.Default);
            ComponentRegistration.RegisterServices(container);

            return container.Resolve<IBus>();
        }
        /// <summary>
        /// 메세지 연결 생성 - Logger
        /// </summary>
        /// <returns>IBus 객체</returns>
        public static IBus CreateBus(ConnectionConfiguration connConfig, HostConfiguration hostConfig)
        {
            var connection = new ConnectionConfiguration();

            connection.UserName = (string.IsNullOrEmpty(connConfig.UserName)) ? ConfigurationManager.AppSettings["ID"] : connConfig.UserName;
            connection.Password = (string.IsNullOrEmpty(connConfig.Password)) ? ConfigurationManager.AppSettings["PW"] : connConfig.Password;
            connection.PersistentMessages = false;

            var host1 = new HostConfiguration();
            host1.Host =
                (string.IsNullOrEmpty(hostConfig.Host)) ? ConfigurationManager.AppSettings["HOSTNAME"] : hostConfig.Host;
            host1.Port = ushort.Parse(ConfigurationManager.AppSettings["PORT"]);

            connection.Hosts = new List<HostConfiguration> { host1 };
            connection.Validate();

            var container = _createContainerInternal();
            if (container == null)
            {
                throw new MessageBusException("Could not create container. " +
                    "Have you called SetContainerFactory(...) with a function that returns null?");
            }

            container.Register(_ => connection);
            container.Register(_ => AdvancedBusEventHandlers.Default);
            ComponentRegistration.RegisterServices(container);

            return container.Resolve<IBus>();
        }
        /// <summary>
        /// 메세지 연결 생성 - Logger
        /// </summary>
        /// <returns>IBus 객체</returns>
        public static IBus CreateBus(ILogger logger)
        {
            var connection = new ConnectionConfiguration();

            connection.UserName = ConfigurationManager.AppSettings["ID"];
            connection.Password = ConfigurationManager.AppSettings["PW"];
            connection.PersistentMessages = false;

            var host1 = new HostConfiguration();
            host1.Host = ConfigurationManager.AppSettings["HOSTNAME"];
            host1.Port = ushort.Parse(ConfigurationManager.AppSettings["PORT"]);

            connection.Hosts = new List<HostConfiguration> { host1 };
            connection.Validate();

            var container = _createContainerInternal();
            if (container == null)
            {
                throw new MessageBusException("Could not create container. " +
                    "Have you called SetContainerFactory(...) with a function that returns null?");
            }
            
            container.Register(_ => connection);
            container.Register(_ => AdvancedBusEventHandlers.Default);
            container.Register(_ => logger);
            ComponentRegistration.RegisterServices(container);

            return container.Resolve<IBus>();
        }

        #region -- 미사용
        /// <summary>
        /// Creates a new instance of <see cref="RabbitBus"/>
        /// </summary>
        /// <param name="connectionString">
        /// The EasyNetQ connection string. Example:
        /// host=192.168.1.1;port=5672;virtualHost=MyVirtualHost;username=MyUsername;password=MyPassword;requestedHeartbeat=10
        /// 
        /// The following default values will be used if not specified:
        /// host=localhost;port=5672;virtualHost=/;username=guest;password=guest;requestedHeartbeat=10
        /// </param>
        /// <returns>
        /// A new <see cref="RabbitBus"/> instance.
        /// </returns>
        //private static IBus CreateBus(string connectionString)
        //{
        //    Preconditions.CheckNotNull(connectionString, "connectionString");

        //    return CreateBus(connectionString, AdvancedBusEventHandlers.Default);
        //}

        /// <summary>
        /// Creates a new instance of <see cref="RabbitBus"/>.
        /// </summary>
        /// <param name="connectionString">
        /// The EasyNetQ connection string. Example:
        /// host=192.168.1.1;port=5672;virtualHost=MyVirtualHost;username=MyUsername;password=MyPassword;requestedHeartbeat=10
        /// 
        /// The following default values will be used if not specified:
        /// host=localhost;port=5672;virtualHost=/;username=guest;password=guest;requestedHeartbeat=10
        /// </param>
        /// <param name="advancedBusEventHandlers">
        /// An <see cref="AdvancedBusEventHandlers"/> instance which is used to add handlers
        /// to the events of the newly created <see cref="IBus.Advanced"/>.
        /// As <see cref="RabbitAdvancedBus"/> attempts to connect during instantiation, specifying a <see cref="AdvancedBusEventHandlers"/>
        /// before instantiation is the only way to catch the first <see cref="AdvancedBusEventHandlers.Connected"/> event.
        /// </param>
        /// <returns>
        /// A new <see cref="RabbitBus"/> instance.
        /// </returns>
        //private static IBus CreateBus(string connectionString, AdvancedBusEventHandlers advancedBusEventHandlers)
        //{
        //    Preconditions.CheckNotNull(connectionString, "connectionString");
        //    Preconditions.CheckNotNull(advancedBusEventHandlers, "advancedBusEventHandlers");

        //    return CreateBus(connectionString, advancedBusEventHandlers, x => { });
        //}

        /// <summary>
        /// Creates a new instance of <see cref="RabbitBus"/>.
        /// </summary>
        /// <param name="connectionString">
        /// The EasyNetQ connection string. Example:
        /// host=192.168.1.1;port=5672;virtualHost=MyVirtualHost;username=MyUsername;password=MyPassword;requestedHeartbeat=10
        /// 
        /// The following default values will be used if not specified:
        /// host=localhost;port=5672;virtualHost=/;username=guest;password=guest;requestedHeartbeat=10
        /// </param>
        /// <param name="registerServices">
        /// Override default services. For example, to override the default <see cref="ILogger"/>:
        /// RabbitHutch.CreateBus("host=localhost", x => x.Register{ILogger}(_ => myLogger));
        /// </param>
        /// <returns>
        /// A new <see cref="RabbitBus"/> instance.
        /// </returns>
        //private static IBus CreateBus(string connectionString, Action<IServiceRegister> registerServices)
        //{
        //    Preconditions.CheckNotNull(connectionString, "connectionString");
        //    Preconditions.CheckNotNull(registerServices, "registerServices");

        //    return CreateBus(connectionString, AdvancedBusEventHandlers.Default, registerServices);
        //}

        /// <summary>
        /// Creates a new instance of <see cref="RabbitBus"/>.
        /// </summary>
        /// <param name="connectionString">
        /// The EasyNetQ connection string. Example:
        /// host=192.168.1.1;port=5672;virtualHost=MyVirtualHost;username=MyUsername;password=MyPassword;requestedHeartbeat=10
        /// 
        /// The following default values will be used if not specified:
        /// host=localhost;port=5672;virtualHost=/;username=guest;password=guest;requestedHeartbeat=10
        /// </param>
        /// <param name="advancedBusEventHandlers">
        /// An <see cref="AdvancedBusEventHandlers"/> instance which is used to add handlers
        /// to the events of the newly created <see cref="IBus.Advanced"/>.
        /// As <see cref="RabbitAdvancedBus"/> attempts to connect during instantiation, specifying a <see cref="AdvancedBusEventHandlers"/>
        /// before instantiation is the only way to catch the first <see cref="AdvancedBusEventHandlers.Connected"/> event.
        /// </param>
        /// <param name="registerServices">
        /// Override default services. For example, to override the default <see cref="ILogger"/>:
        /// RabbitHutch.CreateBus("host=localhost", x => x.Register{ILogger}(_ => myLogger));
        /// </param>
        /// <returns>
        /// A new <see cref="RabbitBus"/> instance.
        /// </returns>
        //private static IBus CreateBus(string connectionString, AdvancedBusEventHandlers advancedBusEventHandlers, Action<IServiceRegister> registerServices)
        //{
        //    Preconditions.CheckNotNull(connectionString, "connectionString");
        //    Preconditions.CheckNotNull(registerServices, "registerServices");
        //    Preconditions.CheckNotNull(advancedBusEventHandlers, "advancedBusEventHandlers");

        //    var connectionStringParser = new ConnectionStringParser();

        //    var connectionConfiguration = connectionStringParser.Parse(connectionString);

        //    return CreateBus(connectionConfiguration, advancedBusEventHandlers, registerServices);
        //}

        /// <summary>
        /// Creates a new instance of <see cref="RabbitBus"/>.
        /// </summary>
        /// <param name="hostName">
        /// The RabbitMQ broker.
        /// </param>
        /// <param name="hostPort">
        /// The RabbitMQ broker port.
        /// </param>
        /// <param name="virtualHost">
        /// The RabbitMQ virtualHost.
        /// </param>
        /// <param name="username">
        /// The username to use to connect to the RabbitMQ broker.
        /// </param>
        /// <param name="password">
        /// The password to use to connect to the RabbitMQ broker.
        /// </param>
        /// <param name="requestedHeartbeat">
        /// The initially requested heartbeat interval, in seconds; zero for none.
        /// </param>
        /// <param name="registerServices">
        /// Override default services. For example, to override the default <see cref="ILogger"/>:
        /// RabbitHutch.CreateBus("host=localhost", x => x.Register{ILogger}(_ => myLogger));
        /// </param>
        /// <returns>
        /// A new <see cref="RabbitBus"/> instance.
        /// </returns>
        //private static IBus CreateBus(
        //    string hostName,
        //    ushort hostPort,
        //    string virtualHost,
        //    string username,
        //    string password,
        //    ushort requestedHeartbeat,
        //    Action<IServiceRegister> registerServices)
        //{
        //    Preconditions.CheckNotNull(hostName, "hostName");
        //    Preconditions.CheckNotNull(virtualHost, "virtualHost");
        //    Preconditions.CheckNotNull(username, "username");
        //    Preconditions.CheckNotNull(password, "password");
        //    Preconditions.CheckNotNull(registerServices, "registerServices");

        //    return CreateBus(hostName, hostPort, virtualHost, username, password, requestedHeartbeat, AdvancedBusEventHandlers.Default, registerServices);
        //}

        /// <summary>
        /// Creates a new instance of <see cref="RabbitBus"/>.
        /// </summary>
        /// <param name="hostName">
        /// The RabbitMQ broker.
        /// </param>
        /// <param name="hostPort">
        /// The RabbitMQ broker port.
        /// </param>
        /// <param name="virtualHost">
        /// The RabbitMQ virtualHost.
        /// </param>
        /// <param name="username">
        /// The username to use to connect to the RabbitMQ broker.
        /// </param>
        /// <param name="password">
        /// The password to use to connect to the RabbitMQ broker.
        /// </param>
        /// <param name="requestedHeartbeat">
        /// The initially requested heartbeat interval, in seconds; zero for none.
        /// </param>
        /// <param name="advancedBusEventHandlers">
        /// An <see cref="AdvancedBusEventHandlers"/> instance which is used to add handlers
        /// to the events of the newly created <see cref="IBus.Advanced"/>.
        /// As <see cref="RabbitAdvancedBus"/> attempts to connect during instantiation, specifying a <see cref="AdvancedBusEventHandlers"/>
        /// before instantiation is the only way to catch the first <see cref="AdvancedBusEventHandlers.Connected"/> event.
        /// </param>
        /// <param name="registerServices">
        /// Override default services. For example, to override the default <see cref="ILogger"/>:
        /// RabbitHutch.CreateBus("host=localhost", x => x.Register{ILogger}(_ => myLogger));
        /// </param>
        /// <returns>
        /// A new <see cref="RabbitBus"/> instance.
        /// </returns>
        //private static IBus CreateBus(
        //    string hostName,
        //    ushort hostPort,
        //    string virtualHost,
        //    string username,
        //    string password,
        //    ushort requestedHeartbeat,
        //    AdvancedBusEventHandlers advancedBusEventHandlers,
        //    Action<IServiceRegister> registerServices)
        //{
        //    Preconditions.CheckNotNull(hostName, "hostName");
        //    Preconditions.CheckNotNull(virtualHost, "virtualHost");
        //    Preconditions.CheckNotNull(username, "username");
        //    Preconditions.CheckNotNull(password, "password");
        //    Preconditions.CheckNotNull(advancedBusEventHandlers, "advancedBusEventHandlers");
        //    Preconditions.CheckNotNull(registerServices, "registerServices");

        //    var connectionConfiguration = new ConnectionConfiguration
        //    {
        //        Hosts = new List<HostConfiguration>
        //            {
        //                new HostConfiguration { Host = hostName, Port = hostPort }
        //            },
        //        Port = hostPort,
        //        VirtualHost = virtualHost,
        //        UserName = username,
        //        Password = password,
        //        RequestedHeartbeat = requestedHeartbeat
        //    };
        //    return CreateBus(connectionConfiguration, advancedBusEventHandlers, registerServices);
        //}

        /// <summary>
        /// Creates a new instance of <see cref="RabbitBus"/>.
        /// </summary>
        /// <param name="connectionConfiguration">
        /// An <see cref="ConnectionConfiguration"/> instance.
        /// </param>
        /// <param name="registerServices">
        /// Override default services. For example, to override the default <see cref="ILogger"/>:
        /// RabbitHutch.CreateBus("host=localhost", x => x.Register{ILogger}(_ => myLogger));
        /// </param>
        /// <returns>
        /// A new <see cref="RabbitBus"/> instance.
        /// </returns>
        //public static IBus CreateBus(ConnectionConfiguration connectionConfiguration, Action<IServiceRegister> registerServices)
        //{
        //    Preconditions.CheckNotNull(connectionConfiguration, "connectionConfiguration");
        //    Preconditions.CheckNotNull(registerServices, "registerServices");

        //    return CreateBus(connectionConfiguration, AdvancedBusEventHandlers.Default, registerServices);
        //}

        /// <summary>
        /// Creates a new instance of <see cref="RabbitBus"/>.
        /// </summary>
        /// <param name="connectionConfiguration">
        /// An <see cref="ConnectionConfiguration"/> instance.
        /// </param>
        /// <param name="advancedBusEventHandlers">
        /// An <see cref="AdvancedBusEventHandlers"/> instance which is used to add handlers
        /// to the events of the newly created <see cref="IBus.Advanced"/>.
        /// As <see cref="RabbitAdvancedBus"/> attempts to connect during instantiation, specifying a <see cref="AdvancedBusEventHandlers"/>
        /// before instantiation is the only way to catch the first <see cref="AdvancedBusEventHandlers.Connected"/> event.
        /// </param>
        /// <param name="registerServices">
        /// Override default services. For example, to override the default <see cref="ILogger"/>:
        /// RabbitHutch.CreateBus("host=localhost", x => x.Register{ILogger}(_ => myLogger));
        /// </param>
        /// <returns>
        /// A new <see cref="RabbitBus"/> instance.
        /// </returns>
        //private static IBus CreateBus(ConnectionConfiguration connectionConfiguration, 
        //    AdvancedBusEventHandlers advancedBusEventHandlers, 
        //    Action<IServiceRegister> registerServices)
        //{
        //    Preconditions.CheckNotNull(connectionConfiguration, "connectionConfiguration");
        //    Preconditions.CheckNotNull(advancedBusEventHandlers, "advancedBusEventHandlers");
        //    Preconditions.CheckNotNull(registerServices, "registerServices");

        //    var container = _createContainerInternal();
        //    if (container == null)
        //    {   
        //        throw new MessageBusException("Could not create container. " +
        //            "Have you called SetContainerFactory(...) with a function that returns null?");
        //    }

        //    connectionConfiguration.Validate();
        //    container.Register(_ => connectionConfiguration);
        //    container.Register(_ => advancedBusEventHandlers);
        //    registerServices(container);
        //    ComponentRegistration.RegisterServices(container);

        //    return container.Resolve<IBus>();
        //}
        #endregion -- 미사용
    }
}